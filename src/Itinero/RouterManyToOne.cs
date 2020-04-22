using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero
{
    /// <summary>
    /// A many to one router.
    /// </summary>
    public class RouterManyToOne
    {
        internal Router Router { get; set; }
        
        internal IReadOnlyList<SnapPoint> Sources { get; set; }
        
        internal (SnapPoint sp, bool? direction) Target { get; set; }
        
        /// <summary>
        /// Configures a route with the given arrival location.
        /// </summary>
        /// <param name="point">The departure location.</param>
        /// <returns>A route.</returns>
        public RouterManyToOne To(SnapPoint point)
        {
            this.Target = (point, null);
            return this;
        }

        /// <summary>
        /// Calculates the many to one routes.
        /// </summary>
        /// <returns>The routes.</returns>
        public IReadOnlyList<Result<Route>> Calculate()
        {
            var settings = this.Router.Settings;
            var sources = this.Sources;
            var target = this.Target;
            var routerDb = this.Router.RouterDb;
            
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
            
            var paths = Dijkstra.Default.Run(routerDb, target.sp, sources,
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

    /// <summary>
    /// Contains extension methods for the many to one router.
    /// </summary>
    public static class ManyToOneRouterExtensions
    {
        /// <summary>
        /// Configures a many to one route.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="points">The departure locations.</param>
        /// <returns>A route.</returns>
        public static RouterManyToOne From(this Router router, IReadOnlyList<SnapPoint> points)
        {
            return new RouterManyToOne()
            {
                Router = router,
                Sources = points
            };
        }
    }
}