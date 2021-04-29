using System.Collections.Generic;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes;
using Itinero.Routing;
using Itinero.Routing.Alternatives;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests
{
    /// <summary>
    /// A simple point-to-point routing test.
    /// </summary>
    public class RouterOneToOneWithAlternativeTest : FunctionalTest<IReadOnlyList<Route>, (RoutingNetwork routerDb, SnapPoint sp1, SnapPoint sp2,
        Profile profile)>
    {
        protected override IReadOnlyList<Route>
            Execute((RoutingNetwork routerDb, SnapPoint sp1, SnapPoint sp2, Profile profile) input)
        {
            var (routerDb, sp1, sp2, profile) = input;

            var route = routerDb.Route(new RoutingSettings {
                    Profile = profile, MaxDistance = double.MaxValue
                })
                .From(sp1)
                .To(sp2)
                .WithAlternatives(new AlternativeRouteSettings())
                .Calculate();
            return route.Value;
        }

        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterOneToOneWithAlternativeTest Default = new();
    }
}