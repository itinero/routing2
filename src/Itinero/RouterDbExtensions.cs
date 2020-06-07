using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Geo;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;
using Itinero.Routers;
using Itinero.Serialization;

namespace Itinero
{
    /// <summary>
    /// Extensions related to the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Mutate the router db using a delegate.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="mutate">The delegate.</param>
        public static void Mutate(this RouterDb routerDb, Action<IMutableRouterDb> mutate)
        {
            using var mutable = routerDb.GetAsMutable();
            mutate(mutable);
        }

        /// <summary>
        /// Serializes the router db to disk.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="config">The delegate to configure.</param>
        public static void Serialize(this RouterDb routerDb, Action<RouterDbSerializationSettings> config)
        {
            using var mutable = routerDb.GetAsMutable();
            
            var settings = new RouterDbSerializationSettings();
            config(settings);
            
            
        }
    }
}