using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A many-to-one routing test.
    /// </summary>
    public class ManyToOneTest : FunctionalTest<Route[], (RouterDb routerDb, SnapPoint[] sources, SnapPoint target, Profile profile)>
    {
        protected override Route[] Execute((RouterDb routerDb, SnapPoint[] sources, SnapPoint target, Profile profile) input)
        {
            var (routerDb, sources, target, profile) = input;
            var results = routerDb.Calculate(profile, sources, target);

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
        public static readonly ManyToOneTest Default = new ManyToOneTest();
    }
}