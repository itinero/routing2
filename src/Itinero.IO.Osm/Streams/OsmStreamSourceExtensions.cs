using System;
using Itinero.IO.Osm.Filters;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Osm.Streams
{
    /// <summary>
    /// Contains extension methods for filtering an OSM stream source.
    /// </summary>
    public static class OsmStreamSourceExtensions
    {
        /// <summary>
        /// Applies a filter to the given source and returns a new filtered source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A new filtered source.</returns>
        public static OsmStreamSource ApplyFilter(this OsmStreamSource source,
            TagsFilter.FilterDelegate filter)
        {
            var filtered = new OsmGeoTagsPreprocessor(osmGeo => filter(osmGeo));
            filtered.RegisterSource(source);
            return filtered;
        }

        /// <summary>
        /// Applies a complete to the given source and returns a new filtered source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A new filtered source.</returns>
        public static OsmStreamSource ApplyCompleteFilter(this OsmStreamSource source,
            TagsFilter.CompleteFilterDelegate filter)
        {
            var filtered = new CompleteOsmGeoPreprocessor((geo, osmGeo) => filter(geo, osmGeo));
            filtered.RegisterSource(source);
            return filtered;
        }

        /// <summary>
        /// Applies a relation member filter to the given source and returns a new filtered source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A new filtered source.</returns>
        public static OsmStreamSource ApplyRelationMemberFilter(this OsmStreamSource source,
            TagsFilter.FilterRelationMemberDelegate filter)
        {
            var filtered = new RelationTagsPreprocessor(
                (relation, geo) => filter(relation, geo));
            filtered.RegisterSource(source);
            return filtered;
        }
    }
}
