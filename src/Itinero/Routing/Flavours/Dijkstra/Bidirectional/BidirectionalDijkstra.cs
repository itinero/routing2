using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;


internal class BidirectionalDijkstra
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly BidirectionalDijkstraForward _forward;
    private readonly BidirectionalDijkstraBackward _backward;
    private ICostFunction _costFunction;

    internal BidirectionalDijkstra(RoutingNetwork routingNetwork)
    {
        _routingNetwork = routingNetwork;

        _backward = new BidirectionalDijkstraBackward(this);
        _forward = new BidirectionalDijkstraForward(this);
    }

    public static BidirectionalDijkstra ForNetwork(RoutingNetwork routingNetwork) => new(routingNetwork);

    public async Task<(Path? path, double cost)> RunAsync(SnapPoint origin,
        SnapPoint destination, ICostFunction costFunction, Func<VertexId, Task<bool>>? settled = null,
        Func<VertexId, Task<bool>>? queued = null)
    {
        _costFunction = costFunction;

        (uint forward, uint backward, double cost, Path? singleHopPath) best = (uint.MaxValue, uint.MaxValue, double.MaxValue, null);
        if (_routingNetwork.TrySingleHop(origin, destination, costFunction, out var singleHopPath, out var singleHopCost))
        {
            best = (uint.MaxValue, uint.MaxValue, singleHopCost, singleHopPath);
        }

        _backward.Push(costFunction, destination, false);
        _forward.Push(costFunction, origin, true);

        var forwardDone = false;
        var backwardDone = false;
        var forwardCost = 0d;
        var backwardCost = 0d;
        while (!forwardDone || !backwardDone)
        {
            if (!forwardDone)
            {
                var (p, v, c) = _forward.Pop();
                forwardCost = c;
                if (p != uint.MaxValue)
                {
                    if (_backward.TryGetVisit(v.vertex, out var backwardVisit))
                    {
                        var cost = c + backwardVisit.cost;
                        if (cost < best.cost)
                        {
                            best = (p, backwardVisit.p, cost, null);
                        }
                    }

                    if (settled != null) await settled(v.vertex);

                    if (!_forward.Step(p, v, c)) forwardDone = true;
                }
                else
                {
                    forwardDone = true;
                }
            }
            if (!backwardDone)
            {
                var (p, v, c) = _backward.Pop();
                backwardCost = c;
                if (p != uint.MaxValue)
                {
                    if (_forward.TryGetVisit(v.vertex, out var forwardVisit))
                    {
                        var cost = c + forwardVisit.cost;
                        if (cost < best.cost)
                        {
                            best = (forwardVisit.p, p, cost, null);
                        }
                    }

                    if (settled != null) await settled(v.vertex);

                    if (!_backward.Step(p, v, c)) backwardDone = true;
                }
                else
                {
                    backwardDone = true;
                }
            }

            if (best.cost < (forwardCost + backwardCost)) break;
        }

        if (best.cost >= double.MaxValue) return (null, double.MaxValue);
        if (best.forward == uint.MaxValue) return (best.singleHopPath, best.cost);

        var forwardPath = _forward.GetPathToVisit(best.forward);
        var backwardPath = _backward.GetPathToVisit(best.backward);
        forwardPath.Append(backwardPath.InvertDirection());

        forwardPath.Offset1 = forwardPath.First.direction ? origin.Offset : (ushort)(ushort.MaxValue - origin.Offset);
        forwardPath.Offset2 = forwardPath.Last.direction
            ? destination.Offset
            : (ushort)(ushort.MaxValue - destination.Offset);

        return (forwardPath, best.cost);
    }

    internal class BidirectionalDijkstraForward : DijkstraAlgorithm
    {
        private readonly BidirectionalDijkstra _bidirectionalDijkstra;

        internal BidirectionalDijkstraForward(BidirectionalDijkstra bidirectionalDijkstra)
            : base(bidirectionalDijkstra._routingNetwork)
        {
            _bidirectionalDijkstra = bidirectionalDijkstra;
        }


        protected override bool OnQueued(uint visit, EdgeId edge, (double cost, double turnCost) edgeCost, VertexId vertex, double totalCost)
        {
            return true;
        }

        protected override bool OnSettled(uint visit, VertexId vertex, double cost)
        {
            return true;
        }

        protected override (double cost, double turnCost) GetCost(RoutingNetworkEdgeEnumerator edgeEnumerator, IEnumerable<(EdgeId edge, byte? turn)> previousEdges)
        {
            return _bidirectionalDijkstra._costFunction.GetCost(edgeEnumerator, true, previousEdges);
        }
    }

    internal class BidirectionalDijkstraBackward : DijkstraAlgorithm
    {
        private readonly BidirectionalDijkstra _bidirectionalDijkstra;

        internal BidirectionalDijkstraBackward(BidirectionalDijkstra bidirectionalDijkstra)
            : base(bidirectionalDijkstra._routingNetwork)
        {
            _bidirectionalDijkstra = bidirectionalDijkstra;
        }


        protected override bool OnQueued(uint visit, EdgeId edge, (double cost, double turnCost) edgeCost, VertexId vertex, double totalCost)
        {
            return true;
        }

        protected override bool OnSettled(uint visit, VertexId vertex, double cost)
        {
            return true;
        }

        protected override (double cost, double turnCost) GetCost(RoutingNetworkEdgeEnumerator edgeEnumerator, IEnumerable<(EdgeId edge, byte? turn)> previousEdges)
        {
            return _bidirectionalDijkstra._costFunction.GetCost(edgeEnumerator, false, previousEdges);
        }
    }
}
