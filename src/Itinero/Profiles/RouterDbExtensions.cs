using System.Collections;
using System.Collections.Generic;

namespace Itinero.Profiles
{
    /// <summary>
    /// Extension methods related to profiles and router dbs.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Prepares the router db for use with the given profiles.
        /// </summary>
        /// <param name="routerDb">The router dbs.</param>
        /// <param name="profiles">The profiles.</param>
        public static void PrepareFor(this RouterDb routerDb, params Profile[] profiles)
        {
            routerDb.ProfileConfiguration.AddProfiles(profiles);
        }
        
        /// <summary>
        /// Prepares the router db for use with the given profiles.
        /// </summary>
        /// <param name="routerDb">The router dbs.</param>
        /// <param name="profiles">The profiles.</param>
        public static void PrepareFor(this RouterDb routerDb, IEnumerable<Profile> profiles)
        {
            routerDb.ProfileConfiguration.AddProfiles(profiles);
        }
    }
}