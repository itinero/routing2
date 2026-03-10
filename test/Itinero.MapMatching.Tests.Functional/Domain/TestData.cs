using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Itinero.MapMatching.Tests.Functional.Domain;

internal class TestData
{
    public string Description { get; set; }

    public ProfileConfig Profile { get; set; }

    public string TrackFile { get; set; }

    public string OsmDataFile { get; set; }

    [JsonConverter(typeof(Json.LineStringJsonConverter))]
    public LineString Expected { get; set; }
}
