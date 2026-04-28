using System;
using System.Collections.Generic;
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
    /// Adds a tile in the form of a standalone tile to the network.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="tile">The tile to add.</param>
    /// <param name="globalIdSet">The global id set.</param>
    public static void AddStandaloneTile(this RoutingNetworkWriter writer, StandaloneNetworkTile tile,
        GlobalNetworkManager globalIdSet)
    {
        // add the tile without boundary crossings.
        writer.AddTile(tile.NetworkTile);

        // register all GlobalEdgeId → EdgeId mappings from the tile's internal edges.
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
                }
            }
        }

        // process boundary crossings.
        foreach (var (isIncoming, globalEdgeId, vertex, attributes, edgeTypeId) in tile.GetBoundaryCrossings())
        {
            if (globalIdSet.PendingBoundaryCrossings.TryGetValue(globalEdgeId, out var pending))
            {
                // match found - the other tile already loaded its half, create the boundary edge.
                EdgeId newEdge;
                if (isIncoming)
                {
                    // isIncoming=true: vertex is at way tail, pending.vertex is at way head.
                    var length = writer.ComputeEdgeLength(vertex, pending.vertex);
                    newEdge = writer.AddEdge(vertex, pending.vertex, null, attributes, edgeTypeId, length, globalEdgeId);
                }
                else
                {
                    // isIncoming=false: vertex is at way head, pending.vertex is at way tail.
                    var length = writer.ComputeEdgeLength(pending.vertex, vertex);
                    newEdge = writer.AddEdge(pending.vertex, vertex, null, attributes, edgeTypeId, length, globalEdgeId);
                }

                // register boundary edge's GlobalEdgeId → EdgeId mapping.
                globalIdSet.EdgeIdSet.Set(globalEdgeId, newEdge);
                globalIdSet.PendingBoundaryCrossings.Remove(globalEdgeId);
            }
            else
            {
                // no match yet - store as pending (materialize attributes).
                globalIdSet.PendingBoundaryCrossings[globalEdgeId] =
                    (vertex, attributes.ToArray(), edgeTypeId, isIncoming);
            }
        }

        // resolve global restrictions from this tile.
        foreach (var (edges, isProhibitory, turnCostTypeId, restrictionAttributes) in tile.GetGlobalRestrictions())
        {
            var globalRestriction = new GlobalRestriction(
                edges.Select(e => e.globalEdgeId),
                isProhibitory,
                restrictionAttributes.ToArray());

            if (!TryResolveRestriction(globalRestriction, globalIdSet, writer))
            {
                globalIdSet.PendingRestrictions.Add(globalRestriction);
            }
        }

        // retry pending restrictions with newly available edges.
        for (var i = globalIdSet.PendingRestrictions.Count - 1; i >= 0; i--)
        {
            if (TryResolveRestriction(globalIdSet.PendingRestrictions[i], globalIdSet, writer))
            {
                globalIdSet.PendingRestrictions.RemoveAt(i);
            }
        }
    }

    private static bool TryResolveRestriction(GlobalRestriction globalRestriction,
        GlobalNetworkManager globalIdSet, RoutingNetworkWriter writer)
    {
        // try to resolve all GlobalEdgeIds to EdgeIds.
        if (!globalRestriction.TryBuildNetworkRestriction(
                (Func<GlobalEdgeId, ushort?, (EdgeId edge, bool forward)?>)GetEdge,
                out var networkRestriction))
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

        (EdgeId edge, bool forward)? GetEdge(GlobalEdgeId geid, ushort? pivot = null)
        {
            if (globalIdSet.EdgeIdSet.TryGet(geid, out var edgeId))
                return (edgeId, true);
            if (globalIdSet.EdgeIdSet.TryGet(geid.GetInverted(), out edgeId))
                return (edgeId, false);

            // exact match not found — search for a subsection sharing the
            // endpoint closest to the restricted vertex. When a pivot is given
            // (the shared endpoint with the adjacent edge in a restriction chain),
            // search adjacent to that pivot. Otherwise default to the head end
            // (turn restriction "to" semantics: head is the restricted vertex).
            var lo = Math.Min(geid.Tail, geid.Head);
            var hi = Math.Max(geid.Tail, geid.Head);
            var fwd = geid.Tail < geid.Head;
            var pivotIsHi = pivot.HasValue ? pivot.Value == hi : fwd;

            for (var d = 1; d < hi - lo; d++)
            {
                if (pivotIsHi)
                {
                    if (TryHi(d, out var r)) return r;
                }
                else
                {
                    if (TryLo(d, out var r)) return r;
                }
            }

            return null;

            bool TryLo(int d, out (EdgeId edge, bool forward)? result)
            {
                result = null;
                var h = lo + d;
                if (h >= hi) return false;
                var sub = GlobalEdgeId.Create(geid.EdgeId, lo, h);
                if (globalIdSet.EdgeIdSet.TryGet(sub, out var eId))
                {
                    result = fwd ? (eId, true) : (eId, false);
                    return true;
                }
                if (globalIdSet.EdgeIdSet.TryGet(sub.GetInverted(), out eId))
                {
                    result = fwd ? (eId, false) : (eId, true);
                    return true;
                }
                return false;
            }

            bool TryHi(int d, out (EdgeId edge, bool forward)? result)
            {
                result = null;
                var t = hi - d;
                if (t <= lo) return false;
                var sub = GlobalEdgeId.Create(geid.EdgeId, t, hi);
                if (globalIdSet.EdgeIdSet.TryGet(sub, out var eId))
                {
                    result = fwd ? (eId, true) : (eId, false);
                    return true;
                }
                if (globalIdSet.EdgeIdSet.TryGet(sub.GetInverted(), out eId))
                {
                    result = fwd ? (eId, false) : (eId, true);
                    return true;
                }
                return false;
            }
        }
    }
}
