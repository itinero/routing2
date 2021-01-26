using Itinero.Geo.Directions;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests
{
    /// <summary>
    /// A simple point-to-point routing test.
    /// </summary>
    public class RouterOneToOneDirectedTest : FunctionalTest<Route, (RoutingNetwork routerDb, (SnapPoint sp,
        DirectionEnum? direction) sp1, (SnapPoint sp, DirectionEnum? direction) sp2,
        Profile profile)>
    {
        protected override Route Execute(
            (RoutingNetwork routerDb, (SnapPoint sp, DirectionEnum? direction) sp1, (SnapPoint sp, DirectionEnum?
                direction) sp2, Profile profile) input)
        {
            var (routerDb, sp1, sp2, profile) = input;

            var route = routerDb.Route(new RoutingSettings {
                    Profile = profile, MaxDistance = double.MaxValue
                })
                .From(sp1)
                .To(sp2)
                .Calculate();
            return route.Value;
        }

        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterOneToOneDirectedTest Default = new();
    }
}