using System.Collections.Generic;
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
    public static class RoutePromiseExtensions
    {
        
    }
}