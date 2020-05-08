using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers
{
    /// <summary>
    /// Many to one extensions.
    /// </summary>
    public static class IRouterManyToOneExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerManyToOne">The router.</param>
        /// <returns>The routes.</returns>
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterManyToOne routerManyToOne)
        {
            var settings = routerManyToOne.Settings;
            var sources = routerManyToOne.Sources;
            var target = routerManyToOne.Target;
            var routerDb = routerManyToOne.RouterDb;
            
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = target.sp.LocationOnNetwork(routerDb);
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? maxBox =
                null;
            if (settings.MaxDistance < double.MaxValue)
            {
                maxBox = sourceLocation.BoxAround(settings.MaxDistance);
            }
            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;
                    
                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex))
                {
                    return true;
                }

                return false;
            }
            
            var paths = Dijkstra.Default.Run(routerDb, target.sp, sources.ToUndirected(),
                profileHandler.GetBackwardWeight,
                settled: (v) =>
                {
                    routerDb.UsageNotifier.NotifyVertex(v);
                    return checkMaxDistance(v);
                });

            var results = new Result<Route>[paths.Length];
            for (var r = 0; r < results.Length; r++)
            {
                var path = paths[r];
                if (path == null)
                {
                    results[r] = new Result<Route>($"Routes not found!");
                }
                else
                {
                    results[r] = RouteBuilder.Default.Build(routerDb, profile, path);
                }
            }
            return results;
        }
    }
}