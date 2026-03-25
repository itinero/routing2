using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

internal class IslandBuilder
{
    /// <summary>
    /// Resolves whether an edge is on an island. Keeps expanding until definitive.
    /// </summary>
    public static async Task<bool?> ResolveEdgeAsync(RoutingNetwork network, Profile profile, EdgeId edgeId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return null;

        var islands = network.IslandManager.GetIslandsFor(profile);
        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile);

        // quick checks.
        if (islands.IsEdgeOnIsland(edgeId)) return true;
        if (dg.IsNotIsland(edgeId)) return false;

        var costFunction = network.GetCostFunctionFor(profile);
        var edgeEnumerator = network.GetEdgeEnumerator();
        var maxIslandSize = network.IslandManager.MaxIslandSize;

        // process the edge.
        ProcessEdge(dg, costFunction, edgeEnumerator, edgeId, islands, maxIslandSize);
        if (!dg.IsInGraph(edgeId)) return null; // not traversable

        // expand outward from the edge's component until resolved.
        var processedTiles = new HashSet<uint>();
        var tilesToProcess = new Queue<uint>();

        // seed with tiles of vertices of the edge.
        if (edgeEnumerator.MoveTo(edgeId, true))
        {
            tilesToProcess.Enqueue(edgeEnumerator.Tail.TileId);
            tilesToProcess.Enqueue(edgeEnumerator.Head.TileId);
        }

        while (tilesToProcess.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            // check resolution.
            if (islands.IsEdgeOnIsland(edgeId)) return true;
            if (dg.IsNotIsland(edgeId)) return false;

            var tileId = tilesToProcess.Dequeue();
            if (!processedTiles.Add(tileId)) continue;

            // load tile.
            await network.UsageNotifier.NotifyVertex(network, new VertexId(tileId, 0), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;

            var tile = network.GetTileForRead(tileId);
            if (tile == null) continue;

            // process all edges in the tile.
            var tileEnumerator = new NetworkTileEnumerator();
            tileEnumerator.MoveTo(tile);
            var vertex = new VertexId(tileId, 0);
            while (tileEnumerator.MoveTo(vertex))
            {
                while (tileEnumerator.MoveNext())
                {
                    if (!tileEnumerator.Forward) continue;
                    ProcessEdge(dg, costFunction, edgeEnumerator, tileEnumerator.EdgeId, islands, maxIslandSize);

                    // early exit if resolved.
                    if (dg.IsNotIsland(edgeId)) return false;
                }
                vertex = new VertexId(tileId, vertex.LocalId + 1);
            }
            if (dg.IsNotIsland(edgeId)) return false;

            // try resolve with cycle detection.
            var candidates = CollectComponentCandidates(dg, edgeId, islands);
            TryResolve(dg, islands, candidates, maxIslandSize);

            if (islands.IsEdgeOnIsland(edgeId)) return true;
            if (dg.IsNotIsland(edgeId)) return false;

            // enqueue tiles of unresolved neighbors.
            EnqueueNeighborTiles(dg, edgeId, edgeEnumerator, processedTiles, tilesToProcess);
        }

        // final resolution attempt.
        var finalCandidates = CollectComponentCandidates(dg, edgeId, islands);
        TryResolve(dg, islands, finalCandidates, maxIslandSize);

        if (islands.IsEdgeOnIsland(edgeId)) return true;
        if (dg.IsNotIsland(edgeId)) return false;

        // no more tiles to expand and still unresolved.
        // if the component can't reach the sentinel, it's isolated → island.
        return true;
    }

    /// <summary>
    /// Processes all edges in a tile into the DG.
    /// </summary>
    public static async Task BuildForTileAsync(RoutingNetwork network, Profile profile, uint tileId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        var islands = network.IslandManager.GetIslandsFor(profile);

        var tile = network.GetTileForRead(tileId);
        if (tile == null) return;

        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile);
        var costFunction = network.GetCostFunctionFor(profile);
        var edgeEnumerator = network.GetEdgeEnumerator();
        var maxIslandSize = network.IslandManager.MaxIslandSize;

