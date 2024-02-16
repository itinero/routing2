using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.IO.Json.GeoJson;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.DataStructures;

namespace Itinero.Network.Search.Islands;

internal class IslandBuilder
{
    public static async Task BuildForTileAsync(RoutingNetwork network, Profile profile, uint tileId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        var islands = network.IslandManager.GetIslandsFor(profile);

        var tile = network.GetTileForRead(tileId);
        if (tile == null) return;

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(tile);

        var labels = new IslandLabels(network.IslandManager.MaxIslandSize);
        var costFunction = network.GetCostFunctionFor(profile);
        var vertex = new VertexId(tileId, 0);
        while (enumerator.MoveTo(vertex))
        {
            while (enumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested) return;

                var onIsland = await IsOnIslandAsync(network, labels, costFunction, enumerator.EdgeId,
                    IsOnIslandAlready, cancellationToken);
                if (onIsland == null) continue; // edge cannot be traversed by profile.
                if (!onIsland.Value) continue; // edge is not on island.

                islands.SetEdgeOnIsland(enumerator.EdgeId);
            }

            vertex = new VertexId(tileId, vertex.LocalId + 1);
        }

        if (cancellationToken.IsCancellationRequested) return;
        islands.SetTileDone(tileId);
        return;

        bool? IsOnIslandAlready(IEdgeEnumerator e)
        {
            return islands.IsEdgeOnIsland(e, tileId);
        }
    }

    internal static async Task<bool?> IsOnIslandAsync(RoutingNetwork network, IslandLabels labels, ICostFunction costFunction, EdgeId edgeId,
        Func<IEdgeEnumerator, bool?>? isOnIslandAlready = null, CancellationToken cancellationToken = default)
    {
        // when we get here the edge either has no island yet or the current label is not final yet.
        var edgeEnumerator = network.GetEdgeEnumerator();
        var backwardEdgeEnumerator = network.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(edgeId, true);
        var canForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        backwardEdgeEnumerator.MoveTo(edgeId, false);
        var canBackward = costFunction.GetIslandBuilderCost(backwardEdgeEnumerator);

        // test if the edge can be traversed, if not return undefined value.
        if (!canForward && !canBackward) return null;

        // see if the edge already has a label.
        if (!labels.TryGetWithDetails(edgeId, out var rootIsland))
        {
            // check if the neighbour has a status already we can use.
            var onIslandAlready = isOnIslandAlready?.Invoke(edgeEnumerator);
            if (onIslandAlready != null)
            {
                if (onIslandAlready.Value)
                {
                    rootIsland = labels.AddNew(edgeId, false);
                }
                else
                {
                    rootIsland = labels.AddTo(IslandLabels.NotAnIslandLabel, edgeId);
                }
            }
            else
            {
                // create the root island.
                rootIsland = labels.AddNew(edgeId);
            }
        }
        if (rootIsland.size >= network.IslandManager.MaxIslandSize)
        {
            if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                throw new Exception("A large island without the not-an-island label should not exist");
            return false;
        }
        if (!rootIsland.statusNotFinal) return true; // the island can never ever get bigger anymore.

        // do a breadth-first search and stop when the island is big enough OR the search stops and the edge is on an island.
        // update any useful info about edges and their island labels along the way.
        var heap =
            new BinaryHeap<((EdgeId id, bool forward) edge, byte? turn)>();
        if (canForward) heap.Push(((edgeId, true), edgeEnumerator.HeadOrder), 1);
        if (canBackward) heap.Push(((edgeId, false), edgeEnumerator.TailOrder), 1);
        var visits = new HashSet<(EdgeId id, bool forward)>();
        while (heap.Count > 0)
        {
            // visit the next edge.
            var (currentEdge, currentTurn) = heap.Pop(out var hops);
            if (!visits.Add(currentEdge)) continue;

            // calculate the costs.
            if (!edgeEnumerator.MoveTo(currentEdge.id, currentEdge.forward))
                throw new Exception("Enumeration attempted to an edge that does not exist");
            var currentCanForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
            if (!currentCanForward)
                throw new Exception("Queued edge always has to be traversable in the queued direction");

            // notify usage of vertex before loading neighbours
            await network.UsageNotifier.NotifyVertex(network, edgeEnumerator.Tail, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return true;

            // enumerate the neighbours and see if the label propagates.
            if (!edgeEnumerator.MoveTo(edgeEnumerator.Head)) throw new Exception("Vertex does not exist");
            while (edgeEnumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested) return true;
                if (edgeEnumerator.EdgeId == currentEdge.id) continue; // u-turn.

                // determine cost and see if neighbour is worth looking at.
                var neighbourCanForward =
                    costFunction.GetIslandBuilderCost(edgeEnumerator, new[] { (currentEdge.id, currentTurn) });
                backwardEdgeEnumerator.MoveTo(edgeEnumerator.EdgeId, !edgeEnumerator.Forward);
                var neighbourCanBackward =
                    costFunction.GetIslandBuilderCost(backwardEdgeEnumerator);
                if (!neighbourCanForward && !neighbourCanBackward) continue;

                var neighbourBackward = (backwardEdgeEnumerator.EdgeId, backwardEdgeEnumerator.HeadOrder);
                backwardEdgeEnumerator.MoveTo(currentEdge.id, !currentEdge.forward);
                var currentCanBackward =
                    costFunction.GetIslandBuilderCost(backwardEdgeEnumerator, new[] { neighbourBackward });

                // get label for the current edge.
                if (!labels.TryGetWithDetails(currentEdge.id, out var currentLabelDetails))
                    throw new Exception("Current should already have been assigned a label");
                if (!currentLabelDetails.statusNotFinal) continue;
                var currentLabel = currentLabelDetails.label;

                // if the neighbour has a final status, no need to continue.
                var neighbourIsFinal = false;

                var canTurnForward = currentCanForward && neighbourCanForward; // turn current -> vertex -> neighbour is possible or not.
                var canTurnBackward = neighbourCanBackward && currentCanBackward; // turn neighbour -> vertex -> current is possible or not.

                if (!canTurnForward && !canTurnBackward) continue;

                if (canTurnForward && canTurnBackward)
                {
                    if (!labels.TryGet(edgeEnumerator.EdgeId, out var neighbourLabel))
                    {
                        // check if the neighbour has a status already we can use.
                        var onIslandAlready = isOnIslandAlready?.Invoke(edgeEnumerator);
                        if (onIslandAlready != null)
                        {
                            neighbourIsFinal = true;

                            // check and verify status
                            if (onIslandAlready.Value)
                            {
                                // neighbour has a known status and is on an island.
                                (neighbourLabel, _, _) = labels.AddNew(edgeEnumerator.EdgeId, false);
                            }
                            else
                            {
                                // neighbour has a known status and is not on an island.
                                neighbourLabel = IslandLabels.NotAnIslandLabel;
                                labels.AddTo(neighbourLabel, edgeEnumerator.EdgeId);
                            }

                            // neighbour already has a label and is connected to the current edge.
                            labels.Merge(currentLabel, neighbourLabel);
                        }
                        else
                        {
                            // neighbour has no label or no known status, just assign it the same as the current edge.
                            labels.AddTo(currentLabel, edgeEnumerator.EdgeId);
                        }
                    }
                    else
                    {
                        // neighbour already has a label and is connected to the current edge.
                        labels.Merge(currentLabel, neighbourLabel);
                    }

                    // test the original root island, it could now be big enough.
                    if (labels.TryGetWithDetails(edgeId, out rootIsland))
                    {
                        if (rootIsland.size >= network.IslandManager.MaxIslandSize)
                        {
                            if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                                throw new Exception("A large island without the not-an-island label should not exist");
                            return false;
                        }
                        if (!rootIsland.statusNotFinal)
                            return true; // the island can never ever get bigger anymore.
                    }
                }
                else
                {
                    // current is connected to neighbour but only in one way.

                    // get or determine neighbour label.
                    if (!labels.TryGet(edgeEnumerator.EdgeId, out var neighbourLabel))
                    {
                        // check if the neighbour has a status already we can use.
                        var onIslandAlready = isOnIslandAlready?.Invoke(edgeEnumerator);
                        if (onIslandAlready != null)
                        {
                            neighbourIsFinal = true;

                            // check and verify status
                            if (onIslandAlready.Value)
                            {
                                // neighbour has a known status and is on an island.
                                (neighbourLabel, _, _) = labels.AddNew(edgeEnumerator.EdgeId, false);
                            }
                            else
                            {
                                // neighbour has a known status and is not on an island.
                                neighbourLabel = IslandLabels.NotAnIslandLabel;
                                labels.AddTo(neighbourLabel, edgeEnumerator.EdgeId);
                            }
                        }
                        else
                        {
                            // neighbour has no label or no known status, just assign it the same as the current edge.
                            (neighbourLabel, _, _) = labels.AddNew(edgeEnumerator.EdgeId);
                        }
                    }

                    var madeConnection =
                        // connect current label to neighbour label.
                        canTurnForward
                            ? labels.ConnectTo(currentLabel, neighbourLabel)
                            :
                            // connect neighbour label to current label.
                            labels.ConnectTo(neighbourLabel, currentLabel);

                    // test the original root island, it could now be big enough.
                    if (madeConnection && labels.TryGetWithDetails(edgeId, out rootIsland))
                    {
                        if (rootIsland.size >= network.IslandManager.MaxIslandSize)
                        {
                            if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                                throw new Exception("A large island without the not-an-island label should not exist");
                            return false;
                        }

                        if (!rootIsland.statusNotFinal)
                            return true; // the island can never ever get bigger anymore.
                    }
                }

                // do no queue neighbours when not forward connected or when it already has a final label.
                if (!canTurnForward || neighbourIsFinal) continue;

                // add the neighbour to the queue.
                var neighbourHops = hops + 1;
                if (neighbourHops <= network.IslandManager.MaxIslandSize)
                {
                    heap.Push(((edgeEnumerator.EdgeId, edgeEnumerator.Forward), edgeEnumerator.HeadOrder),
                        neighbourHops);
                }
            }
        }

        // test again.
        if (labels.TryGetWithDetails(edgeId, out rootIsland))
        {
            if (rootIsland.size >= network.IslandManager.MaxIslandSize) return false;
            if (!rootIsland.statusNotFinal) return true; // the island can never ever get bigger anymore.
        }

        // island cannot get bigger anymore.
        // it has also not reached the min size.
        labels.SetAsComplete(rootIsland.label);
        return true;
    }
}
