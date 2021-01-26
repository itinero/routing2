using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing.DataStructures;

namespace Itinero.Routes.Builders
{
    /// <summary>
    /// Abstract representation of a route builder.
    /// </summary>
    public interface IRouteBuilder
    {
        /// <summary>
        ///     Builds a route from the given path for the given profile.
        /// </summary>
        /// <param name="db">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="path">The path.</param>
        /// <returns>The route.</returns>
        Result<Route> Build(RoutingNetwork db, Profile profile, Path path);
    }
}