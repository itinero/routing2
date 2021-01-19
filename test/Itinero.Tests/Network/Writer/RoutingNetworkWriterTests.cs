using System.Collections.Generic;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.Writer
{
    public class RoutingNetworkWriterTests
    {
        [Fact]
        public void RoutingNetworkWriter_Empty_AddVertex_ShouldReturnTileIdAnd0()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile.
            
            var network = new RoutingNetwork(new RouterDb());
            var (vertices, _) = network.Write(new (double longitude, double latitude, float? e)[]
            {
                (4.7868, 51.2643, (float?)null)
            });

            Assert.Equal(Tile.WorldToTile(4.7868, 51.2643, network.Zoom).LocalId, vertices[0].TileId);
            Assert.Equal((uint) 0, vertices[0].LocalId);
        }

        [Fact]
        public void RoutingNetworkWriter_OneVertex_AddSecondVertexInTile_ShouldReturnTileIdAnd1()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile.

            var network = new RoutingNetwork(new RouterDb());
            var (vertices, _) = network.Write(new (double longitude, double latitude, float? e)[]
            {
                (4.7868, 51.2643, (float?)null)
            });

            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            (vertices, _) = network.Write(new (double longitude, double latitude, float? e)[]
            {
                (4.7868, 51.2643, (float?)null)
            });

            Assert.Equal((uint) 89546969, vertices[0].TileId);
            Assert.Equal((uint) 1, vertices[0].LocalId);
        }
        
        [Fact]
        public void RoutingNetworkWriter_OneVertex_TryGetVertex_ShouldReturnVertexLocation()
        {
            // when adding a vertex to a tile the network should store the location accurately.

            var network = new RoutingNetwork(new RouterDb());
            var (vertices, _) = network.Write(new (double longitude, double latitude, float? e)[]
            {
                (4.7868, 51.2643, (float?)null)
            });

            Assert.True(network.TryGetVertex(vertices[0], out var longitude, out var latitude, out _));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }

        [Fact]
        public void RoutingNetworkWriter_Empty_AddVertex_ShouldReturnProperTileId()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile with a proper
            // local id.

            var network = new RoutingNetwork(new RouterDb());
            var (vertices, _) = network.Write(new (double longitude, double latitude, float? e)[]
            {
                (4.7868, 51.2643, (float?)null)
            });

            var tile = Tile.FromLocalId(vertices[0].TileId, network.Zoom);
            Assert.Equal((uint) 8409, tile.X);
            Assert.Equal((uint) 5465, tile.Y);
            Assert.Equal(14, tile.Zoom);
        }

         [Fact]
         public void RoutingNetworkWriter_TwoVertices_AddEdge_ShouldReturn0()
         {
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[]
             {
                 (4.792613983154297, 51.26535213392538, (float?)null),
                 (4.797506332397461, 51.26674845584085, (float?)null)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
             {
                 (0, 1, null)
             });

             Assert.Equal((uint)0, edges[0].LocalId);
         }

         [Fact]
         public void RoutingNetworkWriter_ThreeVertices_AddSecondEdge_ShouldReturnPointerAsId()
         {
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[]
             {
                 (4.792613983154297, 51.26535213392538, (float?)null),
                 (4.797506332397461, 51.26674845584085, (float?)null),
                 (4.797506332397461, 51.26674845584085, (float?)null)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
             {
                 (0, 1, null),
                 (0, 2, null)
             });
             
             Assert.Equal((uint)11, edges[1].LocalId);
         }

         [Fact]
         public void RoutingNetwork_AddEdge_OverTileBoundary_ShouldAddEdgeTwice_WithOneId()
         {
             // store an edge across tile boundaries should store the edge twice.
             // once in the tile of vertex1, in forward direction.
             // once in the tile of vertex2, in backward direction.
             // we test this by enumeration edges for both vertices.
             
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[]
             {
                 (3.1074142456054688,51.31012070202407, (float?)null),
                 (3.146638870239258,51.31060357805506, (float?)null)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape, IEnumerable<(string key, string value)>? attributes)[]
             {
                 (0, 1, null, null)
             });
             
             Assert.Equal(vertices[0].TileId, edges[0].TileId);
             Assert.Equal((uint)EdgeId.MinCrossId + 0, edges[0].LocalId);

             var enumerator = network.GetEdgeEnumerator();
             Assert.True(enumerator.MoveTo(vertices[0]));
             Assert.True(enumerator.MoveNext());
             Assert.Equal(vertices[0], enumerator.From);
             Assert.Equal(vertices[1], enumerator.To);
             Assert.True(enumerator.Forward);
             Assert.Equal(enumerator.Id, edges[0]);
             
             Assert.True(enumerator.MoveTo(vertices[1]));
             Assert.True(enumerator.MoveNext());
             Assert.Equal(vertices[1], enumerator.From);
             Assert.Equal(vertices[0], enumerator.To);
             Assert.False(enumerator.Forward);
             Assert.Equal(enumerator.Id, edges[0]);
         }
    }
}