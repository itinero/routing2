using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Tiles;
using Itinero.Routes;
using Itinero.Tests.Network.Tiles;
using Xunit;

namespace Itinero.Tests
{
    internal static class ItineroAsserts
    {
        public static IEqualityComparer<(double longitude, double latitude)> GetCoordinateComparer(int tolerance = 4) =>
            new CoordinateComparer(tolerance);

        public static void Equal(NetworkTileEnumerator enumerator, EdgeId? edge = null, VertexId? vertex1 = null, VertexId? vertex2 = null,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null,
            uint? edgeType = uint.MaxValue,
            IEqualityComparer<(double longitude, double latitude)>? coordinateComparer = null)
        {
            if (edge != null) Assert.Equal(edge, enumerator.EdgeId);
            if (vertex1 != null) Assert.Equal(vertex1, enumerator.Vertex1);
            if (vertex2 != null) Assert.Equal(vertex2, enumerator.Vertex2);
            if (edgeType != uint.MaxValue) Assert.Equal(edgeType, enumerator.EdgeTypeId);
            if (attributes != null) Assert.Equal(attributes, enumerator.Attributes);
            if (shape != null)
            {
                coordinateComparer ??= ItineroAsserts.GetCoordinateComparer();
                Assert.Equal(shape, enumerator.Shape, coordinateComparer);
            }
        }

        public static void SameLocations((double longitude, double latitude) expected,
            (double longitude, double latitude) actual,
            double toleranceInMeters = 1)
        {
            var distance = expected.DistanceEstimateInMeter(actual);
            if (distance > toleranceInMeters) Assert.True(false, "Coordinates are too far apart to be considered at the same location.");
        }

        public static void RouteMatches((double longitude, double latitude)[] shape, Route route,
            double toleranceInMeters = 1)
        {
            Assert.Equal(shape.Length, route.Shape.Count);

            for (var s = 0; s < shape.Length; s++)
            {
                ItineroAsserts.SameLocations(shape[s], route.Shape[s]);
            }
        }
    }

    internal class CoordinateComparer : IEqualityComparer<(double longitude, double latitude)>
    {
        private readonly int _tolerance ;

        public CoordinateComparer(int tolerance)
        {
            _tolerance = tolerance;
        }
        
        public bool Equals((double longitude, double latitude) x, (double longitude, double latitude) y)
        {
            var xlon = Math.Round((decimal)x.longitude, _tolerance);
            var xlat = Math.Round((decimal)x.latitude, _tolerance);
            var ylon = Math.Round((decimal)y.longitude, _tolerance);
            var ylat = Math.Round((decimal)y.latitude, _tolerance);

            return xlon == ylon && xlat == ylat;
        }

        public int GetHashCode((double longitude, double latitude) obj)
        {
            var lon = Math.Round((decimal)obj.longitude, _tolerance);
            var lat = Math.Round((decimal)obj.latitude, _tolerance);

            return HashCode.Combine(lon, lat);
        }
    }
}