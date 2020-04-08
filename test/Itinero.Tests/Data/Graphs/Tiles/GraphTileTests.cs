using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Tiles
{
    public partial class GraphTileTests
    {
        [Fact]
        public void GraphTile_AddVertex_TileEmpty_ShouldReturn0()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.7868, 51.2643, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
        }
        
        [Fact]
        public void GraphTile_AddVertex_OneVertex_ShouldReturn1()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.7868, 51.2643, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate a new local id.
            
            vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }

        [Fact]
        public void GraphTile_TryGetVertex0_Vertex0DoesNotExists_ShouldReturnFalse()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.7868, 51.2643, 14).LocalId);
            
            Assert.False(graphTile.TryGetVertex(new VertexId(graphTile.TileId, 0), 
                out var longitude, out var latitude));
        }

        [Fact]
        public void GraphTile_TryGetVertex0_Vertex0Exists_ShouldReturnTrueAndLatLon()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.7868, 51.2643, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }

        [Fact]
        public void GraphTile_AddEdge0_VerticesExist_ShouldReturn0()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edge.LocalId);
        }

        [Fact]
        public void GraphTile_AddEdge1_VerticesExist_ShouldReturn6()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            // the first edge takes 4 bytes.
            graphTile.AddEdge(vertex1, vertex2);
            // the second edge get the pointer as id.
            var edge = graphTile.AddEdge(vertex2, vertex1);
            Assert.Equal((uint)6, edge.LocalId);
        }
    }
}