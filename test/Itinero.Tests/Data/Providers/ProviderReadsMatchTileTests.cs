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
/// The gate for moving metadata out of the tile: asked with nothing but the <see cref="EdgeId"/>,
/// the provider answers what the tile holds for that edge, so the read path can be flipped without
/// changing an answer.
/// </summary>
public class ProviderReadsMatchTileTests
{
    private static OsmDataProvider Build()
    {
        var provider = new OsmDataProvider(new OsmEnumerableStreamSource(Ways()));
        provider.Load();

        return provider;
    }

    [Fact]
    public void ProviderAttributes_WithEdgeType_ShouldEqualTileAttributes()
    {
        var provider = Build();
        var network = provider.RouterDb.Latest;

        var seen = 0;
        var enumerator = network.GetEdgeEnumerator();
        var vertices = network.GetVertexEnumerator();
        while (vertices.MoveNext())
        {
            if (!enumerator.MoveTo(vertices.Current)) continue;
            while (enumerator.MoveNext())
            {
                var fromProvider = provider
                    .GetAttributes(enumerator.EdgeId)
                    .Concat(provider.RouterDb.EdgeTypeIndex.GetById(enumerator.EdgeTypeId));

                Assert.Equal(enumerator.Attributes.ToHashSet(), fromProvider.ToHashSet());
                seen++;
            }
        }

        Assert.True(seen > 0, "no edges were examined");
    }

    [Fact]
    public void ProviderShape_ShouldEqualTileShape()
    {
        var provider = Build();
        var network = provider.RouterDb.Latest;

        var seen = 0;
        var withShape = 0;
        var shape = new List<(double longitude, double latitude, float? e)>();
        var enumerator = network.GetEdgeEnumerator();
        var vertices = network.GetVertexEnumerator();
        while (vertices.MoveNext())
        {
            if (!enumerator.MoveTo(vertices.Current)) continue;
            while (enumerator.MoveNext())
            {
                // the provider answers in the direction the edge was written, which is forward.
                if (!enumerator.Forward) continue;

                shape.Clear();
                provider.GetShape(enumerator.EdgeId, shape);

                var fromTile = enumerator.Shape.ToList();
                Assert.Equal(fromTile.Count, shape.Count);
                for (var s = 0; s < shape.Count; s++)
                {
                    // the tile quantizes to a 4096 x 4096 grid per tile, ~5.4e-6 degrees at zoom
                    // 14, where the provider keeps the coordinate it was given.
                    Assert.True(Math.Abs(fromTile[s].longitude - shape[s].longitude) < 1e-5,
                        $"longitude {shape[s].longitude} vs {fromTile[s].longitude}");
                    Assert.True(Math.Abs(fromTile[s].latitude - shape[s].latitude) < 1e-5,
                        $"latitude {shape[s].latitude} vs {fromTile[s].latitude}");
                    Assert.Equal(fromTile[s].e, shape[s].e);
                }

                seen++;
                if (shape.Count > 0) withShape++;
            }
        }

        Assert.True(seen > 0, "no edges were examined");
        Assert.True(withShape > 0, "no edge had a shape, so nothing was compared");
    }

    [Fact]
    public void AnEdgeTheProviderNeverRecorded_ShouldAnswerEmpty()
    {
        // The failure mode the map replaces a key with: a missed entry has to read as nothing,
        // not as another edge's data.
        var provider = Build();
        var shape = new List<(double longitude, double latitude, float? e)>();

        var unwritten = new EdgeId(uint.MaxValue - 1, 0);
        Assert.Empty(provider.GetAttributes(unwritten));
        provider.GetShape(unwritten, shape);
        Assert.Empty(shape);
    }

    // ways with shape points between their core nodes, so a slice has something to return.
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
