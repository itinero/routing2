using System.Threading;
using Itinero.Algorithms.DataStructures;
using Itinero.Profiles;

namespace Itinero.Algorithms.Routes
{
    /// <summary>
    /// Default route builder implementation.
    /// </summary>
    public class RouteBuilder : IRouteBuilder
    {
        /// <summary>
        /// Builds a route from the given path for the given profile.
        /// </summary>
        /// <param name="db">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="path">The path.</param>
        /// <param name="forward">The forward flag.</param>
        /// <returns>The route.</returns>
        public Result<Route> Build(RouterDb db, Profile profile, Path path, bool forward = true)
        {
            var edgeEnumerator = db.GetEdgeEnumerator();
            var route = new Route {Profile = profile.Name};

            foreach (var (edge, direction, offset1, offset2) in path)
            {
                if (forward)
                {
                    edgeEnumerator.MoveToEdge(edge, direction);
                }
                else
                {
                    edgeEnumerator.MoveToEdge(edge, !direction);
                }
                var attributes = edgeEnumerator.Attributes;
                var factor = profile.Factor(attributes);
                var distance = edgeEnumerator.EdgeLength() / 100.0;
                distance = ((offset2 - offset1) / (double)ushort.MaxValue) * distance;
                route.TotalDistance += distance;
                if (direction)
                {
                    if (factor.ForwardSpeed > 0)
                    { // ok factor makes sense.
                        route.TotalTime += distance / factor.ForwardSpeedMeterPerSecond;
                    }
                }
                else
                {
                    if (factor.BackwardSpeed > 0)
                    { // ok factor makes sense.
                        route.TotalTime += distance / factor.BackwardSpeedMeterPerSecond;
                    }
                }

                route.Shape.AddRange(edgeEnumerator.GetShapeBetween(offset1, offset2));
            }
            
            return route;
        }
        private static readonly ThreadLocal<RouteBuilder> DefaultLazy = new ThreadLocal<RouteBuilder>(() => new RouteBuilder());
        
        /// <summary>
        /// Gets the default instance (for the local thread).
        /// </summary>
        public static RouteBuilder Default => DefaultLazy.Value;
    }
}