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

        /// <summary>
        ///     Builds a route from the given path for the given profile.
        /// </summary>
        /// <param name="db">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="path">The path.</param>
        /// <param name="forward">The forward flag.</param>
        /// <returns>The route.</returns>
        public Result<Route> Build(RoutingNetwork db, Profile profile, Path path, bool forward = true) {
            var edgeEnumerator = db.GetEdgeEnumerator();
            var route = new Route {Profile = profile.Name};

            foreach (var (edge, direction, offset1, offset2) in path) {
                
                edgeEnumerator.MoveToEdge(edge, direction == forward /* AKA: if forward is false, flip direction */);

                var attributes = edgeEnumerator.Attributes;
                var factor = profile.Factor(attributes);
                var distance = edgeEnumerator.EdgeLength() / 100.0;
                distance = (offset2 - offset1) / (double) ushort.MaxValue * distance;
                route.TotalDistance += distance;

                var speed = direction ? factor.ForwardSpeedMeterPerSecond : factor.BackwardSpeedMeterPerSecond;
                if (speed <= 0) {
                    // Something is wrong here
                    throw new ArgumentException("Got a negative speed!");
                }

                var time = distance / speed;
                route.TotalTime += time;


                // add shape points to route.
                using var shapeBetween = edgeEnumerator.GetShapeBetween(offset1, offset2).GetEnumerator();
                // skip first if there are already previous edges.
                if (route.Shape.Count > 0 && offset1 == 0) {
                    shapeBetween.MoveNext();
                }

                route.ShapeMeta.Add(new Route.Meta {
                    Shape = route.Shape.Count,
                    Attributes = attributes,
                    AttributesDirection = direction,
                    Distance = distance,
                    Profile = profile.Name,
                    Time = time
                });

                while (shapeBetween.MoveNext()) {
                    route.Shape.Add(shapeBetween.Current);
                }
            }

            return route;
        }
    }
}