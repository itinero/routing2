using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers.ManyToMany
{
    public class RouterManyToMany
    {
        internal Router Router { get; set; }
        
        internal IReadOnlyList<(SnapPoint sp, bool? direction)> Sources { get; set; }
        
        internal IReadOnlyList<(SnapPoint sp, bool? direction)> Targets { get; set; }

        public IReadOnlyList<IReadOnlyList<Result<Route>>> Calculate()
        {
            var settings = this.Router.Settings;
            var routerDb = this.Router.RouterDb;
            var sources = this.Sources;
            var targets = this.Targets;

            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? maxBox =
                null;
            if (settings.MaxDistance < double.MaxValue)
            {
                foreach (var source in sources)
                {
                    var sourceLocation = source.sp.LocationOnNetwork(routerDb);
                    var sourceBox = sourceLocation.BoxAround(settings.MaxDistance);
                    maxBox = maxBox?.Expand(sourceBox) ?? sourceBox;
                }
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

            var results = new IReadOnlyList<Result<Route>>[sources.Count];
            var undirectedTargets = targets.ToUndirected();
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                var paths = Dijkstra.Default.Run(routerDb, source.sp, undirectedTargets,
                    profileHandler.GetForwardWeight,
                    settled: (v) =>
                    {
                        routerDb.UsageNotifier.NotifyVertex(v);
                        return checkMaxDistance(v);
                    });

                var sourceResults = new Result<Route>[paths.Length];
                for (var r = 0; r < sourceResults.Length; r++)
                {
                    var path = paths[r];
                    if (path == null)
                    {
                        sourceResults[r] = new Result<Route>($"Routes not found!");
                    }
                    else
                    {
                        sourceResults[r] = RouteBuilder.Default.Build(routerDb, profile, path);
                    }
                }

                results[s] = sourceResults;
            }

            return results;
        }
    }

    /// <summary>
    /// Contains extension methods for the many to one router.
    /// </summary>
    public static class RouterManyToManyExtensions
    {
        /// <summary>
        /// Configures a many to one route.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="points">The departure locations.</param>
        /// <returns>A route.</returns>
        public static RouterManyToMany To(this RouterManyToOne router, IReadOnlyList<SnapPoint> points)
        {
            return new RouterManyToMany()
            {
                Router = router.Router,
                Sources = router.Sources,
                Targets = points.ToDirected()
            };
        }
    }
}