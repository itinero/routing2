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
        Result<Route> Build(RoutingNetwork db, Profile profile, Path path, bool forward = true);
    }
}