        var tileEnumerator = new NetworkTileEnumerator();
        tileEnumerator.MoveTo(tile);
        var vertex = new VertexId(tileId, 0);
        while (tileEnumerator.MoveTo(vertex))
        {
            while (tileEnumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (!tileEnumerator.Forward) continue;
                ProcessEdge(dg, costFunction, edgeEnumerator, tileEnumerator.EdgeId, islands, maxIslandSize);
            }
            vertex = new VertexId(tileId, vertex.LocalId + 1);
        }

        // collect tile edges as candidates.
        var candidates = new List<EdgeId>();
        tileEnumerator.MoveTo(tile);
        vertex = new VertexId(tileId, 0);
        while (tileEnumerator.MoveTo(vertex))
        {
            while (tileEnumerator.MoveNext())
            {
                if (!tileEnumerator.Forward) continue;
                var eid = tileEnumerator.EdgeId;
                if (dg.IsInGraph(eid) && !dg.IsNotIsland(eid) && !islands.IsEdgeOnIsland(eid))
                    candidates.Add(eid);
            }
            vertex = new VertexId(tileId, vertex.LocalId + 1);
        }

        TryResolve(dg, islands, candidates, maxIslandSize);

        if (candidates.Count == 0)
            islands.SetTileDone(tileId);
    }

    /// <summary>
    /// Processes a single edge into the DG.
    /// </summary>
    internal static void ProcessEdge(IslandDirectedGraph dg,
        ICostFunction costFunction, RoutingNetworkEdgeEnumerator edgeEnumerator,
        EdgeId edgeId, Islands islands, int maxIslandSize)
    {
        if (dg.IsProcessed(edgeId)) return;
        if (dg.IsNotIsland(edgeId)) return;
        if (islands.IsEdgeOnIsland(edgeId)) return;

        if (!edgeEnumerator.MoveTo(edgeId, true)) return;
        var canForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        if (!edgeEnumerator.MoveTo(edgeId, false)) return;
        var canBackward = costFunction.GetIslandBuilderCost(edgeEnumerator);

        if (!canForward && !canBackward) return;

        dg.AddVertex(edgeId);

        for (var pass = 0; pass < 2; pass++)
        {
            var forward = pass == 0;
            if (forward && !canForward) continue;
            if (!forward && !canBackward) continue;

            edgeEnumerator.MoveTo(edgeId, forward);
            var targetVertex = edgeEnumerator.Head;
            var order = edgeEnumerator.HeadOrder;
            var prevEdges = order.HasValue ? new (EdgeId, byte?)[] { (edgeId, order) } : null;

            if (!edgeEnumerator.MoveTo(targetVertex)) continue;

            while (edgeEnumerator.MoveNext())
            {
                if (edgeEnumerator.EdgeId == edgeId) continue;

                var neighborId = edgeEnumerator.EdgeId;

                // skip known islands.
                if (islands.IsEdgeOnIsland(neighborId)) continue;

                // determine DG vertex for the neighbor.
                EdgeId neighborDgVertex;
                if (dg.IsNotIsland(neighborId))
                {
                    neighborDgVertex = IslandDirectedGraph.MainNetworkSentinel;
                }
                else
                {
                    dg.AddVertex(neighborId);
                    neighborDgVertex = neighborId;
                }

                // can this edge travel TO the neighbor?
                var canGoTo = costFunction.GetIslandBuilderCost(edgeEnumerator, true, prevEdges);
                if (canGoTo)
                {
                    dg.AddDirectedLink(edgeId, neighborDgVertex);
                    // bidirectional merge check: O(1).
                    if (dg.HasDirectedLink(neighborDgVertex, edgeId))
                    {
                        dg.Merge(edgeId, neighborDgVertex);
                        var newSize = dg.GetSize(dg.Find(edgeId));
                        if (newSize >= maxIslandSize)
                            dg.CollapseToMainNetwork(edgeId);
                    }
                }

                // can the neighbor travel TO this edge (arrive at the shared vertex)?
                // no turn cost check — just check if the neighbor can be traversed
                // in the direction that arrives at the shared vertex.
                // if Forward=true: tail is at shared vertex → arriving means head→tail = backward
                // if Forward=false: head is at shared vertex → arriving means tail→head = forward
                var canComeFrom = costFunction.GetIslandBuilderCost(edgeEnumerator, !edgeEnumerator.Forward);
                if (canComeFrom)
                {
                    dg.AddDirectedLink(neighborDgVertex, edgeId);
                    // bidirectional merge check: O(1).
                    if (dg.HasDirectedLink(edgeId, neighborDgVertex))
                    {
                        dg.Merge(edgeId, neighborDgVertex);
                        var newSize = dg.GetSize(dg.Find(edgeId));
                        if (newSize >= maxIslandSize)
                            dg.CollapseToMainNetwork(edgeId);
                    }
                }

                if (dg.IsNotIsland(edgeId)) break;

                // re-enumerate to continue.
                edgeEnumerator.MoveTo(targetVertex);
                while (edgeEnumerator.MoveNext())
                {
                    if (edgeEnumerator.EdgeId == neighborId) break;
                }
            }

            if (dg.IsNotIsland(edgeId)) break;
        }

        dg.SetProcessed(edgeId);
    }

    /// <summary>
    /// Resolves candidates: dead-end pruning, cycle detection, reachability.
    /// </summary>
    private static void TryResolve(IslandDirectedGraph dg, Islands islands,
        List<EdgeId> candidates, int maxIslandSize)
    {
        bool changed;
        do
        {
            changed = false;

            // 1. Prune dead-ends.
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var edgeId = candidates[i];
                if (!dg.IsInGraph(edgeId) || dg.IsNotIsland(edgeId))
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                if (dg.IsDeadEnd(edgeId))
                {
                    var members = dg.GetMembers(dg.Find(edgeId));
                    if (members != null)
                    {
                        var copy = new List<EdgeId>(members);
                        foreach (var m in copy)
                        {
                            islands.SetEdgeOnIsland(m);
                            dg.RemoveEdge(m);
                        }
                    }
                    candidates.RemoveAt(i);
                    changed = true;
                }
            }

            // 2. Detect one-way cycles among remaining candidates (Tarjan's SCC).
            if (candidates.Count >= 2)
            {
                if (dg.DetectAndMergeCycles(candidates))
                {
                    changed = true;
                    // check if any merged component now exceeds MaxIslandSize.
                    for (var i = candidates.Count - 1; i >= 0; i--)
                    {
                        var edgeId = candidates[i];
                        if (!dg.IsInGraph(edgeId) || dg.IsNotIsland(edgeId))
                        {
                            candidates.RemoveAt(i);
                            continue;
                        }

                        var root = dg.Find(edgeId);
                        if (dg.GetSize(root) >= maxIslandSize)
                        {
                            dg.CollapseToMainNetwork(root);
                            candidates.RemoveAt(i);
                        }
                    }
                }
            }

            // 3. Check if any remaining candidate can reach the sentinel both ways.
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var edgeId = candidates[i];
                if (!dg.IsInGraph(edgeId) || dg.IsNotIsland(edgeId))
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                if (dg.CanReachMainNetwork(edgeId))
                {
                    dg.CollapseToMainNetwork(edgeId);
                    candidates.RemoveAt(i);
                    changed = true;
                }
            }
        } while (changed);
    }

    private static List<EdgeId> CollectComponentCandidates(IslandDirectedGraph dg, EdgeId edgeId, Islands islands)
    {
        var candidates = new List<EdgeId>();
        if (!dg.IsInGraph(edgeId)) return candidates;

        var members = dg.GetMembers(dg.Find(edgeId));
        if (members != null)
        {
            foreach (var m in members)
            {
                if (!dg.IsNotIsland(m) && !islands.IsEdgeOnIsland(m))
                    candidates.Add(m);
            }
        }

        return candidates;
    }

    private static void EnqueueNeighborTiles(IslandDirectedGraph dg, EdgeId edgeId,
        RoutingNetworkEdgeEnumerator edgeEnumerator,
        HashSet<uint> processedTiles, Queue<uint> tilesToProcess)
    {
        if (!dg.IsInGraph(edgeId)) return;

        var members = dg.GetMembers(dg.Find(edgeId));
        if (members == null) return;

        foreach (var member in members)
        {
            if (!edgeEnumerator.MoveTo(member, true)) continue;
            var t = edgeEnumerator.Tail.TileId;
            var h = edgeEnumerator.Head.TileId;
            if (!processedTiles.Contains(t)) tilesToProcess.Enqueue(t);
            if (!processedTiles.Contains(h)) tilesToProcess.Enqueue(h);
        }
    }
}
