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
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1095707416534424,51.31076453560284);

            var edge = routerDb.AddEdge(vertex1, vertex2);
            var snapPoint = new SnapPoint(edge, 0);

            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDb);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDb.GetVertex(vertex1)) < .1);
        }

        [Fact]
        public void SnapExtensions_LocationOnNetwork_OffsetMax_ShouldReturnVertex2()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1095707416534424,51.31076453560284);

            var edge = routerDb.AddEdge(vertex1, vertex2);
            var snapPoint = new SnapPoint(edge, ushort.MaxValue);

            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDb);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDb.GetVertex(vertex2)) < .1);
        }

        [Fact]
        public void SnapExtensions_LocationOnNetwork_OffsetHalf_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1095707416534424,51.31076453560284);

            var edge = routerDb.AddEdge(vertex1, vertex2);
            var snapPoint = new SnapPoint(edge, ushort.MaxValue / 2);

            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDb);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter((routerDb.GetVertex(vertex1), routerDb.GetVertex(vertex2)).Center()) < .1);
        }
        
        [Fact]
        public void Snap_Empty_ShouldFail()
        {
            var routerDb = new RouterDb();

            var result = routerDb.Snap(new VertexId(0, 1));
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneVertex_ShouldFail()
        {
            var routerDb = new RouterDb();
            var vertex = routerDb.AddVertex(3.146638870239258, 51.31060357805506);

            var result = routerDb.Snap(vertex);
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex1_ShouldReturnOffset0()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1466388702392580,51.31060357805506);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var result = routerDb.Snap(vertex1);
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex2_ShouldReturnOffsetMax()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1466388702392580,51.31060357805506);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var result = routerDb.Snap(vertex2);
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdge_Middle_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.1095707416534424,51.31076453560284);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var location = ((3.1074142456054688 + 3.1095707416534424) / 2,
                (51.31012070202407 + 51.31076453560284) / 2);
            var result = routerDb.Snap(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDb).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex1_ShouldReturnOffset0()
        {          
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(4.801111221313477,51.26676859478893);

            var edge = routerDb.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
            {
                (4.800703525543213, 51.26832598004091),
                (4.801368713378906, 51.26782252075405)
            });

            var result = routerDb.Snap(routerDb.GetVertex(vertex1));
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex2_ShouldReturnOffsetMax()
        {          
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(4.801111221313477,51.26676859478893);

            var edge = routerDb.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
            {
                (4.800703525543213, 51.26832598004091),
                (4.801368713378906, 51.26782252075405)
            });

            var result = routerDb.Snap(routerDb.GetVertex(vertex2));
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdgeWithShape_Middle_ShouldReturnMiddle()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(4.801111221313477,51.26676859478893);

            var edge = routerDb.AddEdge(vertex1, vertex2, shape: new (double longitude, double latitude)[]
            {
                (4.800703525543213, 51.26832598004091),
                (4.801368713378906, 51.26782252075405)
            });

            var location = (new SnapPoint(edge, ushort.MaxValue / 2)).LocationOnNetwork(routerDb);
            var result = routerDb.Snap(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDb).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edge, result.Value.EdgeId);
        }

        [Fact]
        public void Snap_Direction_0Degrees_PerfectForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            
            // add a perfect horizontal edge.
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(routerDb.GetVertex(vertex1).OffsetWithDistanceX(100));
            var edge = routerDb.AddEdge(vertex1, vertex2);
            
            // calculate direction.
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDb, DirectionEnum.East);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_45OffsetForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            
            // add a perfect horizontal edge.
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(routerDb.GetVertex(vertex1).OffsetWithDistanceX(100));
            var edge = routerDb.AddEdge(vertex1, vertex2);
            
            // calculate direction.
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDb, DirectionEnum.NorthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
            
            // calculate direction.
            direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDb, DirectionEnum.SouthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_180OffsetForward_ShouldReturnBackward()
        {
            var routerDb = new RouterDb();
            
            // add a perfect horizontal edge.
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(routerDb.GetVertex(vertex1).OffsetWithDistanceX(100));
            var edge = routerDb.AddEdge(vertex1, vertex2);
            
            // calculate direction.
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDb, DirectionEnum.West);
            Assert.NotNull(direction);
            Assert.False(direction.Value);
        }

        [Fact]
        public void Snap_Direction_180Degrees_PerfectForward_ShouldReturnForward()
        {
            var routerDb = new RouterDb();
            
            // add a perfect horizontal edge.
            var vertex1 = routerDb.AddVertex(4.800467491149902,51.26896368721961);
            var vertex2 = routerDb.AddVertex(routerDb.GetVertex(vertex1).OffsetWithDistanceX(100));
            var edge = routerDb.AddEdge(vertex2, vertex1);
            
            // calculate direction.
            var direction = (new SnapPoint(edge, ushort.MaxValue / 2)).Direction(routerDb, DirectionEnum.West);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }
    }
}