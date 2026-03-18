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
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Routes;
using Itinero.Snapping;
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
                EdgeTypeMap = new DefaultAttributeSetMap(),
                MaxIslandSize = 0
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
                });
                var matchSw = System.Diagnostics.Stopwatch.StartNew();
                var match = await matcher.MatchAsync(track);
                matchSw.Stop();
                Console.Write($" match={matchSw.Elapsed.TotalMilliseconds:F0}ms");

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

    private static async Task WriteSnapDebugAsync(RoutingNetwork routingNetwork, Profile profile, Track track, string outputPath)
    {
        var features = new FeatureCollection();
        var searchRadius = 50.0;
        var snapBox = Math.Max(searchRadius * 3, 500);
        var edgeEnumerator = routingNetwork.GetEdgeEnumerator();

        for (var i = 0; i < track.Count; i++)
        {
            var tp = track[i];
            var lon = tp.Location.longitude;
            var lat = tp.Location.latitude;

            // add GPS track point.
            features.Add(new Feature(
                new Point(new Coordinate(lon, lat)),
                new AttributesTable
                {
                    { "type", "gps" },
                    { "index", i }
                }));

            // get all snap candidates.
            await foreach (var snapPoint in routingNetwork.Snap(profile, s =>
                           {
                               s.OffsetInMeter = snapBox;
                               s.OffsetInMeterMax = snapBox;
                           })
                           .ToAllAsync(lon, lat))
            {
                var loc = snapPoint.LocationOnNetwork(routingNetwork);
                var dist = (lon, lat, (float?)null).DistanceEstimateInMeter(loc);
                if (dist > searchRadius) continue;

                // get edge attributes for labeling.
                edgeEnumerator.MoveTo(snapPoint.EdgeId);
                var attrs = edgeEnumerator.Attributes.ToList();
                var highway = attrs.FirstOrDefault(a => a.key == "highway").value ?? "?";
                var name = attrs.FirstOrDefault(a => a.key == "name").value ?? "";
                var bicycle = attrs.FirstOrDefault(a => a.key == "bicycle").value ?? "";

                var label = $"{highway}";
                if (!string.IsNullOrEmpty(name)) label += $" ({name})";
                if (!string.IsNullOrEmpty(bicycle)) label += $" bicycle={bicycle}";

                // add snap candidate point.
                features.Add(new Feature(
                    new Point(new Coordinate(loc.longitude, loc.latitude)),
                    new AttributesTable
                    {
                        { "type", "snap" },
                        { "index", i },
                        { "edge_id", snapPoint.EdgeId.ToString() },
                        { "offset", snapPoint.Offset },
                        { "distance_m", Math.Round(dist, 2) },
                        { "highway", highway },
                        { "name", name },
                        { "bicycle", bicycle },
                        { "label", label }
                    }));

                // add line from GPS point to snap candidate.
                features.Add(new Feature(
                    new LineString(new[]
                    {
                        new Coordinate(lon, lat),
                        new Coordinate(loc.longitude, loc.latitude)
                    }),
                    new AttributesTable
                    {
                        { "type", "snap_line" },
                        { "index", i },
                        { "distance_m", Math.Round(dist, 2) },
                        { "label", label }
                    }));
            }
        }

        await File.WriteAllTextAsync(outputPath, features.ToGeoJson());
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
