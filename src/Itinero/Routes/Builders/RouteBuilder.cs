using System;
using System.Threading;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routes.Paths;

namespace Itinero.Routes.Builders {
    /// <summary>
    ///     Default route builder implementation.
    /// </summary>
    public class RouteBuilder : IRouteBuilder {
        private static readonly ThreadLocal<RouteBuilder> DefaultLazy = new(() => new RouteBuilder());

        /// <summary>
        ///     Gets the default instance (for the local thread).
        /// </summary>
        public static RouteBuilder Default => DefaultLazy.Value;

        /// <inheritdoc />
        public Result<Route> Build(RoutingNetwork routingNetwork, Profile profile, Path path) {
            var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
            var route = new Route {Profile = profile.Name};

            var seenEdges = 0;
            foreach (var (edge, direction, offset1, offset2) in path) {
                
                edgeEnumerator.MoveToEdge(edge, direction);

                if (route.Shape.Count == 0) {
                    // This is the first edge of the route - we have to check for branches at the start loction
                    bool firstEdgeIsFullyContained;
                    if (direction) {
                        // Forward: we look to offset1
                        firstEdgeIsFullyContained = offset1 == 0;
                    }
                    else {
                        firstEdgeIsFullyContained = offset2 == ushort.MaxValue;
                    }

                    if (firstEdgeIsFullyContained) {
                        // We check for branches
                        edgeEnumerator.MoveTo(edgeEnumerator.From);

                        AddBranches(route, edgeEnumerator);

                        // Move back to the start point
                        edgeEnumerator.MoveToEdge(edge, direction);
                    }
                }


                var attributes = edgeEnumerator.Attributes;
                var factor = profile.Factor(attributes);
                var distance = edgeEnumerator.EdgeLength() / 100.0;
                distance = (offset2 - offset1) / (double) ushort.MaxValue * distance;
                route.TotalDistance += distance;

                var speed = direction ? factor.ForwardSpeedMeterPerSecond : factor.BackwardSpeedMeterPerSecond;
                if (speed <= 0) {
                    // Something is wrong here
                    throw new ArgumentException("Speed is zero! Did you pass a wrong profile to the route builder?");
                }

                var time = distance / speed;
                route.TotalTime += time;


                // add shape points to route.
                using var shapeBetween = edgeEnumerator.GetShapeBetween(offset1, offset2).GetEnumerator();
                // skip first coordinate if there are already previous edges.
                if (route.Shape.Count > 0 && offset1 == 0) {
                    shapeBetween.MoveNext();
                }


                while (shapeBetween.MoveNext()) {
                    route.Shape.Add(shapeBetween.Current);
                }

                route.ShapeMeta.Add(new Route.Meta {
                    Shape = route.Shape.Count - 1,
                    Attributes = attributes,
                    AttributesAreForward = direction,
                    Distance = distance,
                    Profile = profile.Name,
                    Time = time
                });

                // Intermediate points of an edge will never have branches
                // So, to calculate branches, it is enough to only look at the last point of the edge
                // (and the first point of the first edge - a true edge case)
                // (Also note that the first and last edge might not be needed entirely, so that means we possible can ignore those branches)

                // What is the end vertex? Add its branches...
                var endVertextId = edgeEnumerator.To;
                edgeEnumerator.MoveTo(endVertextId);

                if (seenEdges + 1 == path.Count) {
                    // Hmm, this is the last edge
                    // We should add the branches of it, but only if the edge is completely contained
                    bool lastEdgeIsFullyContained;
                    if (!direction) {
                        // Backward: we look to offset1
                        lastEdgeIsFullyContained = offset1 == 0;
                    }
                    else {
                        lastEdgeIsFullyContained = offset2 == ushort.MaxValue;
                    }

                    if (lastEdgeIsFullyContained) {
                        AddBranches(route, edgeEnumerator);
                    }
                }
                else {
                    AddBranches(route, edgeEnumerator);
                }
                
                seenEdges++;
            }

            return route;
        }


        private void AddBranches(Route route, RoutingNetworkEdgeEnumerator edgeEnumerator) {
            while (edgeEnumerator.MoveNext()) {
                // Iterates over all edges of the endvertex
                // We make sure not to pick the current nor the next edge of the path
                // TODO
            }
        }
    }
}