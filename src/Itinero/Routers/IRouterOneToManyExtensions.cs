using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers
{
    public static class IRouterOneToManyExtensions
    {
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterOneToMany routerOneToMany)
        {
            var settings = routerOneToMany.Settings;
            var routerDb = routerOneToMany.RouterDb;
            var source = routerOneToMany.Source;
            var targets = routerOneToMany.Targets.ToUndirected();
            
            var profile =  settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = source.sp.LocationOnNetwork(routerDb);
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
            
            var paths = Dijkstra.Default.Run(routerDb, source.sp, targets,
                profileHandler.GetForwardWeight,
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