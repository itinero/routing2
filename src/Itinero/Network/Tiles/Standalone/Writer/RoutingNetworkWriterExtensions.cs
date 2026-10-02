using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Itinero.Data;
using Itinero.Network.Tiles.Standalone.Global;
using Itinero.Network.Writer;

namespace Itinero.Network.Tiles.Standalone.Writer;

/// <summary>
/// Extension methods related to writing standalone tiles to a network.
/// </summary>
public static class RoutingNetworkWriterExtensions
{
    /// <summary>
    /// Per-thread scratch for the global edge ids one insert registers.
    /// </summary>
    [ThreadStatic]
    private static List<GlobalEdgeId>? ArrivedEdges;


    /// <summary>
    /// Adds a tile in the form of a standalone tile to the network.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="tile">The tile to add.</param>
    /// <param name="globalIdSet">The global id set.</param>
    /// <remarks>
    /// One lock acquisition for the whole insert, not one per edge.
    ///
    /// Three phases:
    ///
    /// <list type="number">
    /// <item><b>Claim, unlocked.</b> Register the tile's internal edges' global ids and claim the
    /// boundary crossings whose other half has arrived. A standalone tile arrives with its internal
    /// edges already encoded, so nothing here writes tile data — it fills concurrent dictionaries.
    /// Doing it before any lock is what makes the next step possible.</item>
    /// <item><b>Write, grouped by neighbour.</b> Create the matched boundary edges holding this
    /// tile's stripe and one partner's, taken together and ordered, all of that partner's
    /// crossings at once. Each edge is created exactly
    /// once — only the tile that won the claim in step 1 builds it — and writing it puts an entry
    /// in both tiles, which is how a cross-tile edge is represented.</item>
    /// <item><b>Restrictions.</b> Separate, and locking themselves, because a restriction names
    /// edges in tiles that cannot be predicted from the tile being inserted.</item>
    /// </list>
    /// </remarks>
    public static void AddStandaloneTile(this RoutingNetworkWriter writer, StandaloneNetworkTile tile,
        GlobalNetworkManager globalIdSet)
    {
        // ---- install first, under this tile's stripe alone ----
        // Before anything is claimable. A crossing this tile records becomes visible to the tile
        // on the other side, which will immediately want this tile's vertices to build the edge —
        // so the tile has to be in the network before its halves are published. Claiming first and
        // installing later fails with "vertex not found" on the other thread.
        using (writer.LockTiles(stackalloc[] { tile.TileId }))
        {
            writer.AddTile(tile.NetworkTile);
        }

        // ---- phase 1: no tile writes, no locks ----
        // Reused per thread rather than allocated per insert: ~346 ids per tile is a 5KB list
        // every time, on a path where GC already shows up. Gating the collection on "is anything
        // pending" instead looked cheaper and was wrong — a restriction parked by another thread
        // mid-insert then waits for the sweep, which made resolution depend on how many tiles
        // happen to follow. ConcurrentInsert_ManyTilesWithBoundaryCrossings_MatchesSerialInsert
        // fails on that.
        //
        // Safe to reuse: the list never outlives this call. RetryPendingRestrictionsFor reads it
        // synchronously, and nothing here inserts a tile re-entrantly on the same thread.
        var arrived = ArrivedEdges ??= new List<GlobalEdgeId>(512);
        arrived.Clear();

        // 2.6M map entries per cold block against ~200k reads, and its only consumer is turn
        // restriction resolution — routing never reads this map, since the global id a client sees
        // comes from the edge's own attributes. Skipping it entirely was measured (+7.1% cold) and
        // is NOT an option: it silently drops turn restrictions that name an interior edge.
        RegisterInternalEdges(tile, globalIdSet, arrived);
        var crossings = ClaimBoundaryCrossings(tile, globalIdSet);
        // ---- phase 2: every tile this insert writes, locked once ----
        // Grouped by partner tile: one acquisition per NEIGHBOUR, not per crossing and not one
        // covering every partner at once.
        //
        // Both extremes are bad here. Locking this tile plus all of its partners for the whole
        // batch holds up to nine stripes at once, and the inserts running concurrently are
        // precisely the ones that share boundaries — VertexTouched loads a tile together with its
        // eight neighbours, BoxTouched loads a whole box — so every insert collides with every
        // other. Locking per crossing keeps holds short but pays an acquisition for each of the
        // many ways that cross a single boundary.
        //
        // A tile has at most eight neighbours and usually fewer, so grouping bounds the
        // acquisitions by neighbours while keeping each hold to two stripes.
        crossings.Sort(static (x, y) => x.OtherVertex.TileId.CompareTo(y.OtherVertex.TileId));
        var start = 0;
        while (start < crossings.Count)
        {
            var partner = crossings[start].OtherVertex.TileId;
            var end = start;
            while (end < crossings.Count && crossings[end].OtherVertex.TileId == partner) end++;

            using (writer.LockTiles(stackalloc[] { tile.TileId, partner }))
            {
                for (var i = start; i < end; i++)
                {
                    CreateBoundaryEdge(writer, globalIdSet, crossings[i]);
                    arrived.Add(crossings[i].GlobalEdgeId);
                }
            }

            start = end;
        }

        // ---- phase 3: restrictions ----
        ResolveRestrictions(writer, tile, globalIdSet, arrived);

        // Give back the headroom geometric growth took. The growth is what makes the appends above
        // linear; the slack is what would otherwise stay resident for the life of the process, and
        // that reaches even the hot regime, which does no inserts at all.
        writer.TrimTile(tile.TileId);
    }

