using Itinero.Profiles;
using Itinero.Routers;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A one-to-many routing test.
    /// </summary>
    public class RouterOneToManyTest : FunctionalTest<Route[], (RouterDbInstance routerDb, SnapPoint source, SnapPoint[] targets, Profile profile)>
    {
        protected override Route[] Execute((RouterDbInstance routerDb, SnapPoint source, SnapPoint[] targets, Profile profile) input)
        {
            var (routerDb, source, targets, profile) = input;

            var results = routerDb.Route(new RoutingSettings() {Profile = profile})
                .From(source)
                .To(targets)
                .Calculate();

            var routes = new Route[targets.Length];
            for (var r = 0; r < routes.Length; r++)
            {
                routes[r] = results[r].Value;
            }
            return routes;
        }
        
        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterOneToManyTest Default = new RouterOneToManyTest();
    }
}