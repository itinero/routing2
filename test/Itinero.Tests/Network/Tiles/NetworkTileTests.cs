using System.Linq;
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
            
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
        }
        
        [Fact]
        public void NetworkTile_AddVertex_OneVertex_ShouldReturn1()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate a new local id.
            
            vertex1 = networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }

        [Fact]
        public void NetworkTile_TryGetVertex0_Vertex0DoesNotExists_ShouldReturnFalse()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            
            Assert.False(networkTile.TryGetVertex(new VertexId(networkTile.TileId, 0), 
                out var longitude, out var latitude));
        }

        [Fact]
        public void NetworkTile_TryGetVertex0_Vertex0Exists_ShouldReturnTrueAndLatLon()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 = networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            Assert.True(networkTile.TryGetVertex(vertex1, out var longitude, out var latitude));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }

        [Fact]
        public void NetworkTile_AddEdge0_VerticesExist_ShouldReturn0()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

            var edge = networkTile.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edge.LocalId);
        }

        [Fact]
        public void NetworkTile_AddEdge1_VerticesExist_ShouldReturn9()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

            // the first edge takes 4 bytes.
            networkTile.AddEdge(vertex1, vertex2);
            // the second edge get the pointer as id.
            var edge = networkTile.AddEdge(vertex2, vertex1);
            Assert.Equal((uint)9, edge.LocalId);
        }

        [Fact]
        public void NetworkTile_AddEdge_OverTileBoundary_ShouldStoreVertex()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = new VertexId(vertex1.TileId + 1, 451);

            var edge = networkTile.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edge.LocalId);

            var size = networkTile.DecodeVertex(edge.LocalId, out var localId, out var tileId);
            Assert.Equal((uint)0, localId);
            Assert.Equal((uint)vertex1.TileId, tileId);
            size = networkTile.DecodeVertex(edge.LocalId + size, out localId, out tileId);
            Assert.Equal((uint)451, localId);
            Assert.Equal((uint)vertex1.TileId + 1, tileId);
        }

        [Fact]
        public void NetworkTile_AddEdge_ShouldAddEdge()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
            var edge = networkTile.AddEdge(vertex1, vertex2);
            
            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            enumerator.MoveNext();
            Assert.Equal(new EdgeId(networkTile.TileId, 0), edge);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
        }

        [Fact]
        public void NetworkTile_CloneForEdgeTypeMap_MapEmpty_OneEdge_ShouldNotAffectEdge()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
            var edge = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key1", "value1")
            });
            
            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            enumerator.MoveNext();
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Null(enumerator.EdgeTypeId);
            var a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);

            networkTile = networkTile.CloneForEdgeTypeMap((0, at => 0));
            
            enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            enumerator.MoveNext();
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)0, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);
        }

        [Fact]
        public void NetworkTile_CloneForEdgeTypeMap_MapEmpty_TwoEdges_ShouldNotAffectEdges()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
            var edge1 = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key1", "value1")
            });
            var edge2 = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key2", "value2")
            });
            
            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Null(enumerator.EdgeTypeId);
            var a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key2", "value2"), a);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Null(enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);

            networkTile = networkTile.CloneForEdgeTypeMap((0, at => 0));
            
            enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)0, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key2", "value2"), a);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)0, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);
        }

        [Fact]
        public void NetworkTile_CloneForEdgeTypeMap_OneEdge_ShouldNotAffectEdge()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
            var edge = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key1", "value1")
            });
            
            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            enumerator.MoveNext();
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal(null, enumerator.EdgeTypeId);
            var a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);

            networkTile = networkTile.CloneForEdgeTypeMap((0, at => 1234));
            
            enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            enumerator.MoveNext();
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)1234, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);
        }

        [Fact]
        public void NetworkTile_CloneForEdgeTypeMap_TwoEdges_ShouldNotAffectEdges()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
            var edge1 = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key1", "value1")
            });
            var edge2 = networkTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
            {
                ("key2", "value2")
            });
            
            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Null(enumerator.EdgeTypeId);
            var a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key2", "value2"), a);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Null(enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);

            networkTile = networkTile.CloneForEdgeTypeMap((0, at => 1234));
            
            enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            // moved 1 byte because of new edge type id.
            Assert.Equal(new EdgeId(edge2.TileId, edge2.LocalId + 1), enumerator.EdgeId); 
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)1234, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key2", "value2"), a);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.EdgeId);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal((uint?)1234, enumerator.EdgeTypeId);
            a = enumerator.Attributes;
            Assert.Single(a);
            Assert.Contains(("key1", "value1"), a);
        }
    }
}