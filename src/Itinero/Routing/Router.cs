using System.Collections.Generic;
using Itinero.Network;
using Itinero.Snapping;

namespace Itinero.Routing
{
    internal class Router : IRouter, IRouterOneToOne, IRouterManyToMany, IRouterManyToOne, IRouterOneToMany
    {
        internal Router(RoutingNetwork network, RoutingSettings settings)
        {
            this.Network = network;
            this.Settings = settings;
        }
        
        public RoutingNetwork Network { get; }
        public RoutingSettings Settings { get; }
        public (SnapPoint sp, bool? direction) Source { get; internal set; }
        public (SnapPoint sp, bool? direction) Target { get; internal set; }
        public IReadOnlyList<(SnapPoint sp, bool? direction)> Sources { get; internal set;  } = null!;
        public IReadOnlyList<(SnapPoint sp, bool? direction)> Targets { get; internal set; } = null!;
    }
}