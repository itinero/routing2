using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Osm
{
    /// <summary>
    /// Contains extensions method for the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Loads the given OSM data into the routerdb.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="data">The data.</param>
        /// <param name="configure">The configure function.</param>
        public static void UseOsmData(this RouterDb routerDb, OsmStreamSource data, Action<DataProviderSettings> configure = null)
        {
            var settings = new DataProviderSettings();
            
            configure?.Invoke(settings);
            
            var routerDbStreamTarget = new RouterDbStreamTarget(routerDb);
            routerDbStreamTarget.RegisterSource(data);
            
            routerDbStreamTarget.Initialize();
            routerDbStreamTarget.Pull();
        }
    }
}