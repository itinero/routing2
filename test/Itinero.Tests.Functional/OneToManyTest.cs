using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A one-to-many routing test.
    /// </summary>
    public class OneToManyTest : FunctionalTest<Route[], (RouterDb routerDb, SnapPoint source, SnapPoint[] targets, Profile profile)>
    {
        protected override Route[] Execute((RouterDb routerDb, SnapPoint source, SnapPoint[] targets, Profile profile) input)
        {
            var (routerDb, source, targets, profile) = input;
            var results = routerDb.Calculate( new RoutingSettings() { Profile = profile }, source, targets);

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
        public static readonly OneToManyTest Default = new OneToManyTest();
    }
}