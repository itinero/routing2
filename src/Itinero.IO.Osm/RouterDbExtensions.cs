using System;
using OsmSharp.Streams;

namespace Itinero.IO.Osm
{
    /// <summary>
    /// Contains extensions method for the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Loads the given OSM data into the router db.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="data">The data.</param>
        /// <param name="configure">The configure function.</param>
        public static void UseOsmData(this RouterDb routerDb, OsmStreamSource data, Action<DataProviderSettings> configure = null)
        {
            // get writer.
            if (routerDb.HasMutable) throw new InvalidOperationException($"Cannot add data to a {nameof(RouterDb)} that is only being written to.");
            using var routerDbWriter = routerDb.GetAsMutable();
            
            // create settings.
            var settings = new DataProviderSettings();
            configure?.Invoke(settings);
            
            // use writer to fill router db.
            var routerDbStreamTarget = new RouterDbStreamTarget(routerDbWriter);
            routerDbStreamTarget.RegisterSource(data);
            routerDbStreamTarget.Initialize();
            routerDbStreamTarget.Pull();
        }
    }
}