using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Itinero.IO.Osm;
using Itinero.MapMatching;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Tests.Mocks.Indexes;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Operation.Buffer;
using OsmSharp.Streams;
using Xunit;

namespace Itinero.Tests.MapMatching;

public class MapMatchingFunctionalTests
{
    private static readonly string DataDir = Path.Combine("MapMatching", "data");

    private static readonly Dictionary<string, Profile> ProfileCache = new();

    private static readonly JsonSerializerOptions GeoJsonOptions = new()
    {
        Converters = { new GeoJsonConverterFactory() },
        PropertyNameCaseInsensitive = true
    };

    private static Profile LoadProfile(string vehicleFile)
    {
        if (ProfileCache.TryGetValue(vehicleFile, out var vehicle)) return vehicle;

        var name = Path.GetFileNameWithoutExtension(vehicleFile);
        vehicle = LuaProfile.Load(File.ReadAllText(vehicleFile), name);
        ProfileCache[vehicleFile] = vehicle;
        return vehicle;
    }

    private static async Task RunTestAsync(string testJsonPath)
    {
        var fullPath = Path.Combine(DataDir, testJsonPath);
        var json = await File.ReadAllTextAsync(fullPath);
        var testData = JsonSerializer.Deserialize<TestData>(json, GeoJsonOptions);

        // paths in JSON are relative with "data/" prefix, map to our output layout.
        var profileFile = testData!.Profile.File.Replace("data/", DataDir + "/");
        var trackFile = testData.TrackFile.Replace("data/", DataDir + "/");
        var osmFile = testData.OsmDataFile.Replace("data/", DataDir + "/");

        var profile = LoadProfile(profileFile);

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            EdgeTypeMap = new SimpleAttributesSetMapMock(),
            MaxIslandSize = 0
        });
        await using (var stream = File.OpenRead(osmFile))
        {
            if (osmFile.EndsWith("osm.pbf"))
            {
                routerDb.UseOsmData(new PBFOsmStreamSource(stream));
            }
            else
            {
                var osmStream = new XmlOsmStreamSource(stream);
                var osmData = osmStream.ToList();
                routerDb.UseOsmData(new OsmEnumerableStreamSource(osmData));
            }
        }

        routerDb.PrepareFor(profile);
        var routingNetwork = routerDb.Latest;

        Track track;
        await using (var stream = File.OpenRead(trackFile))
        {
            track = trackFile.EndsWith(".tsv")
                ? FromTsv(new StreamReader(stream))
                : FromGeoJson(new StreamReader(stream));
        }

        var matcher = routingNetwork.Matcher(s => { s.Profile = profile; });
        var match = await matcher.MatchAsync(track);
        var routes = matcher.Routes(match);

        var routeLineString = new MultiLineString(
            routes.Select(Itinero.Geo.RouteExtensions.ToLineString).ToArray());

        var expectedBuffered = BufferOp.Buffer(testData.Expected, 0.00005);

        Assert.True(expectedBuffered.Covers(routeLineString),
            $"Route outside of expected buffer for {testJsonPath}");
    }

    private static Track FromTsv(TextReader reader)
    {
        var track = new List<TrackPoint>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var fields = line.Split("\t");
            var lat = double.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var lon = double.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
            track.Add(new TrackPoint(lat, lon));
        }

        return new Track(track);
    }

    private static Track FromGeoJson(TextReader reader)
    {
        var track = new List<TrackPoint>();
        var json = reader.ReadToEnd();
        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(json, GeoJsonOptions);
        foreach (var feature in featureCollection!)
        {
            if (feature.Geometry is not LineString lineString) continue;
            for (var i = 0; i < lineString.Coordinates.Length; i++)
            {
                if (track.Count > 0 && i == 0) continue;
                var c = lineString.Coordinates[i];
                track.Add(new TrackPoint(c.X, c.Y));
            }
        }

        if (track.Count == 0) throw new Exception("No track found.");
        return new Track(track);
    }

    [Theory]
    [InlineData("car/test1.json")]
    [InlineData("car/test2.json")]
    [InlineData("car/test3.json")]
    [InlineData("car/test4.json")]
    [InlineData("car/test5.json")]
    [InlineData("bicycle/test1.json")]
    [InlineData("bicycle/test2.json")]
    [InlineData("bicycle/test3.json")]
    [InlineData("bicycle/test4.json")]
    [InlineData("bicycle/test5.json")]
    [InlineData("bicycle/test6.json")]
    [InlineData("bicycle/test7.json")]
    [InlineData("bicycle/test8.json")]
    [InlineData("bicycle/test9.json")]
    [InlineData("gpx/test1.json")]
    [InlineData("gpx/test2.json")]
    [InlineData("gpx/test3.json")]
    [InlineData("gpx/test4.json")]
    [InlineData("gpx/test5.json")]
    public async Task MapMatching_FunctionalTest(string testJsonPath)
    {
        await RunTestAsync(testJsonPath);
    }
}

internal class TestData
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("profile")]
    public TestProfileConfig Profile { get; set; } = new();

    [JsonPropertyName("trackfile")]
    public string TrackFile { get; set; } = "";

    [JsonPropertyName("osmdatafile")]
    public string OsmDataFile { get; set; } = "";

    [JsonPropertyName("expected")]
    public LineString Expected { get; set; } = null!;
}

internal class TestProfileConfig
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
