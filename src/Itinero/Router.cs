using System.Collections.Generic;
using System.IO;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero
{
    /// <summary>
    /// A router.
    /// </summary>
    public class Router
    {
        internal Router(RouterDb routerDb, RoutingSettings settings)
        {
            this.RouterDb = routerDb;
            this.Settings = settings;
        }
        
        internal RouterDb RouterDb { get; }
        
        internal RoutingSettings Settings { get; }
    }
    
    /// <summary>
    /// Contains extension methods for the route promise.
    /// </summary>
    public static class RouterExtensions
    {
        internal static IReadOnlyList<(SnapPoint sp, bool? directed)> ToDirected(this IReadOnlyList<SnapPoint> sps)
        {
            var directedSps = new List<(SnapPoint sp, bool? directed)>();
            foreach (var sp in sps)
            {
                directedSps.Add((sp, null));
            }

            return directedSps;
        }

        internal static IReadOnlyList<SnapPoint> ToUndirected(
            this IReadOnlyList<(SnapPoint sp, bool? directed)> directedSps)
        {
            var sps = new List<SnapPoint>();
            foreach (var (sp, direction) in directedSps)
            {
                if (direction != null) throw new InvalidDataException($"{nameof(SnapPoint)} is directed cannot convert to undirected.");
                sps.Add(sp);
            }

            return sps;
        }
    }
}