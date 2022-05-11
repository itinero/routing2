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
        public static IEqualityComparer<(double longitude, double latitude, float? e)> GetCoordinateComparer(
            int tolerance = 4)
        {
            return new CoordinateComparer(tolerance);
        }

        public static void Equal(NetworkTileEnumerator enumerator, EdgeId? edge = null, VertexId? vertex1 = null,
            VertexId? vertex2 = null,
            IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null,
            uint? edgeType = uint.MaxValue,
            IEqualityComparer<(double longitude, double latitude, float? e)>? coordinateComparer = null)
        {
            if (edge != null) {
                Assert.Equal(edge, enumerator.EdgeId);
            }

            if (vertex1 != null) {
                Assert.Equal(vertex1, enumerator.Tail);
            }

            if (vertex2 != null) {
                Assert.Equal(vertex2, enumerator.Head);
            }

            if (edgeType != uint.MaxValue) {
                Assert.Equal(edgeType, enumerator.EdgeTypeId);
            }

            if (attributes != null) {
                Assert.Equal(attributes, enumerator.Attributes);
            }

            if (shape != null) {
                coordinateComparer ??= GetCoordinateComparer();
                Assert.Equal(shape, enumerator.Shape, coordinateComparer);
            }
        }

        public static void SameLocations((double longitude, double latitude, float? e) expected,
            (double longitude, double latitude, float? e) actual,
            double toleranceInMeters = 1)
        {
            var distance = expected.DistanceEstimateInMeter(actual);
            if (distance > toleranceInMeters) {
                Assert.True(false, "Coordinates are too far apart to be considered at the same location.");
            }
        }

        public static void SameLocations((double longitude, double latitude) expected,
            (double longitude, double latitude) actual,
            double toleranceInMeters = 1)
        {
            SameLocations((expected.longitude, expected.latitude, 0f),
                (actual.longitude, actual.latitude, 0f), toleranceInMeters);
        }

        public static void SameLocations((double longitude, double latitude) expected,
            (double longitude, double latitude, float? e) actual,
            double toleranceInMeters = 1)
        {
            SameLocations((expected.longitude, expected.latitude, 0f),
                actual, toleranceInMeters);
        }

        public static void RouteMatches((double longitude, double latitude, float? e)[] shape, Route route,
            double toleranceInMeters = 1)
        {
            Assert.Equal(shape.Length, route.Shape.Count);

            for (var s = 0; s < shape.Length; s++) {
                SameLocations(shape[s], route.Shape[s]);
            }
        }
    }

    internal class CoordinateComparer : IEqualityComparer<(double longitude, double latitude, float? e)>
    {
        private readonly int _tolerance;

        public CoordinateComparer(int tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals((double longitude, double latitude, float? e) x,
            (double longitude, double latitude, float? e) y)
        {
            var xlon = Math.Round((decimal) x.longitude, _tolerance);
            var xlat = Math.Round((decimal) x.latitude, _tolerance);
            var ylon = Math.Round((decimal) y.longitude, _tolerance);
            var ylat = Math.Round((decimal) y.latitude, _tolerance);

            return xlon == ylon && xlat == ylat;
        }

        public int GetHashCode((double longitude, double latitude, float? e) obj)
        {
            var lon = Math.Round((decimal) obj.longitude, _tolerance);
            var lat = Math.Round((decimal) obj.latitude, _tolerance);

            return HashCode.Combine(lon, lat);
        }
    }
}