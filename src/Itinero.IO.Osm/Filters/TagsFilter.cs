using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Complete;

namespace Itinero.IO.Osm.Filters
{
    /// <summary>
    /// A tags filter.
    /// </summary>
    /// <remarks>
    /// The filter processes objects as follows:
    /// - First pass:
    ///   - The FilterAsComplete predicate is used, if any, to determine what objects should be built up as complete objects.
    ///   - The FilterChildren function is used, if any, to store the children where, in the second pass, another function needs to be ran.
    /// - Second pass:
    ///   - The FilterChildren functions for the children is executed first, if any.
    ///   - The CompleteFilter is executed second, only on the objects that pass the FilterAsComplete predicate.
    ///   - The Filter is executed last, limiting the objects considered.
    /// </remarks>
    public sealed class TagsFilter
    {
        /// <summary>
        /// Returns tags to keep for the given way or relation.
        /// </summary>
        /// <remarks>
        /// When null is returned the way is not considered further.
        /// </remarks>
        public Func<OsmGeo, IEnumerable<(string key, string value)>?>? Filter { get; set; }
        
        /// <summary>
        /// A predicate to decide if an object has to filtered in it's complete form. Only handles way and relations.
        /// </summary>
        /// <remarks>
        /// When true a complete version of this object will be built and fed to the filter.
        /// </remarks>
        public Predicate<OsmGeo> FilterAsComplete { get; set; } = (_) => false;

        /// <summary>
        /// Filters OSM tags to keep from the given complete OSM object.
        /// </summary>
        /// <remarks>
        /// When null is returned the object is not considered further.
        /// </remarks>
        public Func<CompleteOsmGeo, IEnumerable<(string key, string value)>?>? CompleteFilter { get; set; }
        
        /// <summary>
        /// A predicate to decide if a relations members have to be filtered.
        /// </summary>
        public Predicate<Relation> FilterMembers { get; set; } = (_) => false;

        /// <summary>
        /// Filters OSM tags on children of OSM objects by applying another specific filter if an object is a child.
        /// </summary>
        /// <remarks>
        /// When null is returned, no child-specific filter is applied.
        /// </remarks>
        public Func<Relation, OsmGeo, IEnumerable<(string key, string value)>?>? MemberFilter { get; set; }
    }
}