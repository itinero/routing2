using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing.DataStructures;

namespace Itinero.Routes.Builders
{
    /// <summary>
    /// Contains extension methods for the route builders.
    /// </summary>
    public static class IRouteBuilderExtensions
    {
        /// <summary>
        /// Builds a route for the given path.
        /// </summary>
        /// <param name="builder">The route builder.</param>
        /// <param name="db">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="path">The path.</param>
        /// <param name="forward">Forward flag.</param>
        /// <returns>The route.</returns>
        public static Result<Route> Build(this IRouteBuilder builder, RouterDb db, Profile profile, Result<Path> path, bool forward = true)
        {
            if (path.IsError) return path.ConvertError<Route>();
            
            return builder.Build(db, profile, path, forward);
        }
    }
}