    /// <summary>
    /// Registers every internal edge's GlobalEdgeId → EdgeId mapping, collecting the ids it wrote.
    /// </summary>
    /// <remarks>
    /// The collected ids are what a pending restriction can be waiting for, so they drive the
    /// targeted retry instead of a full pass over the pending list.
    /// </remarks>
    private static int RegisterInternalEdges(StandaloneNetworkTile tile, GlobalNetworkManager globalIdSet,
        List<GlobalEdgeId> arrived)
    {
        var registered = 0;
        var tileEnumerator = new NetworkTileEnumerator();
        tileEnumerator.MoveTo(tile.NetworkTile);
        var tileId = tile.TileId;
        for (uint v = 0; v < tile.NetworkTile.VertexCount; v++)
        {
            var vertexId = new VertexId(tileId, v);
            if (!tileEnumerator.MoveTo(vertexId)) continue;

            while (tileEnumerator.MoveNext())
            {
                // only process forward edges to avoid double registration.
                if (!tileEnumerator.Forward) continue;

                var globalEdgeId = tileEnumerator.GlobalEdgeId;
                if (globalEdgeId != null)
                {
                    globalIdSet.EdgeIdSet.Set(globalEdgeId.Value, tileEnumerator.EdgeId);

                    arrived.Add(globalEdgeId.Value);
                    registered++;
                }
            }
        }

        return registered;
    }

    /// One half of a boundary crossing matched with the other, ready to become an edge.
    private readonly record struct MatchedCrossing(
        GlobalEdgeId GlobalEdgeId,
        bool IsIncoming,
        VertexId Vertex,
        (string key, string value)[] Attributes,
        uint EdgeTypeId,
        VertexId OtherVertex);

    /// <summary>
    /// Claims the crossings whose other half has already arrived, recording the rest.
    /// </summary>
    /// <remarks>
    /// Returns the matches instead of creating the edges here, so that no tile lock is taken
    /// while still walking the tile — which is what keeps phase 1 free of any lock ordering.
    /// </remarks>
    private static List<MatchedCrossing> ClaimBoundaryCrossings(StandaloneNetworkTile tile,
        GlobalNetworkManager globalIdSet)
    {
        var matched = new List<MatchedCrossing>();
        foreach (var (isIncoming, globalEdgeId, vertex, attributes, edgeTypeId) in tile.GetBoundaryCrossings())
        {
            // Materialised before the claim: the stored half outlives this tile's enumerator, and
            // the claim may hand it to the other tile's thread at any moment.
            var ownAttributes = attributes.ToArray();

            // Claim-or-record in one step. Reading, then creating the edge, then removing would
            // let two tiles each see the other's half and each create the edge; the claim makes
            // exactly one of them the winner.
            if (globalIdSet.TryClaimBoundaryCrossing(globalEdgeId,
                    (vertex, ownAttributes, edgeTypeId, isIncoming), out var pending))
            {
                matched.Add(new MatchedCrossing(globalEdgeId, isIncoming, vertex, ownAttributes,
                    edgeTypeId, pending.vertex));
            }

            // No else: when the claim fails this tile's half has already been recorded by the
            // same call, and the other tile will find it.
        }

        return matched;
    }

