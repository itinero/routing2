using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.IO.Osm;
using Itinero.Network;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Data.Providers;

/// <summary>
/// The gate for moving metadata out of the tile: what an edge reports, recorded rather than
/// compared against another part of the same code, so it means the same thing before the read path
/// is flipped to the provider and after.
/// </summary>
/// <remarks>
/// Edges are identified by their edge type, which comes from the type index either way. Identifying
/// them by their attributes would be identifying them by the thing under test.
/// </remarks>
public class EdgeDataIsUnchangedTests
{
    [Theory]
    [MemberData(nameof(Expectations))]
    public void AnEdge_ShouldReportTheWayItWasBuiltFrom(string highway,
        (string key, string value)[] attributes, (double longitude, double latitude)[] shape)
    {
        var provider = new OsmDataProvider(new OsmEnumerableStreamSource(Ways()));
        provider.Load();
        var network = provider.RouterDb.Latest;

        var found = 0;
        var enumerator = network.GetEdgeEnumerator();
        var vertices = network.GetVertexEnumerator();
        while (vertices.MoveNext())
        {
            if (!enumerator.MoveTo(vertices.Current)) continue;
            while (enumerator.MoveNext())
            {
                if (!enumerator.Forward) continue;

                var edgeType = provider.RouterDb.EdgeTypeIndex.GetById(enumerator.EdgeTypeId);
                if (!edgeType.Contains(("highway", highway))) continue;

                Assert.Equal(attributes.ToHashSet(), enumerator.Attributes.ToHashSet());

                var reported = enumerator.Shape.ToList();
                Assert.Equal(shape.Length, reported.Count);
                for (var s = 0; s < shape.Length; s++)
                {
                    // the tile quantizes to a 4096 x 4096 grid per tile, ~5.4e-6 degrees at zoom 14,
                    // where the provider keeps the coordinate it was given. One bound covers both.
                    Assert.True(Math.Abs(shape[s].longitude - reported[s].longitude) < 1e-5,
                        $"longitude {reported[s].longitude}, expected {shape[s].longitude}");
                    Assert.True(Math.Abs(shape[s].latitude - reported[s].latitude) < 1e-5,
                        $"latitude {reported[s].latitude}, expected {shape[s].latitude}");
                }

                found++;
            }
        }

        Assert.Equal(1, found);
    }

    // recorded by hand from the fixture below: the way's full tag set, and the coordinates of the
    // nodes strictly between its two core nodes.
    public static IEnumerable<object[]> Expectations =>
    [
        [
            "residential",
            new[] { ("highway", "residential"), ("name", "Main Street") },
            new[] { (4.802, 51.271), (4.803, 51.2715) }
        ],
        [
            "primary",
            new[]
            {
                ("highway", "primary"), ("maxspeed", "70"), ("oneway", "yes"), ("ref", "N1")
            },
            new[] { (4.805, 51.2725) }
        ],
        [
            "cycleway",
            new[] { ("highway", "cycleway"), ("operator", "x") },
            new[] { (4.807, 51.2735), (4.808, 51.274) }
        ]
    ];

    // three ways meeting end to end, each with interior shape nodes. Node n sits at
    // (4.800 + n * 0.001, 51.270 + n * 0.0005).
    private static OsmGeo[] Ways()
    {
        var osm = new List<OsmGeo>();
        for (var n = 1; n <= 9; n++)
        {
            osm.Add(new Node
            {
                Id = n, Version = 1,
                Longitude = 4.800 + (n * 0.001), Latitude = 51.270 + (n * 0.0005)
            });
        }

        osm.Add(new Way
        {
            Id = 1, Version = 1, Nodes = [1L, 2, 3, 4],
            Tags = new TagsCollection(new Tag("highway", "residential"), new Tag("name", "Main Street"))
        });
        osm.Add(new Way
        {
            Id = 2, Version = 1, Nodes = [4L, 5, 6],
            Tags = new TagsCollection(new Tag("highway", "primary"), new Tag("maxspeed", "70"),
                new Tag("oneway", "yes"), new Tag("ref", "N1"))
        });
        osm.Add(new Way
        {
            Id = 3, Version = 1, Nodes = [6L, 7, 8, 9],
            Tags = new TagsCollection(new Tag("highway", "cycleway"), new Tag("operator", "x"))
        });

        return osm.ToArray();
    }
}
