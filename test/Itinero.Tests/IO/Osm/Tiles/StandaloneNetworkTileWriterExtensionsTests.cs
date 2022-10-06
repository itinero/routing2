using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone.Writer;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;
using System.Linq;
using Itinero.Indexes;
using Itinero.Tests.Mocks.Indexes;

namespace Itinero.Tests.IO.Osm.Tiles;


public class StandaloneNetworkTileWriterExtensionsTests
{
    [Fact]
    public void StandaloneNetworkTileWriterExtensions_AddTileData_AddWayInsideTile_ShouldAddRegularEdge()
    {
        // https://tile.openstreetmap.org/14/8410/5465.png
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });

        var osmGeos = new OsmGeo[]
        {
            new Node()
            {
                Id = 1,
                Version = 1,
                Longitude = 4.80177104473114,
                Latitude = 51.26888649155825
            },
            new Node()
            {
                Id = 2,
                Version = 1,
                Longitude = 4.801084399223328,
                Latitude = 51.268054112711056
            },
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var writer = routerDb.Latest.GetStandaloneTileWriter(8410, 5465);

        writer.AddTileData(osmGeos);

        var tile = writer.GetResultingTile();
        var tileEnumerator = tile.GetEnumerator();
        Assert.True(tileEnumerator.MoveTo(new VertexId(tile.TileId, 0)));
        Assert.True(tileEnumerator.MoveNext());
        Assert.Equal(0U, tileEnumerator.Tail.LocalId);
        Assert.Equal(1U, tileEnumerator.Head.LocalId);
        Assert.Equal(1U, tileEnumerator.EdgeTypeId);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            tileEnumerator.Attributes.Where(x => !x.key.StartsWith("_")));
        Assert.True(tileEnumerator.Forward);
        Assert.Null(tileEnumerator.TailOrder);
        Assert.Null(tileEnumerator.HeadOrder);
    }

    [Fact]
    public void StandaloneNetworkTileWriterExtensions_AddTileData_AddWayCrossBoundary_ShouldAddBoundaryEdge()
    {
        // https://tile.openstreetmap.org/14/8410/5465.png
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });

        var osmGeos = new OsmGeo[]
        {
            new Node()
            {
                Id = 1,
                Version = 1,
                Longitude = 4.80177104473114,
                Latitude = 51.26888649155825
            },
            new Node()
            {
                Id = 2,
                Version = 1,
                Longitude = 4.788472652435303,
                Latitude = 51.2667216038964,
            },
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var writer = routerDb.Latest.GetStandaloneTileWriter(8410, 5465);

        writer.AddTileData(osmGeos);

        var tile = writer.GetResultingTile();
        var tileEnumerator = tile.GetEnumerator();
        Assert.True(tileEnumerator.MoveTo(new VertexId(tile.TileId, 0)));
        Assert.False(tileEnumerator.MoveNext());

        var boundaryEdges = tile.GetBoundaryCrossings().ToList();
        Assert.Single(boundaryEdges);
        var boundaryEdge = boundaryEdges[0];
        Assert.Equal(1U, boundaryEdge.edgeTypeId);
        Assert.Equal(0U, boundaryEdge.vertex.LocalId);
        Assert.Equal(1, boundaryEdge.globalIdFrom);
        Assert.Equal(2, boundaryEdge.globalIdTo);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            boundaryEdge.attributes.Where(x => !x.key.StartsWith("_")));
    }

    [Fact]
    public void StandaloneNetworkTileWriterExtensions_AddTileData_AddRestrictionInside_ShouldAddBinaryTurnCost()
    {
        // https://tile.openstreetmap.org/14/8410/5465.png
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });

        var osmGeos = new OsmGeo[]
        {
            new Node()
            {
                Id = 1,
                Version = 1,
                Longitude = 4.80177104473114,
                Latitude = 51.26888649155825
            },
            new Node()
            {
                Id = 2,
                Version = 1,
                Longitude =   4.801084399223328,
                Latitude = 51.268054112711056,
            },
            new Node()
            {
                Id = 3,
                Version = 1,
                Longitude = 4.8004889488220215,
                Latitude = 51.268856284525015
            },
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Relation()
            {
                Id = 1,
                Version = 1,
                Members = new[] { new RelationMember(1, "from", OsmGeoType.Way), new RelationMember(2, "via", OsmGeoType.Node), new RelationMember(2, "to", OsmGeoType.Way) },
                Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
            }
        };

        var writer = routerDb.Latest.GetStandaloneTileWriter(8410, 5465);

        writer.AddTileData(osmGeos);

        var tile = writer.GetResultingTile();
        var tileEnumerator = tile.GetEnumerator();

        Assert.True(tileEnumerator.MoveTo(new VertexId(tile.TileId, 0)));
        Assert.True(tileEnumerator.MoveNext());
        Assert.Equal(0U, tileEnumerator.Tail.LocalId);
        Assert.Equal(1U, tileEnumerator.Head.LocalId);
        Assert.Equal(1U, tileEnumerator.EdgeTypeId);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            tileEnumerator.Attributes.Where(x => !x.key.StartsWith("_")));
        Assert.True(tileEnumerator.Forward);
        Assert.Null(tileEnumerator.TailOrder);
        Assert.Equal((byte)0, tileEnumerator.HeadOrder);
        var turnCosts = tileEnumerator.GetTurnCostFromHead(1).ToList();
        Assert.Single(turnCosts);
        var turnCost = turnCosts[0];
        Assert.Equal(1U, turnCost.cost);
        Assert.False(tileEnumerator.MoveNext());

        Assert.True(tileEnumerator.MoveTo(new VertexId(tile.TileId, 1)));
        Assert.True(tileEnumerator.MoveNext());
        Assert.Equal(1U, tileEnumerator.Tail.LocalId);
        Assert.Equal(2U, tileEnumerator.Head.LocalId);
        Assert.Equal(1U, tileEnumerator.EdgeTypeId);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            tileEnumerator.Attributes.Where(x => !x.key.StartsWith("_")));
        Assert.True(tileEnumerator.Forward);
        Assert.Equal((byte)1, tileEnumerator.TailOrder);
        Assert.Null(tileEnumerator.HeadOrder);
        turnCosts = tileEnumerator.GetTurnCostToTail(0).ToList();
        Assert.Single(turnCosts);
        turnCost = turnCosts[0];
        Assert.Equal(1U, turnCost.cost);
        Assert.True(tileEnumerator.MoveNext());
        Assert.Equal(1U, tileEnumerator.Tail.LocalId);
        Assert.Equal(0U, tileEnumerator.Head.LocalId);
        Assert.Equal(1U, tileEnumerator.EdgeTypeId);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            tileEnumerator.Attributes.Where(x => !x.key.StartsWith("_")));
        Assert.False(tileEnumerator.Forward);
        Assert.Equal((byte)0, tileEnumerator.TailOrder);
        Assert.Null(tileEnumerator.HeadOrder);
        turnCosts = tileEnumerator.GetTurnCostFromTail(1).ToList();
        Assert.Single(turnCosts);
        turnCost = turnCosts[0];
        Assert.Equal(1U, turnCost.cost);
        Assert.False(tileEnumerator.MoveNext());

        Assert.True(tileEnumerator.MoveTo(new VertexId(tile.TileId, 2)));
        Assert.True(tileEnumerator.MoveNext());
        Assert.Equal(2U, tileEnumerator.Tail.LocalId);
        Assert.Equal(1U, tileEnumerator.Head.LocalId);
        Assert.Equal(1U, tileEnumerator.EdgeTypeId);
        Assert.Equal(new (string Key, string value)[] { ("highway", "residential") },
            tileEnumerator.Attributes.Where(x => !x.key.StartsWith("_")));
        Assert.False(tileEnumerator.Forward);
        Assert.Null(tileEnumerator.TailOrder);
        Assert.Equal((byte)1, tileEnumerator.HeadOrder);
        turnCosts = tileEnumerator.GetTurnCostToHead(0).ToList();
        Assert.Single(turnCosts);
        turnCost = turnCosts[0];
        Assert.Equal(1U, turnCost.cost);
        Assert.False(tileEnumerator.MoveNext());
    }
}
