using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Routing.DataStructures;

namespace Itinero.Network.Search.Islands;

internal class IslandBuilder : IIslandBuilder
{
    private readonly IslandBuilderSettings _settings;
    private readonly RoutingNetwork _routingNetwork;

    internal IslandBuilder(RoutingNetwork routingNetwork, IslandBuilderSettings settings)
    {
        _routingNetwork = routingNetwork;
        _settings = settings;
    }

    internal IslandLabels GetLabels()
    {
        if (!_routingNetwork._islandLabels.TryGetValue(_settings.Profile.Name, out var labels))
        {
            labels = new IslandLabels();
            _routingNetwork._islandLabels[_settings.Profile.Name] = labels;
        }

        return labels;
    }

    public bool IsOnIsland(EdgeId edgeId, bool forward)
    {
        // make sure there are labels for the used profile.
        if (!_routingNetwork._islandLabels.TryGetValue(_settings.Profile.Name, out var labels))
        {
            labels = new IslandLabels();
            _routingNetwork._islandLabels[_settings.Profile.Name] = labels;
        }

        // see if the edge already has a label.
        if (labels.TryGetWithDetails((edgeId, forward), out var island))
        {
            if (island.size >= _settings.MinIslandSize) return false;
            if (!island.canGrow) return true; // the island can never ever get bigger anymore.
        }
        else
        {
            // create the root island.
            labels.AddNew((edgeId, forward));
        }

        // when we get here the edge either has no island yet or the current label is not final yet.
        var costFunction = _routingNetwork.GetCostFunctionFor(_settings.Profile);
        var localCostFunction = costFunction.GetIslandBuilderWeightFunc();
        var edgeEnumerator = _routingNetwork.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(edgeId, true);
        var cost = localCostFunction(edgeEnumerator);

        // test if the edge cannot be traversed in the requested direction.
        if (forward && !cost.forward) return true;
        if (!forward && !cost.backward) return true;

        // do a breadth-first search and stop when the island is big enough.
        // update any useful info about edges and their island labels along the way.
        var heap = new BinaryHeap<((EdgeId id, bool forward) edge, VertexId vertex, (bool forward, bool backward) cost)>();
        heap.Push(((edgeId, forward), edgeEnumerator.Head, cost), 1);
        var visits = new HashSet<(EdgeId id, bool forward)>();
        while (heap.Count > 0)
        {
            // visit the next edge.
            var (currentEdge, currentVertex, currentCost) = heap.Pop(out var hops);
            if (!visits.Add(currentEdge)) continue;

            // enumerate the neighbours and follow.
            if (!edgeEnumerator.MoveTo(currentVertex)) continue;
            while (edgeEnumerator.MoveNext())
            {
                if (edgeEnumerator.EdgeId == currentEdge.id) continue; // u-turn.

                var neighbourEdge = (edgeEnumerator.EdgeId, edgeEnumerator.Forward);
                var neighbourCost = localCostFunction(edgeEnumerator);

                var isForwardConnected = currentEdge.CanBeTraversed(currentCost) &&
                                         neighbourEdge.CanBeTraversed(neighbourCost);
                var neighbourEdgeOpposite = neighbourEdge.Invert();
                var currentEdgeOpposite = currentEdge.Invert();
                var isBackwardConnected = neighbourEdgeOpposite.CanBeTraversed(neighbourCost) &&
                                          currentEdgeOpposite.CanBeTraversed(currentCost);

                if (isForwardConnected)
                {
                    // add the neighbour to the queue.
                    var neighbourHops = hops + 1;
                    if (neighbourHops <= _settings.MinIslandSize)
                    {
                        heap.Push((neighbourEdge, edgeEnumerator.Head, neighbourCost), neighbourHops);
                    }
                }

                if (isForwardConnected && isBackwardConnected)
                {
                    // assign the same labels to both.

                    // propagate label in forward direction.
                    if (!labels.TryGet(currentEdge, out var label))
                    {
                        (label, _, _) = labels.AddNew(currentEdge);
                    }

                    if (!labels.TryGet(neighbourEdge, out var neighbourLabel))
                    {
                        // neighbour has no label yet, propagate the current label
                        labels.AddTo(label, neighbourEdge);
                    }
                    else
                    {
                        // labels were different, connect them.
                        var connected = labels.ConnectTo(label, neighbourLabel);
                        connected = connected || labels.ConnectTo(neighbourLabel, label);

                        if (connected)
                        {
                            // if connected, check if the original edge is either final or has a label big enough.
                            if (labels.TryGetWithDetails((edgeId, forward), out var rootIsland))
                            {
                                if (rootIsland.size >= _settings.MinIslandSize) return false;
                                if (!rootIsland.canGrow) return true; // the island can never ever get bigger anymore.
                            }
                        }
                    }

                    // propagate label in backward direction.
                    if (!labels.TryGet(neighbourEdgeOpposite, out var neighbourOppositeLabel))
                    {
                        (neighbourOppositeLabel, _, _) = labels.AddNew(neighbourEdgeOpposite);
                    }

                    if (!labels.TryGet(currentEdgeOpposite, out var currentOppositeLabel))
                    {
                        // neighbour has no label yet, propagate the current label
                        labels.AddTo(neighbourOppositeLabel, currentEdgeOpposite);
                    }
                    else
                    {
                        // labels were different, connect them.
                        var connected = labels.ConnectTo(neighbourOppositeLabel, currentOppositeLabel);
                        connected = connected || labels.ConnectTo(currentOppositeLabel, neighbourOppositeLabel);

                        // connect the neighbour opposite label -> current opposite label.
                        if (connected)
                        {
                            // if connected, check if the original edge is either final or has a label big enough.
                            if (labels.TryGetWithDetails((edgeId, forward), out var rootIsland))
                            {
                                if (rootIsland.size >= _settings.MinIslandSize) return false;
                                if (!rootIsland.canGrow) return true; // the island can never ever get bigger anymore.
                            }
                        }
                    }
                }
                else
                {
                    // see if we can connect current to neighbour in the direction the currently is in:
                    // currentEdge -> currentVertex -> neighbourEdge
                    if (isForwardConnected)
                    {
                        // add a new label if needed or get existing label.
                        if (!labels.TryGet(currentEdge, out var label))
                        {
                            (label, _, _) = labels.AddNew(currentEdge);
                        }

                        if (!labels.TryGet(neighbourEdge, out var neighbourLabel))
                        {
                            // neighbour has no label yet.
                            (neighbourLabel, _, _) = labels.AddNew(neighbourEdge);
                        }

                        // connect the label -> neighbour label.
                        if (labels.ConnectTo(label, neighbourLabel))
                        {
                            // if connected, check if the original edge is either final or has a label big enough.
                            if (labels.TryGetWithDetails((edgeId, forward), out var rootIsland))
                            {
                                if (rootIsland.size >= _settings.MinIslandSize) return false;
                                if (!rootIsland.canGrow) return true; // the island can never ever get bigger anymore.
                            }
                        }
                    }

                    // see if we can connect neighbour to current in the opposite directions they are currently in:
                    // neighbourEdge -> currentVertex -> currentEdge
                    if (isBackwardConnected)
                    {
                        // add a new label if needed or get existing label.
                        if (!labels.TryGet(neighbourEdgeOpposite, out var neighbourOppositeLabel))
                        {
                            (neighbourOppositeLabel, _, _) = labels.AddNew(neighbourEdgeOpposite);
                        }

                        if (!labels.TryGet(currentEdgeOpposite, out var currentOppositeLabel))
                        {
                            // current edge opposite has no label yet.
                            (currentOppositeLabel, _, _) = labels.AddNew(currentEdgeOpposite);
                        }

                        // connect the neighbour opposite label -> current opposite label.
                        if (labels.ConnectTo(neighbourOppositeLabel, currentOppositeLabel))
                        {
                            // if connected, check if the original edge is either final or has a label big enough.
                            if (labels.TryGetWithDetails((edgeId, forward), out var rootIsland))
                            {
                                if (rootIsland.size >= _settings.MinIslandSize) return false;
                                if (!rootIsland.canGrow) return true; // the island can never ever get bigger anymore.
                            }
                        }
                    }
                }
            }
        }

        if (labels.TryGetWithDetails((edgeId, forward), out island))
        {
            if (island.size >= _settings.MinIslandSize) return false;
            if (!island.canGrow) return true; // the island can never ever get bigger anymore.
        }

        // island cannot get bigger anymore.
        // it has also not reached the min size.
        labels.SetAsComplete(island.label);
        return true;
    }
}
