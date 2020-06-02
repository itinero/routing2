using Itinero.Data.Graphs;
using Itinero.Geo;
using Itinero.Geo.Directions;
using Xunit;

namespace Itinero.Tests
{
    public class SnapPointExtensionsTests
    {
        [Fact]
        public void SnapExtensions_LocationOnNetwork_Offset0_ShouldReturnVertex1()
        {
            var routerDb = new RouterDb();
            VertexId vertex1;
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                var vertex2 = routerDbWriter.AddVertex(3.1095707416534424, 51.31076453560284);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edge, 0);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDbLatest.GetVertex(vertex1)) < .1);
        }

        [Fact]
        public void SnapExtensions_LocationOnNetwork_OffsetMax_ShouldReturnVertex2()
        {
            var routerDb = new RouterDb();
            VertexId vertex2;
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                vertex2 = routerDbWriter.AddVertex(3.1095707416534424, 51.31076453560284);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edge, ushort.MaxValue);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDbLatest.GetVertex(vertex2)) < .1);
        }

        [Fact]
        public void SnapExtensions_LocationOnNetwork_OffsetHalf_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            VertexId vertex1;
            VertexId vertex2;
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                vertex2 = routerDbWriter.AddVertex(3.1095707416534424, 51.31076453560284);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edge, ushort.MaxValue / 2);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter((routerDbLatest.GetVertex(vertex1), routerDbLatest.GetVertex(vertex2)).Center()) < .1);
        }
        
        [Fact]
        public void Snap_Empty_ShouldFail()
        {
            var routerDb = new RouterDb();

            var result = routerDb.Latest.Snap(new VertexId(0, 1));
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneVertex_ShouldFail()
        {
            var routerDb = new RouterDb();
            VertexId vertex;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                vertex = routerDbWriter.AddVertex(3.146638870239258, 51.31060357805506);
            }

            var result = routerDb.Latest.Snap(vertex);
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex1_ShouldReturnOffset0()
        {
            var routerDb = new RouterDb();
            VertexId vertex1;
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                var vertex2 = routerDbWriter.AddVertex(3.1466388702392580, 51.31060357805506);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var result = routerDb.Latest.Snap(vertex1);
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex2_ShouldReturnOffsetMax()
        {
            var routerDb = new RouterDb();
            VertexId vertex2;
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                vertex2 = routerDbWriter.AddVertex(3.1466388702392580, 51.31060357805506);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var result = routerDb.Latest.Snap(vertex2);
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdge_Middle_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(3.1074142456054688, 51.31012070202407);
                var vertex2 = routerDbWriter.AddVertex(3.1095707416534424, 51.31076453560284);

                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            var location = ((3.1074142456054688 + 3.1095707416534424) / 2,
                (51.31012070202407 + 51.31076453560284) / 2);
            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDbLatest).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex1_ShouldReturnOffset0()
        {          
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(4.801111221313477, 51.26676859478893);

                edge = routerDbWriter.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                });
            }

            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap(routerDbLatest.GetVertex(vertex1));
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex2_ShouldReturnOffsetMax()
        {          
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex2;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                vertex2 = routerDbWriter.AddVertex(4.801111221313477, 51.26676859478893);

                edge = routerDbWriter.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                });
            }

            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap(routerDbLatest.GetVertex(vertex2));
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdgeWithShape_Middle_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(4.801111221313477, 51.26676859478893);

                edge = routerDbWriter.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                });
            }

            var routerDbLatest = routerDb.Latest;
            var location = (new SnapPoint(edge, ushort.MaxValue / 2)).LocationOnNetwork(routerDbLatest);
            var result = routerDbLatest.Snap(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDbLatest).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_Direction_0Degrees_PerfectForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(routerDbWriter.GetVertex(vertex1).OffsetWithDistanceX(100));
                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDbLatest, DirectionEnum.East);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_45OffsetForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            EdgeId edge;

            // add a perfect horizontal edge.
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(routerDbWriter.GetVertex(vertex1).OffsetWithDistanceX(100));
                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }
            

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDbLatest, DirectionEnum.NorthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
            
            // calculate direction.
            direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDbLatest, DirectionEnum.SouthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_180OffsetForward_ShouldReturnBackward()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            
            // add a perfect horizontal edge.
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(routerDbWriter.GetVertex(vertex1).OffsetWithDistanceX(100));
                edge = routerDbWriter.AddEdge(vertex1, vertex2);
            }

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDbLatest, DirectionEnum.West);
            Assert.NotNull(direction);
            Assert.False(direction.Value);
        }

        [Fact]
        public void Snap_Direction_180Degrees_PerfectForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            
            // add a perfect horizontal edge.
            using (var routerDbWriter = routerDb.GetWriter())
            {
                var vertex1 = routerDbWriter.AddVertex(4.800467491149902, 51.26896368721961);
                var vertex2 = routerDbWriter.AddVertex(routerDbWriter.GetVertex(vertex1).OffsetWithDistanceX(100));
                edge = routerDbWriter.AddEdge(vertex2, vertex1);
            }

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDbLatest, DirectionEnum.West);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }
    }
}