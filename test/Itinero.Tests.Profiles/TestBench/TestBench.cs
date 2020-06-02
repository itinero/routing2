using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Routers;
using Itinero.Tests.Profiles.TestBench.Domain;
using NetTopologySuite.Operation.Buffer;

namespace Itinero.Tests.Profiles.TestBench
{
    internal static class TestBench
    {
        private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>();

        private static async Task<Profile> LoadVehicle(string vehicleFile)
        {
            if (Profiles.TryGetValue(vehicleFile, out var vehicle))
            {
                return vehicle;
            }

            var profile = LuaProfile.Load(await File.ReadAllTextAsync(vehicleFile));
            Profiles[vehicleFile] = profile;
            return profile;
        }
        
        public static async Task<(bool success, string message)> Run(this TestData test, string testFile)
        {
            // load vehicle.
            var vehicle = await LoadVehicle(test.Profile.File);

            // load data using vehicle.
            var routerDb = new RouterDb();
            using (var writer = routerDb.GetWriter())
            {
                var routerDbStreamTarget = new RouterDbStreamTarget(writer);
                await using (var stream = File.OpenRead(test.OsmDataFile))
                {
                    if (test.OsmDataFile.EndsWith("osm.pbf"))
                    {
                        var osmSource = new OsmSharp.Streams.PBFOsmStreamSource(stream);
                        routerDbStreamTarget.RegisterSource(osmSource);
                        routerDbStreamTarget.Pull();
                    }
                    else
                    {
                        var osmSource = new OsmSharp.Streams.XmlOsmStreamSource(stream);
                        routerDbStreamTarget.RegisterSource(osmSource);
                        routerDbStreamTarget.Pull();
                    }
                }
            }

//            File.WriteAllText(test.OsmDataFile + ".geojson", 
//                routerDb.ToGeoJson().AddColours());

//            Console.Write($" {test.Profile.Name}");

            // test route.
            var source = test.Expected.Coordinates[0];
            var target = test.Expected.Coordinates[test.Expected.Coordinates.Length - 1];

            var latest = routerDb.Network;
            var sourceSnap = latest.Snap((source.X, source.Y), profile: vehicle);
            var targetSnap = latest.Snap((target.X, target.Y), profile: vehicle);

            var route = latest.Route(new RoutingSettings() {Profile = vehicle})
                    .From(sourceSnap)
                    .To(targetSnap)
                    .Calculate();

            if (route.IsError)
            {
                var msg = route.ErrorMessage;
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "No error message given. The route might simply not be found";
                }

                return (false, msg);
            }

            // check route.
            var routeLineString = route.Value.ToLineString();
            var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
            if (!expectedBuffered.Covers(routeLineString))
            {
                
                File.WriteAllText(testFile + ".failed.geojson", 
                    routeLineString.ToJson());
                File.WriteAllText(testFile + ".expected.geojson", 
                    test.Expected.ToJson());
                return (false, "Route outside of expected buffer.");
            }

            return (true, string.Empty);
        }
    }
}