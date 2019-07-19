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
            var route = new Route();
            route.Profile = profile.Name;

            for (var i = 0; i < path.Count; i++)
            {
                var segment = path[i];
                
                // determine offsets if any.
                ushort offset1 = 0;
                ushort offset2 = ushort.MaxValue;
                if (i == 0)
                {
                    offset1 = path.Offset1;
                    if (path.Count == 1)
                    {
                        offset2 = path.Offset2;
                    }
                }
                else if (i == path.Count - 1)
                {
                    offset2 = path.Offset2;
                }

                var attributes = db.GetAttributes(segment.edge);
                var factor = profile.Factor(attributes);
                if (forward)
                {
                    edgeEnumerator.MoveToEdge(segment.edge, segment.forward);
                }
                else
                {
                    edgeEnumerator.MoveToEdge(segment.edge, !segment.forward);
                }
                var distance = edgeEnumerator.EdgeLength() / 100.0;
                distance = ((offset2 - offset1) / (double)ushort.MaxValue) * distance;
                route.TotalDistance += distance;
                if (segment.forward)
                {
                    if (factor.BackwardSpeed > 0)
                    { // ok factor makes sense.
                        route.TotalTime += distance / factor.ForwardSpeed;
                    }
                }
                else
                {
                    if (factor.BackwardSpeed > 0)
                    { // ok factor makes sense.
                        route.TotalTime += distance / factor.BackwardSpeed;
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