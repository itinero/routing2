using System.Collections.Generic;

namespace Itinero.Routers
{
    internal class Router : IRouter, IRouterOneToOne, IRouterManyToMany, IRouterManyToOne, IRouterOneToMany
    {
        internal Router(Network network, RoutingSettings settings)
        {
            this.Network = network;
            this.Settings = settings;
        }
        
        public Network Network { get; }
        public RoutingSettings Settings { get; }
        public (SnapPoint sp, bool? direction) Source { get; internal set; }
        public (SnapPoint sp, bool? direction) Target { get; internal set; }
        public IReadOnlyList<(SnapPoint sp, bool? direction)> Sources { get; internal set;  } = null!;
        public IReadOnlyList<(SnapPoint sp, bool? direction)> Targets { get; internal set; } = null!;
    }
}