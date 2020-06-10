using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero
{
    /// <summary>
    /// Contains extensions for the router db writer.
    /// </summary>
    public static class IMutableRouterDbExtensions
    {
        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="mutableRouterDb">The router db writer.</param>
        /// <param name="location">The location.</param>
        /// <returns>The ID of the new vertex.</returns>
        public static VertexId AddVertex(this IMutableRouterDb mutableRouterDb, (double longitude, double latitude) location)
        {
            return mutableRouterDb.AddVertex(location.longitude, location.latitude);
        }

        /// <summary>
        /// Gets the vertex.
        /// </summary>
        /// <param name="mutableRouterDb">The router db writer.</param>
        /// <param name="vertexId">The ID of the vertex.</param>
        /// <returns>The location of the ID.</returns>
        public static (double longitude, double latitude) GetVertex(this IMutableRouterDb mutableRouterDb,
            VertexId vertexId)
        {
            if (!mutableRouterDb.TryGetVertex(vertexId, out var longitude, out var latitude)) throw new ArgumentException("Vertex not found!", nameof(vertexId));

            return (longitude, latitude);
        }

        /// <summary>
        /// Prepare the router db for use with the given profile.
        /// </summary>
        /// <param name="mutableRouterDb">The mutable router db.</param>
        /// <param name="profile">The profile.</param>
        public static void PrepareFor(this IMutableRouterDb mutableRouterDb, Profile profile)
        {
            mutableRouterDb.ProfileConfiguration.AddProfile(profile);
        }

        /// <summary>
        /// Gets the profiles this router db is prepared for.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <returns>The profiles this router db is prepared for.</returns>
        public static IEnumerable<Profile> PreparedProfiles(this RouterDb routerDb)
        {
            return routerDb.ProfileConfiguration.Profiles.ToList();
        }
    }
}