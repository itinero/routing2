using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTests
    {
        [Fact]
        public void Graph_Empty_AddVertex_ShouldReturnTileIdAnd0()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal(Tile.WorldToTile(4.7868, 51.2643, graph.Zoom).LocalId, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
        }
        
        [Fact]
        public void Graph_OneVertex_AddSecondVertexInTile_ShouldReturnTileIdAnd1()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graph = new Graph();
            graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }
        
        [Fact]
        public void Graph_OneVertex_TryGetVertex_ShouldReturnVertexLocation()
        {
            // when adding a vertex to a tile the graph should store the location accurately.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            Assert.True(graph.TryGetVertex(vertex1, out var longitude, out var latitude));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude,4);
        }
        
        [Fact]
        public void Graph_Empty_AddVertex_ShouldReturnProperTileId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile with a proper
            // local id.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            var tile = Tile.FromLocalId(vertex1.TileId, graph.Zoom);
            Assert.Equal((uint)8409, tile.X);
            Assert.Equal((uint)5465, tile.Y);
            Assert.Equal(14, tile.Zoom);
        }

        [Fact]
        public void Graph_TwoVertices_AddEdge_ShouldReturn0()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edges = graph.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edges.LocalId); // first edge should have id 0.
        }

        [Fact]
        public void Graph_ThreeVertices_AddSecondEdge_ShouldReturnPointerAsId()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            graph.AddEdge(vertex1, vertex2);
            var edges = graph.AddEdge(vertex1, vertex3);
            Assert.Equal((uint)6, edges.LocalId);
        }

        [Fact]
        public void Graph_AddEdge_OverTileBoundary_ShouldEdgeTwice_WithOneId()
        {
            // store an edge across tile boundaries should store the edge twice.
            // once in the tile of vertex1, in forward direction.
            // once in the tile of vertex2, in backward direction.
            // we test this by enumeration edges for both vertices.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = graph.AddVertex(3.146638870239258,51.31060357805506);

            var edge = graph.AddEdge(vertex1, vertex2);
            Assert.Equal(vertex1.TileId, edge.TileId);
            Assert.Equal((uint)0, edge.LocalId);

            var enumerator = graph.GetEnumerator();
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
        public void GraphEnumerator_EdgeWithShape_ShouldEnumerateShapeForward()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = graph.AddVertex(4.801111221313477,51.26676859478893);

            var edge = graph.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
            {
                (4.800703525543213, 51.26832598004091),
                (4.801368713378906, 51.26782252075405)
            });

            var enumerator = graph.GetEnumerator();
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
        public void GraphEnumerator_EdgeWithShape_ShouldEnumerateShapeBackward()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = graph.AddVertex(4.801111221313477,51.26676859478893);

            var edge = graph.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
            {
                (4.800703525543213, 51.26832598004091),
                (4.801368713378906, 51.26782252075405)
            });

            var enumerator = graph.GetEnumerator();
            Assert.True(enumerator.MoveTo(vertex2));
            Assert.True(enumerator.MoveNext());

            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            Assert.Equal(4.800703525543213, shape[1].longitude, 4);
            Assert.Equal(51.26832598004091, shape[1].latitude, 4);
            Assert.Equal(4.801368713378906, shape[0].longitude, 4);
            Assert.Equal(51.26782252075405, shape[0].latitude, 4);
        }
    }
}