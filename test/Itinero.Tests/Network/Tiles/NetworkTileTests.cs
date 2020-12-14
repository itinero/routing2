using Itinero.Network;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles
{
    public partial class NetworkTileTests
    {
        [Fact]
        public void NetworkTile_AddVertex_TileEmpty_ShouldReturn0()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
        }
        
        [Fact]
        public void NetworkTile_AddVertex_OneVertex_ShouldReturn1()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate a new local id.
            
            vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }

        [Fact]
        public void NetworkTile_TryGetVertex0_Vertex0DoesNotExists_ShouldReturnFalse()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            
            Assert.False(graphTile.TryGetVertex(new VertexId(graphTile.TileId, 0), 
                out var longitude, out var latitude));
        }

        [Fact]
        public void NetworkTile_TryGetVertex0_Vertex0Exists_ShouldReturnTrueAndLatLon()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }

        [Fact]
        public void NetworkTile_AddEdge0_VerticesExist_ShouldReturn0()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edge.LocalId);
        }

        [Fact]
        public void NetworkTile_AddEdge1_VerticesExist_ShouldReturn9()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            // the first edge takes 4 bytes.
            graphTile.AddEdge(vertex1, vertex2);
            // the second edge get the pointer as id.
            var edge = graphTile.AddEdge(vertex2, vertex1);
            Assert.Equal((uint)9, edge.LocalId);
        }

        [Fact]
        public void NetworkTile_AddEdge_OverTileBoundary_ShouldStoreVertex()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = new VertexId(vertex1.TileId + 1, 451);

            var edge = graphTile.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edge.LocalId);

            var size = graphTile.DecodeVertex(edge.LocalId, out var localId, out var tileId);
            Assert.Equal((uint)0, localId);
            Assert.Equal((uint)vertex1.TileId, tileId);
            size = graphTile.DecodeVertex(edge.LocalId + size, out localId, out tileId);
            Assert.Equal((uint)451, localId);
            Assert.Equal((uint)vertex1.TileId + 1, tileId);
        }
    }
}