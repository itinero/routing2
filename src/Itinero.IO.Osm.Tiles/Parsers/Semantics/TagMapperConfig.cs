using System.Collections.Generic;

namespace Itinero.IO.Osm.Tiles.Parsers.Semantics
{
    /// <summary>
    /// Represents mapping configuration for a single tag.
    /// </summary>
    public class TagMapperConfig
    {
        /// <summary>
        /// Gets or sets the osm key.
        /// </summary>
        public string OsmKey { get; set; }

        /// <summary>
        /// Gets or sets the predicate.
        /// </summary>
        public string Predicate { get; set; }

        /// <summary>
        /// Gets or sets the reverse mapping.
        /// </summary>
        public Dictionary<string, string> ReverseMapping { get; set; }
    }
}