using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;
using Itinero.Routers;

namespace Itinero
{
    /// <summary>
    /// Contains extension methods for networks.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        /// Configure a router with the given settings.
        /// </summary>
        /// <param name="network">The network.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>A router.</returns>
        public static IRouter Route(this Network network, RoutingSettings settings)
        {
            return new Router(network, settings);
        }

        /// <summary>
        /// Configure a router with the given profile.
        /// </summary>
        /// <param name="network">The network.</param>
        /// <param name="profile">The profile.</param>
        /// <returns>A router.</returns>
        public static IRouter Route(this Network network, Profile profile)
        {
            return network.Route(new RoutingSettings()
            {
                Profile = profile
            });
        }

        internal static ProfileHandler GetProfileHandler(this Network routerDb, Profile profile)
        {
            return new ProfileHandlerDefault(profile);
        }
        
        internal static IEnumerable<(string key, string value)> GetAttributes(this Network routerDb, EdgeId edge)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            if (!enumerator.MoveToEdge(edge)) return Enumerable.Empty<(string key, string value)>();

            return enumerator.Attributes;
        }
    }
}