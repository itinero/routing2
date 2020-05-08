using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers
{
    public static class IRouterOneToOneExtensions
    {
        public static Result<Route> Calculate(this IRouterOneToOne oneToOneRouter)
        {
            var routerDb = oneToOneRouter.RouterDb;
            var settings = oneToOneRouter.Settings;
            
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = oneToOneRouter.Source.sp.LocationOnNetwork(routerDb);
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
            
            // run dijkstra.
            var path = Dijkstra.Default.Run(routerDb, oneToOneRouter.Source.sp, oneToOneRouter.Target.sp,
                profileHandler.GetForwardWeight,
                settled: (v) =>
                {
                    routerDb.UsageNotifier.NotifyVertex(v);
                    return checkMaxDistance(v);
                });

            if (path == null) return new Result<Route>($"Route not found!");
            return RouteBuilder.Default.Build(routerDb, profile, path);
        }
    }
}