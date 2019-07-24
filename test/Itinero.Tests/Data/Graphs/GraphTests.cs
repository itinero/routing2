using System.IO;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Itinero.LocalGeo;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTests
    {
        [Fact]
        public void Graph_ShouldGenerateTiledVertexId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }
        
        [Fact]
        public void Graph_ShouldStoreVertexLocation()
        {
            // when adding a vertex to a tile the graph should store the location accurately.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            
            Assert.True(graph.TryGetVertex(vertex1, out var location));
            Assert.Equal(4.7868, location.Longitude, 4);
            Assert.Equal(51.2643, location.Latitude,4);
        }
        
        [Fact]
        public void Graph_ShouldGenerateTiledVertexIdWithProperLocalId()
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
        public void Graph_ShouldAddEdgeAndReturnId0()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edgeId); // first edge should have id 0.
        }

        [Fact]
        public void Graph_ShouldAddEdgeSecondEdgeAndReturnId1()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId1 = graph.AddEdge(vertex1, vertex2);
            var edgeId2 = graph.AddEdge(vertex1, vertex3);
            Assert.Equal((uint)1, edgeId2);
        }
        
        [Fact]
        public void Graph_ShouldStoreShape()
        {
            var network = new Graph();
            var vertex1 = network.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = network.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edgeId = network.AddEdge(vertex1, vertex2, shape: new [] { new Coordinate(4.795167446136475,
                51.26580191532799), });

            var enumerator = network.GetEnumerator();
            enumerator.MoveToEdge(edgeId);
            var shape = enumerator.GetShape();
            Assert.NotNull(shape);
            var shapeList = shape.ToList();
            Assert.Single(shapeList);
            Assert.Equal(4.795167446136475, shapeList[0].Longitude);
            Assert.Equal(51.26580191532799, shapeList[0].Latitude);
        }
        
        [Fact]
        public void Graph_ShouldProperlyStoreACompleteGraph()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            
            var edgeId1 = graph.AddEdge(vertex1, vertex2);
            var edgeId2 = graph.AddEdge(vertex1, vertex3);
            var edgeId3 = graph.AddEdge(vertex2, vertex3);

            var enumerator = graph.GetEnumerator();
            Assert.True(enumerator.MoveTo(vertex1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex3, enumerator.To);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex2, enumerator.To);
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(vertex2));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex3, enumerator.To);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.To);
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(vertex3));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.To);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GraphEdgeEnumerator_ShouldMoveToVertexEvenWhenNoEdges()
        {
            var graph = new Graph();
            var vertex = graph.AddVertex(4.792613983154297, 51.26535213392538);

            var enumerator = graph.GetEnumerator();
            Assert.True(enumerator.MoveTo(vertex));
        }

        [Fact]
        public void GraphEdgeEnumerator_MoveNextShouldReturnFalseWhenNoEdges()
        {
            var graph = new Graph();
            var vertex = graph.AddVertex(4.792613983154297, 51.26535213392538);

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GraphEdgeEnumerator_MoveToShouldMoveToFirstEdge()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }

        [Fact]
        public void GraphEdgeEnumerator_ShouldInitializeEdgeData()
        {
            var graph = new Graph(edgeDataSize: 4);
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            var data = enumerator.Data;
            Assert.NotNull(data);
            Assert.Equal(4, data.Length);
            Assert.Equal(255, data[0]);
            Assert.Equal(255, data[1]);
            Assert.Equal(255, data[2]);
            Assert.Equal(255, data[3]);
        }

        [Fact]
        public void GraphEdgeEnumerator_ShouldStoreEdgeData()
        {
            var graph = new Graph(edgeDataSize: 4);
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2, new byte[]{ 0, 1, 2, 3 });

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            var data = enumerator.Data;
            Assert.NotNull(data);
            Assert.Equal(4, data.Length);
            Assert.Equal(0, data[0]);
            Assert.Equal(1, data[1]);
            Assert.Equal(2, data[2]);
            Assert.Equal(3, data[3]);
        }
        
        [Fact]
        public void Graph_WriteToAndReadFromShouldBeCopy()
        {
            var original = new Graph();
            var vertex1 = original.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = original.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = original.AddVertex(4.797506332397461, 51.26674845584085);
            
            var edgeId1 = original.AddEdge(vertex1, vertex2,
                shape: new [] { new Coordinate(4.795167446136475, 51.26580191532799)});
            var edgeId2 = original.AddEdge(vertex1, vertex3,
                shape: new [] { new Coordinate(4.795167446136475, 51.26580191532799)});
            var edgeId3 = original.AddEdge(vertex2, vertex3,
                shape: new [] { new Coordinate(4.795167446136475, 51.26580191532799)});

            using (var memory = new MemoryStream())
            {
                original.WriteTo(memory);

                memory.Seek(0, SeekOrigin.Begin);

                var graph = Graph.ReadFrom(memory);

                var enumerator = graph.GetEnumerator();
                Assert.True(enumerator.MoveTo(vertex1));
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex3, enumerator.To);
                var shape = enumerator.GetShape();
                Assert.NotNull(shape);
                var shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex2, enumerator.To);
                shape = enumerator.GetShape();
                Assert.NotNull(shape);
                shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.False(enumerator.MoveNext());
            
                Assert.True(enumerator.MoveTo(vertex2));
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex3, enumerator.To);
                shape = enumerator.GetShape();
                Assert.NotNull(shape);
                shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex1, enumerator.To);
                shape = enumerator.GetShape();
                Assert.NotNull(shape);
                shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.False(enumerator.MoveNext());
            
                Assert.True(enumerator.MoveTo(vertex3));
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex2, enumerator.To);
                shape = enumerator.GetShape();
                Assert.NotNull(shape);
                shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.True(enumerator.MoveNext());
                Assert.Equal(vertex1, enumerator.To);
                shape = enumerator.GetShape();
                Assert.NotNull(shape);
                shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].Longitude);
                Assert.Equal(51.26580191532799, shapeList[0].Latitude);
                Assert.False(enumerator.MoveNext());
            }
        }
    }
}