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
            if (routerDb.HasMutableNetwork) {
                throw new InvalidOperationException(
                    $"Cannot add data to a {nameof(RouterDb)} that is only being written to.");
            }

            using var routerDbWriter = routerDb.GetMutableNetwork();

            // create settings.
            var settings = new DataProviderSettings();
            configure?.Invoke(settings);

            // get settings.
            var tagsFilter = settings.TagsFilter;
            var elevationHandler = settings.ElevationHandler;
            
            // do filtering.
            
            // 1: complete objects.
            if (settings.TagsFilter.CompleteFilter != null) {
                void CompleteFilterFunc(CompleteOsmGeo completeOsmGeo, OsmGeo osmGeo)
                {
                    if (osmGeo.Type == OsmGeoType.Node) return;

                    var t = settings.TagsFilter.CompleteFilter(completeOsmGeo);
                    if (t == null) return;
                    
                    osmGeo.Tags = new TagsCollection(t.Select(x => new Tag(x.key, x.value)));
                }

                var filter = new CompleteOsmGeoPreprocessor(settings.TagsFilter.FilterAsComplete,
                    CompleteFilterFunc);
                filter.RegisterSource(data);
                data = filter;
            }
            
            // 2: filter relations.
            if (settings.TagsFilter.MemberFilter != null) {
                
                void FilterMember(Relation relation, OsmGeo member)
                {
                    var t = settings.TagsFilter.MemberFilter(relation, member);
                    if (t == null) return;

                    member.Tags = new TagsCollection(t.Select(x => new Tag(x.key, x.value)));
                }

                var filter = new RelationTagsPreprocessor(settings.TagsFilter.FilterMembers,
                    FilterMember);
                filter.RegisterSource(data);
                data = filter;
            }
            
            // 3: filter tags on ways and relations.
            if (settings.TagsFilter.Filter != null) {
                bool FilterFunc(OsmGeo osmGeo)
                {
                    switch (osmGeo.Type) {
                        case OsmGeoType.Node:
                            return true;
                        case OsmGeoType.Way:
                        case OsmGeoType.Relation:
                            if (osmGeo?.Tags == null) return false;
                            
                            var wt = settings?.TagsFilter.Filter?.Invoke(osmGeo);
                            if (wt == null) return false;

                            osmGeo.Tags = new TagsCollection(wt.Select(x => new Tag(x.key, x.value)));
                            return true;
                        default:
                            return false;
                    }
                }

                var filter = new OsmGeoTagsPreprocessor(FilterFunc);
                filter.RegisterSource(data);
                data = filter;
            }
            
            // use writer to fill router db.
            var routerDbStreamTarget = new RouterDbStreamTarget(routerDbWriter, elevationHandler);
            routerDbStreamTarget.RegisterSource(data);
            routerDbStreamTarget.Initialize();
            routerDbStreamTarget.Pull();
        }
    }
}