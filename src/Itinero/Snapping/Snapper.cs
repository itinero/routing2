using System;
using Itinero.Network;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    internal class Snapper : ISnapper
    {
        public Snapper(RoutingNetwork routingNetwork)
        {
            this.RoutingNetwork = routingNetwork;
        }

        internal RoutingNetwork RoutingNetwork { get; }

        public ISnappable Using(Profile profile, Action<SnapperSettings>? settings = null)
        {
            var s = new SnapperSettings();
            settings?.Invoke(s);
            
            return new Snappable(this, new []{ profile })
            {
                AnyProfile = s.AnyProfile,
                CheckCanStopOn = s.CheckCanStopOn,
                MaxOffsetInMeter = s.MaxOffsetInMeter
            };
        }
    }
}