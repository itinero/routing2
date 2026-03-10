using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.IO.Json.GeoJson;
using Itinero.IO.Osm;
using Itinero.MapMatching.IO.GeoJson;
using Itinero.MapMatching.Tests.Functional.Domain;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Routes;
using Neo.IronLua;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using OsmSharp.Streams;

namespace Itinero.MapMatching.Tests.Functional;

internal static class TestBench
{
    private static readonly Dictionary<string, Profile> Profiles = new();

    private static Profile LoadProfile(string vehicleFile)
    {
        if (Profiles.TryGetValue(vehicleFile, out var vehicle)) return vehicle;

        var name = new FileInfo(vehicleFile).Name;
        name = name[..name.LastIndexOf(".", StringComparison.Ordinal)];
        vehicle = LuaProfile.Load(File.ReadAllText(vehicleFile), name);
        Profiles[vehicleFile] = vehicle;
        return vehicle;
    }

    public static async Task<(bool success, string message)> RunAsync(this TestData test)
    {
        try
        {
            // load profile.
            var profile = LoadProfile(test.Profile.File);

            // load data using profile.
            var routerDb = new RouterDb(new RouterDbConfiguration()
            {
                EdgeTypeMap = new DefaultAttributeSetMap()
            });
            await using (var stream = File.OpenRead(test.OsmDataFile))
            {
                if (test.OsmDataFile.EndsWith("osm.pbf"))
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

            var geojson = routerDb.Latest.ToGeoJson();

            var routingNetwork = routerDb.Latest;

            // test route.
            Track track;
            if (test.TrackFile.EndsWith(".tsv"))
            {
                await using var stream = File.OpenRead(test.TrackFile);
                track = FromTsv(new StreamReader(stream));
            }
            else
            {
                await using var stream = File.OpenRead(test.TrackFile);
                track = FromGeoJson(new StreamReader(stream));
            }

            try
            {
                var matcher = routingNetwork.Matcher(s =>
                {
                    s.Profile = profile;
                    s.MaxDistanceRatio = 5;
                    //s.MaxSnappingDistance = 30;
                });
                var match = await matcher.MatchAsync(track);

                // check route.
                var routes = matcher.Routes(match);
                var routeLineString = routes.ToMultiLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    await File.WriteAllTextAsync(test.TrackFile + ".track.geojson",
                        track.ToGeoJson());
                    await File.WriteAllTextAsync(test.TrackFile + ".failed.geojson",
                        BuildErrorOutput(routes, expectedBuffered, track).ToGeoJson());
                    await File.WriteAllTextAsync(test.TrackFile + ".network.geojson",
                        routingNetwork.ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }
                else
                {
                    await File.WriteAllTextAsync(test.TrackFile + ".expected.geojson",
                        BuildErrorOutput(routes, expectedBuffered, track).ToGeoJson());
                }

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                await File.WriteAllTextAsync(test.TrackFile + ".network.geojson",
                    routingNetwork.ToGeoJson());
                await File.WriteAllTextAsync(test.TrackFile + ".track.geojson",
                    track.ToGeoJson());
                return (false, e.ToString());
            }
        }
        catch (Exception e)
        {
            return (false, e.ToString());
        }
    }

    private static FeatureCollection BuildErrorOutput(IEnumerable<Route> routes, Geometry buffer, Track track)
    {
        var features = new FeatureCollection();

        foreach (var route in routes)
        {
            features.Add(new Feature(route.ToLineString(),
                new AttributesTable { { "type", "route" } }));
        }

        foreach (var feature in track.ToFeatures()) features.Add(feature);

        features.Add(new Feature(buffer, new AttributesTable { { "type", "buffer" } }));

        return features;
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
        var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
        var featureCollection = jsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(reader));
        foreach (var feature in featureCollection)
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
}
