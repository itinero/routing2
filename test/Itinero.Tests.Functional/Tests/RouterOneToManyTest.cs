using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests {
    /// <summary>
    /// A one-to-many routing test.
    /// </summary>
    public class RouterOneToManyTest : FunctionalTest<Route[], (RoutingNetwork routerDb, SnapPoint source, SnapPoint[]
        targets, Profile profile)> {
        protected override Route[] Execute(
            (RoutingNetwork routerDb, SnapPoint source, SnapPoint[] targets, Profile profile) input) {
            var (routerDb, source, targets, profile) = input;

            var results = routerDb.Route(new RoutingSettings() {Profile = profile})
                .From(source)
                .To(targets)
                .Calculate();

            var routes = new Route[targets.Length];
            for (var r = 0; r < routes.Length; r++) {
                routes[r] = results[r].Value;
            }

            return routes;
        }

        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterOneToManyTest Default = new();
    }
}