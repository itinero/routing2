using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Algorithms;
using Itinero.Algorithms.DataStructures;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers
{
    /// <summary>
    /// Many to many extensions.
    /// </summary>
    public static class IRouterManyToManyExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="manyToManyRouter">The router.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IReadOnlyList<IReadOnlyList<Result<Route>>> Calculate(this IRouterManyToMany manyToManyRouter)
        {
            var settings = manyToManyRouter.Settings;
            var routerDb = manyToManyRouter.RouterDb;
            var sources = manyToManyRouter.Sources;
            var targets = manyToManyRouter.Targets;

            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            var maxBox = settings.MaxBoxFor(routerDb, sources);

            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;

                if (routerDb == null) throw new Exception("Router cannot be null here.");
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
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="manyToManyWeightRouter">The router.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Result<double?[][]> Calculate(this IRouterWeights<IRouterManyToMany> manyToManyWeightRouter)
        {
            var manyToManyRouter = manyToManyWeightRouter.Router;
            
            var settings = manyToManyRouter.Settings;
            var routerDb = manyToManyRouter.RouterDb;
            var sources = manyToManyRouter.Sources;
            var targets = manyToManyRouter.Targets;

            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            var maxBox = settings.MaxBoxFor(routerDb, sources);

            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;

                if (routerDb == null) throw new Exception("Router cannot be null here.");
                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex))
                {
                    return true;
                }

                return false;
            }

            var results = new double?[sources.Count][];
            var undirectedTargets = targets.ToUndirected();
            var edgeEnumerator = routerDb.GetEdgeEnumerator();
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

                var sourceResults = new double?[paths.Length];
                for (var r = 0; r < sourceResults.Length; r++)
                {
                    var path = paths[r];
                    if (path == null)
                    {
                        sourceResults[r] = null;
                    }
                    else
                    {
                        sourceResults[r] = path.Weight((edge) =>
                        {
                            if (!edgeEnumerator.MoveToEdge(edge.edge, edge.direction)) throw new InvalidDataException($"Edge {edge} not found!");

                            return profileHandler.GetForwardWeight(edgeEnumerator);
                        });
                    }
                }

                results[s] = sourceResults;
            }

            return results;
        }
    }
}