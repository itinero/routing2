using Itinero.Geo.Elevation;
using Itinero.IO.Osm.Filters;

namespace Itinero.IO.Osm
{
    /// <summary>
    /// Data provider settings.
    /// </summary>
    public class DataProviderSettings
    {
        /// <summary>
        /// Gets or sets the tags filter.
        /// </summary>
        public TagsFilter TagsFilter { get; set; } = RoutingTagsFilter.Default;

        /// <summary>
        /// Gets or sets the elevation handler.
        /// </summary>
        public IElevationHandler? ElevationHandler { get; set; } = null;
    }
}