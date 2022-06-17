using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.IO.Osm.Streams;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;

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
        public static void UseOsmData(this RouterDb routerDb, OsmStreamSource data,
            Action<DataProviderSettings>? configure = null)
        {
            // get writer.
            if (routerDb.HasMutableNetwork)
            {
                throw new InvalidOperationException(
                    $"Cannot add data to a {nameof(RouterDb)} that is only being written to.");
            }

            using var routerDbWriter = routerDb.GetMutableNetwork();

            // create settings.
            var settings = new DataProviderSettings();
            configure?.Invoke(settings);

            // do filtering.

            // 1: complete objects.
            if (settings.TagsFilter.CompleteFilter != null)
            {
                data = data.ApplyCompleteFilter(settings.TagsFilter.CompleteFilter);
            }

            // 2: filter relations.
            if (settings.TagsFilter.MemberFilter != null)
            {
                data = data.ApplyRelationMemberFilter(settings.TagsFilter.MemberFilter);
            }

            // 3: filter tags on ways and relations.
            if (settings.TagsFilter.Filter != null)
            {
                data = data.ApplyFilter(settings.TagsFilter.Filter);
            }

            // use writer to fill router db.
            var routerDbStreamTarget = new RouterDbStreamTarget(routerDbWriter, settings.ElevationHandler);
            routerDbStreamTarget.RegisterSource(data);
            routerDbStreamTarget.Initialize();
            routerDbStreamTarget.Pull();
        }
    }
}