    /// <summary>
    /// Creates one boundary edge. Caller holds the locks for both tiles.
    /// </summary>
    private static void CreateBoundaryEdge(RoutingNetworkWriter writer, GlobalNetworkManager globalIdSet,
        MatchedCrossing crossing)
    {
        // isIncoming=true: vertex is at way tail, other is at way head.
        var tail = crossing.IsIncoming ? crossing.Vertex : crossing.OtherVertex;
        var head = crossing.IsIncoming ? crossing.OtherVertex : crossing.Vertex;

        var length = writer.ComputeEdgeLength(tail, head);
        var newEdge = writer.AddEdge(tail, head, null, crossing.Attributes,
            crossing.EdgeTypeId, length, crossing.GlobalEdgeId);

        // register boundary edge's GlobalEdgeId → EdgeId mapping.
        globalIdSet.EdgeIdSet.Set(crossing.GlobalEdgeId, newEdge);
    }

    /// Resolves this tile's restrictions, then retries everything still pending.
    private static void ResolveRestrictions(RoutingNetworkWriter writer, StandaloneNetworkTile tile,
        GlobalNetworkManager globalIdSet, IReadOnlyList<GlobalEdgeId> arrived)
    {
        var seen = 0;
        var parked = 0;
        foreach (var (edges, isProhibitory, turnCostTypeId, restrictionAttributes) in tile.GetGlobalRestrictions())
        {
            var globalRestriction = new GlobalRestriction(
                edges.Select(e => e.globalEdgeId),
                isProhibitory,
                restrictionAttributes.ToArray());

            seen++;
            if (!TryResolveRestriction(globalRestriction, globalIdSet, writer))
            {
                parked++;
                globalIdSet.AddPendingRestriction(globalRestriction);
            }
        }

        // Retry only the restrictions waiting for an edge this insert added. A full pass here
        // re-examined every pending restriction on every insert - 535,147 examinations for 2,335
        // resolutions over one cold block.
        globalIdSet.RetryPendingRestrictionsFor(arrived,
            r => TryResolveRestriction(r, globalIdSet, writer));
    }

    private static bool TryResolveRestriction(GlobalRestriction globalRestriction,
        GlobalNetworkManager globalIdSet, RoutingNetworkWriter writer)
    {
        // try to resolve all GlobalEdgeIds to EdgeIds.
        if (!globalRestriction.TryBuildNetworkRestriction(GetEdge, out var networkRestriction))
            return false;

        if (networkRestriction!.Count < 2) return true;

        // get last edge and determine turn cost vertex.
        var last = networkRestriction[^1];
        var edgeEnumerator = writer.GetEdgeEnumerator();
        if (!edgeEnumerator.MoveTo(last.edge, last.forward))
            return false;
        var turnCostVertex = edgeEnumerator.Tail;

        var secondToLast = networkRestriction[^2];

        if (networkRestriction.IsProhibitory)
        {
            // prohibitory: add a single cost entry forbidding this specific turn.
            var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
            writer.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                [secondToLast.edge, last.edge], costs,
                networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));

        }
        else
        {
            // mandatory: add cost for every *other* edge at the vertex.
            if (!edgeEnumerator.MoveTo(secondToLast.edge, secondToLast.forward))
                return false;
            var to = edgeEnumerator.Head;

            edgeEnumerator.MoveTo(to);
            while (edgeEnumerator.MoveNext())
            {
                if (edgeEnumerator.EdgeId == secondToLast.edge ||
                    edgeEnumerator.EdgeId == last.edge) continue;

                var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
                writer.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                    [secondToLast.edge, edgeEnumerator.EdgeId], costs,
                    networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
            }
        }

        return true;

        (EdgeId edge, bool forward)? GetEdge(GlobalEdgeId geid, bool isFirst)
        {
            // exact match short-circuit (cheap check before walking).
            if (globalIdSet.EdgeIdSet.TryGet(geid, out var edgeId))
                return (edgeId, true);
            if (globalIdSet.EdgeIdSet.TryGet(geid.GetInverted(), out edgeId))
                return (edgeId, false);

            // walk from the chain-anchor end; first edge anchors at Head, every
            // subsequent edge anchors at Tail (chain invariant: previous.Head ==
            // current.Tail).
            return GlobalRestrictionExtensions.WalkFromAnchor(geid, isFirst,
                globalIdSet.EdgeIdSet.TryGet);
        }
    }
}
