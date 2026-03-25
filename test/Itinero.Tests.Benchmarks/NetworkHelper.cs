using System.Collections.Generic;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Builds test networks of various sizes for benchmarking.
/// </summary>
internal static class NetworkHelper
{
    /// <summary>
    /// Builds a grid network with the given dimensions.
    /// Each grid cell is ~100m, giving realistic edge lengths.
    /// </summary>
    public static RouterDb BuildGridNetwork(int width, int height, Profile? profile = null)
    {
        profile ??= OsmProfiles.Car;

        var osm = new List<OsmGeo>();
        long nodeId = 1;
        long wayId = 1;

        // longitude/latitude step ~100m.
        const double lonStep = 0.0013;
        const double latStep = 0.0009;
        const double baseLon = 4.800;
        const double baseLat = 51.200;

        var nodeIds = new long[width, height];

        // create nodes.
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                nodeIds[x, y] = nodeId;
                osm.Add(new Node
                {
                    Id = nodeId++,
                    Longitude = baseLon + x * lonStep,
                    Latitude = baseLat + y * latStep
                });
            }
        }

        // create horizontal ways.
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { nodeIds[x, y], nodeIds[x + 1, y] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }
        }

        // create vertical ways.
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height - 1; y++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { nodeIds[x, y], nodeIds[x, y + 1] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }
        }

        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 0 });
        routerDb.PrepareFor(profile);
        routerDb.UseOsmData(new OsmEnumerableStreamSource(osm), s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        return routerDb;
    }
}
