using System.Linq;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network
{
    public class NetworkTests
    {
        [Fact]
        public void RoutingNetwork_Empty_AddVertex_ShouldReturnTileIdAnd0()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile.
            
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }
            Assert.Equal(Tile.WorldToTile(4.7868, 51.2643, network.Zoom).LocalId, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
        }
        
        [Fact]
        public void RoutingNetwork_OneVertex_AddSecondVertexInTile_ShouldReturnTileIdAnd1()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile.
            
            var network = new RoutingNetwork(new RouterDb());
            using (var writer = network.GetWriter())
            {
                writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }
            
            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            VertexId vertex1;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }
        
        [Fact]
        public void RoutingNetwork_OneVertex_TryGetVertex_ShouldReturnVertexLocation()
        {
            // when adding a vertex to a tile the network should store the location accurately.
            
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }
            
            Assert.True(network.TryGetVertex(vertex1, out var longitude, out var latitude));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude,4);
        }
        
        [Fact]
        public void RoutingNetwork_Empty_AddVertex_ShouldReturnProperTileId()
        {
            // when adding a vertex to a tile the network should always generate an id in the same tile with a proper
            // local id.
            
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }

            var tile = Tile.FromLocalId(vertex1.TileId, network.Zoom);
            Assert.Equal((uint)8409, tile.X);
            Assert.Equal((uint)5465, tile.Y);
            Assert.Equal(14, tile.Zoom);
        }

        [Fact]
        public void RoutingNetwork_TwoVertices_AddEdge_ShouldReturn0()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                edge = writer.AddEdge(vertex1, vertex2);
            }

            Assert.Equal((uint)0, edge.LocalId);
        }

        [Fact]
        public void RoutingNetwork_ThreeVertices_AddSecondEdge_ShouldReturnPointerAsId()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2, vertex3;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                writer.AddEdge(vertex1, vertex2);
                edge = writer.AddEdge(vertex1, vertex3);
            }
            
            Assert.Equal((uint)11, edge.LocalId);
        }

        [Fact]
        public void RoutingNetwork_AddEdge_OverTileBoundary_ShouldEdgeTwice_WithOneId()
        {
            // store an edge across tile boundaries should store the edge twice.
            // once in the tile of vertex1, in forward direction.
            // once in the tile of vertex2, in backward direction.
            // we test this by enumeration edges for both vertices.
            
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(3.1074142456054688,51.31012070202407);
                vertex2 = writer.AddVertex(3.146638870239258,51.31060357805506);

                edge = writer.AddEdge(vertex1, vertex2);
            }
            Assert.Equal(vertex1.TileId, edge.TileId);
            Assert.Equal((uint)0, edge.LocalId);

            var enumerator = network.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(vertex1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
            Assert.Equal(enumerator.Id, edge);
            
            Assert.True(enumerator.MoveTo(vertex2));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex2, enumerator.From);
            Assert.Equal(vertex1, enumerator.To);
            Assert.False(enumerator.Forward);
            Assert.Equal(enumerator.Id, edge);
        }

        [Fact]
        public void RoutingNetworkEnumerator_EdgeWithShape_ShouldEnumerateShapeForward()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);

                edge = writer.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                });
            }

            var enumerator = network.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(vertex1));
            Assert.True(enumerator.MoveNext());

            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            Assert.Equal(4.800703525543213, shape[0].longitude, 4);
            Assert.Equal(51.26832598004091, shape[0].latitude, 4);
            Assert.Equal(4.801368713378906, shape[1].longitude, 4);
            Assert.Equal(51.26782252075405, shape[1].latitude, 4);
        }
        
        [Fact]
        public void RoutingNetwork_RoutingNetworkEdgeEnumerator_ShouldEnumerateEdgesInRoutingNetwork()
        {
            var network = new RoutingNetwork(new RouterDb());
            
            VertexId vertex1, vertex2;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                writer.AddEdge(vertex1, vertex2);
            }

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }
        
        [Fact]
        public void RoutingNetworkEnumerator_EdgeWithShape_ShouldEnumerateShapeBackward()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);

                edge = writer.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                });
            }

            var enumerator = network.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(vertex2));
            Assert.True(enumerator.MoveNext());

            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            Assert.Equal(4.800703525543213, shape[1].longitude, 4);
            Assert.Equal(51.26832598004091, shape[1].latitude, 4);
            Assert.Equal(4.801368713378906, shape[0].longitude, 4);
            Assert.Equal(51.26782252075405, shape[0].latitude, 4);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_NewType_ShouldAdd()
        {
            var network = new RoutingNetwork(new RouterDb());
            using (var mutable = network.GetAsMutable())
            {
                mutable.SetEdgeTypeFunc(mutable.EdgeTypeFunc.NextVersion(
                    attr => attr.Where(x => x.key == "highway")));
            }
            
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
                edge = writer.AddEdge(vertex1, vertex2,
                    attributes: new (string key, string value)[] {("highway", "residential")});
            }
            
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
            
            Assert.Equal(0U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_ExistingType_ShouldGet()
        {
            var network = new RoutingNetwork(new RouterDb());
            using (var mutable = network.GetAsMutable())
            {
                mutable.SetEdgeTypeFunc(mutable.EdgeTypeFunc.NextVersion(
                    attr => attr.Where(x => x.key == "highway")));
            }
            
            VertexId vertex1, vertex2, vertex3;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
                vertex3 = writer.AddVertex(4.801111221313477,51.26676859478893);
                writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[] { ("highway", "residential") });
                edge = writer.AddEdge(vertex1, vertex3,
                    attributes: new (string key, string value)[] {("highway", "residential")});
            }
            
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
            
            Assert.Equal(0U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_SecondType_ShouldAdd()
        {
            var network = new RoutingNetwork(new RouterDb());
            using (var mutable = network.GetAsMutable())
            {
                mutable.SetEdgeTypeFunc(mutable.EdgeTypeFunc.NextVersion(
                    attr => attr.Where(x => x.key == "highway")));
            }
            
            VertexId vertex1, vertex2, vertex3;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
                vertex3 = writer.AddVertex(4.801111221313477,51.26676859478893);
                writer.AddEdge(vertex1, vertex2,
                    attributes: new (string key, string value)[] {("highway", "residential")});
                edge = writer.AddEdge(vertex1, vertex3,
                    attributes: new (string key, string value)[] {("highway", "primary")});
            }
            
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
            
            Assert.Equal(1U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_NewEdgeTypeFunc_ShouldUpdateEdgeTypeId()
        {
            var network = new RoutingNetwork(new RouterDb());
            using (var mutable = network.GetAsMutable())
            {
                mutable.SetEdgeTypeFunc(mutable.EdgeTypeFunc.NextVersion(
                    attr => attr.Where(x => x.key == "highway")));
            }
            
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);

                edge = writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
                {
                    ("highway", "residential"),
                    ("maxspeed", "50")
                });
            }

            // update edge type func.
            using (var mutable = network.GetAsMutable())
            {
                mutable.SetEdgeTypeFunc(mutable.EdgeTypeFunc.NextVersion(
                    attr => attr.Where(x => x.key == "highway" || x.key == "maxspeed")));
            });
            
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
            
            Assert.Equal(1U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_Empty_ShouldNotReturnVertices()
        {
            var network = new RoutingNetwork(new RouterDb());

            var enumerator = network.GetVertexEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_OneVertex_ShouldReturnOneVertex()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex;
            using (var writer = network.GetWriter())
            {
                vertex = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex, enumerator.Current);
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_TwoVertices_ShouldReturnTwoVertices()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(4.800467491149902,51.26896368721961);
            }

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex2, enumerator.Current);
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_TwoVertices_DifferentTiles_ShouldReturnTwoVertices()
        {
            var network = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                vertex2 = writer.AddVertex(5.801111221313477,51.26676859478893);
            }

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex2, enumerator.Current);
        }
    }
}