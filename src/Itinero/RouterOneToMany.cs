using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero
{
    /// <summary>
    /// A one to many router.
    /// </summary>
    public class RouterOneToMany
    {
        internal Router Router { get; set; }
        
        internal (SnapPoint sp, bool? direction) Source { get; set; }
        
        internal IReadOnlyList<SnapPoint> Targets { get; set; }

        public IReadOnlyList<Result<Route>> Calculate()
        {
            var settings = this.Router.Settings;
            var routerDb = this.Router.RouterDb;
            var source = this.Source;
            var targets = this.Targets;
            
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
    
    /// <summary>
    /// Contains extension methods related to the one to many router.
    /// </summary>
    public static class RouterOneToManyExtensions
    {
        /// <summary>
        /// Creates a one to many router.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="points">The destinations.</param>
        /// <returns>The router.</returns>
        public static RouterOneToMany To(this RouterOneToOne router, IReadOnlyList<SnapPoint> points)
        {
            return new RouterOneToMany()
            {
                Router = router.Router,
                Source = router.Source,
                Targets = points
            };
        }
    }
}