using Itinero.Algorithms.DataStructures;
using Itinero.Profiles;

namespace Itinero.Algorithms.Routes
{
    /// <summary>
    /// Abstract representation of a route builder.
    /// </summary>
    public interface IRouteBuilder
    {
        /// <summary>
        /// Tries to build a route for the given profile/source/target and path.
        /// </summary>
        Result<Route> Build(RouterDb db, Profile profile, Path path, bool forward = true);
    }
}