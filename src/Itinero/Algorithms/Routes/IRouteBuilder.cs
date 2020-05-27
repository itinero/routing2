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
        /// Builds a route for the given path.
        /// </summary>
        /// <param name="db">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="path">The path.</param>
        /// <param name="forward">Forward flag.</param>
        /// <returns>The route.</returns>
        Result<Route> Build(RouterDb db, Profile profile, Path path, bool forward = true);
    }
}