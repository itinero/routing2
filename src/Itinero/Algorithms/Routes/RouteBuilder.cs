using System.Collections.Generic;
using System.Threading;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;
using Itinero.LocalGeo;
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
        /// <returns>The route.</returns>
        public Result<Route> Build(RouterDb db, Profile profile, Path path)
        {
            var edgeEnumerator = db.GetEdgeEnumerator();
            var route = new Route();

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
                
                route.Shape.AddRange(db.GetShapeBetween(segment, offset1, offset2));
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