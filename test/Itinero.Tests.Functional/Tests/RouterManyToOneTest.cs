using Itinero.Profiles;
using Itinero.Routers;

namespace Itinero.Tests.Functional.Tests
{
    /// <summary>
    /// A many-to-one routing test.
    /// </summary>
    public class RouterManyToOneTest : FunctionalTest<Route[], (Network routerDb, SnapPoint[] sources, SnapPoint target, Profile profile)>
    {
        protected override Route[] Execute((Network routerDb, SnapPoint[] sources, SnapPoint target, Profile profile) input)
        {
            var (routerDb, sources, target, profile) = input;

            var results = routerDb.Route(new RoutingSettings() {Profile = profile})
                .From(sources)
                .To(target)
                .Calculate();

            var routes = new Route[sources.Length];
            for (var r = 0; r < routes.Length; r++)
            {
                routes[r] = results[r].Value;
            }
            return routes;
        }
        
        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterManyToOneTest Default = new RouterManyToOneTest();
    }
}