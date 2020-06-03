using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Geo;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;
using Itinero.Routers;

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
    }
}