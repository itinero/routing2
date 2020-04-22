using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero
{
    /// <summary>
    /// A one to one router.
    /// </summary>
    public class RouterOneToOne
    {
        internal Router Router { get; set; }
        
        internal (SnapPoint sp, bool? direction) Source { get; set; }
        
        internal (SnapPoint sp, bool? direction) Target { get; set; }        
        
        /// <summary>
        /// Configures a route with the given arrival location.
        /// </summary>
        /// <param name="point">The departure location.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>A router.</returns>
        public RouterOneToOne To(SnapPoint point, bool? direction = null)
        {
            this.Target = (point, direction);
            return this;
        }
        
        /// <summary>
        /// Calculates the route.
        /// </summary>
        /// <returns>The calculated route.</returns>
        public Result<Route> Calculate()
        {
            var routerDb = this.Router.RouterDb;
            var settings = this.Router.Settings;
            
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = this.Source.sp.LocationOnNetwork(routerDb);
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
            var path = Dijkstra.Default.Run(routerDb, this.Source.sp, this.Target.sp,
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
    
    /// <summary>
    /// Contains extension methods related to the one to one router.
    /// </summary>
    public static class RouterOneToOneExtensions
    {
        /// <summary>
        /// Configures a route with the given departure location.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="point">The departure location.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>A router.</returns>
        public static RouterOneToOne From(this Router router, SnapPoint point, bool? direction = null)
        {
            return new RouterOneToOne()
            {
                Router = router,
                Source = (point, direction)
            };
        }
    }
}