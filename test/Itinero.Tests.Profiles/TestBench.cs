using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Routers;
using Itinero.Tests.Profiles.TestCase;
using NetTopologySuite.Operation.Buffer;

namespace Itinero.Tests.Profiles
{
    internal class TestBench
    {
        private readonly string _profilesDir;
        private readonly string _dataDir;
        private readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>();

        public TestBench(string profilesDir, string dataDir)
        {
            _profilesDir = profilesDir;
            _dataDir = dataDir;
        }

        private async Task<Profile> LoadVehicle(string behaviour)
        {
            if (Profiles.TryGetValue(behaviour, out var profile))
            {
                return profile;
            }

            var contents = await File.ReadAllTextAsync(_profilesDir + behaviour + ".lua");
            profile = LuaProfile.Load(contents);
            Profiles[behaviour] = profile;
            return profile;
        }

        public async Task<(bool success, string message)> Run((TestData test, string path) testdata)
        {
            try
            {
                return await RunUnsafe(testdata);
            }
            catch (Exception e)
            {
                return (false, "EXCEPTION: " + e.Message);
            }
        }

        private async Task<(bool success, string message)> RunUnsafe((TestData test, string path) testdata)
        {
            var (test, path) = testdata;
            // load vehicle.
            var vehicle = await LoadVehicle(test.Profile.Name);

            // load data using vehicle.
            var routerDb = new RouterDb();
            using (var writer = routerDb.GetAsMutable())
            {
                var routerDbStreamTarget = new RouterDbStreamTarget(writer);
                await using (var stream = File.OpenRead(this._dataDir + test.OsmDataFile))
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
                File.WriteAllText(path + ".failed.geojson",
                    routeLineString.ToJson());
                File.WriteAllText(path + ".expected.geojson",
                    test.Expected.ToJson());
                
                return (false, "Route outside of expected buffer. Written debug geojsons to "+path);
            }

            return (true, string.Empty);
        }
    }
}