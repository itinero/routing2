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
        public ITagsFilter TagsFilter = RoutingTagsFilter.Default;
    }
}