using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Itinero.Tests.Profiles.TestCase
{
    /// <summary>
    /// Describes meta-data about a test.
    /// </summary>
    internal class TestData
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the profile name.
        /// </summary>
        public ProfileConfig Profile { get; set; }

        /// <summary>
        /// Gets or sets the osm data file.
        /// </summary>
        public string OsmDataFile { get; set; }
        
        /// <summary>
        /// Gets or sets the expected route.
        /// </summary>
        [JsonConverter(typeof(Json.LineStringJsonConverter))]
        public LineString Expected { get; set; }
    }
}