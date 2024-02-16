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
        Console.WriteLine($"Building islands for: {tileId}");
        var startTicks = DateTime.Now.Ticks;
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
                var ticks = DateTime.Now.Ticks;
                Console.WriteLine($"Building islands for: {tileId} in {new TimeSpan(ticks - startTicks).TotalSeconds}");
                if (onIsland == null) continue; // edge cannot be traversed by profile.
                if (!onIsland.Value) continue; // edge is not on island.

                islands.SetEdgeOnIsland(enumerator.EdgeId);
            }

            vertex = new VertexId(tileId, vertex.LocalId + 1);
        }

        if (cancellationToken.IsCancellationRequested) return;
        islands.SetTileDone(tileId);
        var endTicks = DateTime.Now.Ticks;
        Console.WriteLine($"Done building islands for: {tileId} in {new TimeSpan(endTicks - startTicks).TotalSeconds}");
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
        var localCostFunction = costFunction.GetIslandBuilderWeightFunc();
        var edgeEnumerator = network.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(edgeId, true);
        var cost = localCostFunction(edgeEnumerator);

        // test if the edge can be traversed, if not return undefined value.
        if (cost is { forward: false, backward: false }) return null;
        
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
        heap.Push(((edgeId, true),edgeEnumerator.HeadOrder), 1);
        heap.Push(((edgeId, false),edgeEnumerator.TailOrder), 1);
        var visits = new HashSet<(EdgeId id, bool forward)>();
        while (heap.Count > 0)
        {
            // visit the next edge.
            var (currentEdge, currentTurn) = heap.Pop(out var hops);
            
            // calculate the costs.
            if (!edgeEnumerator.MoveTo(currentEdge.id, currentEdge.forward))
                throw new Exception("Enumeration attempted to an edge that does not exist");
            var currentCostForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
            if (!currentCostForward)
                throw new Exception("A queued edge should always be accessible in forward direction");
            if (!edgeEnumerator.MoveTo(currentEdge.id, !currentEdge.forward))
                throw new Exception("Enumeration attempted to an edge that does not exist");
            var currentCostBackward = costFunction.GetIslandBuilderCost(edgeEnumerator);
            
            await network.UsageNotifier.NotifyVertex(network, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return true;
            if (!visits.Add(currentEdge)) continue;

            // enumerate the neighbours and see if the label propagates.
            if (!edgeEnumerator.MoveTo(currentVertex)) continue;
            while (edgeEnumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested) return true;
                if (edgeEnumerator.EdgeId == currentEdge.id) continue; // u-turn.

                // determine cost and see if neighbour is worth looking at.
                var costToNeighbour =
                    costFunction.GetIslandBuilderCost(true, edgeEnumerator, new[] { (currentEdge.id, currentTurn) });
                var neighbourCost = localCostFunction(edgeEnumerator);
                if (neighbourCost is { backward: false, forward: false }) continue;

                // get label for the current edge.
                if (!labels.TryGetWithDetails(currentEdge.id, out var currentLabelDetails))
                    throw new Exception("Current should already have been assigned a label");
                if (!currentLabelDetails.statusNotFinal) continue;
                var currentLabel = currentLabelDetails.label;
                
                // if the neighbour has a final status, no need to continue.
                var neighbourIsFinal = false;

                var isForwardConnected = currentCost.forward && neighbourCost.forward;
                var isBackwardConnected = currentCost.backward && neighbourCost.backward;

                if (isForwardConnected && isBackwardConnected)
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
                        isForwardConnected
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
                if (!isForwardConnected || neighbourIsFinal)  continue;

                // add the neighbour to the queue.
                var neighbourHops = hops + 1;
                if (neighbourHops <= network.IslandManager.MaxIslandSize)
                {
                    heap.Push(((edgeEnumerator.EdgeId, edgeEnumerator.Forward), edgeEnumerator.Head, neighbourCost),
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
