using System.Collections.Generic;
using OsmSharp;

namespace Itinero.IO.Osm.Filters
{
    /// <summary>
    /// Abstract representation of a tags filter.
    /// </summary>
    public interface ITagsFilter
    {
        /// <summary>
        /// Filters OSM tags to keep from the given OSM object.
        /// </summary>
        /// <param name="osmGeo">The OSM object.</param>
        /// <returns>The tags to keep for this object or null if nothing has to be kept.</returns>
        IEnumerable<(string key, string value)>? Filter(OsmGeo osmGeo);
    }
}