using System;
using OsmSharp;

namespace Itinero.IO.Streams.Processors
{
    public static class CoordinateExtensions
    {
        /// <summary>
        ///     Estimated distance (in meters) to the other point
        /// </summary>
        public static double? DistanceTo(
            this Node n0,
            Node n1
        )
        {
            if (n0.Latitude == null || n0.Longitude == null || n1.Latitude == null || n1.Longitude == null) {
                return null;
            }
            return DistanceEstimateInMeter(n0.Longitude.Value, n0.Latitude.Value, n1.Longitude.Value, n1.Latitude.Value);
        }

        /// <summary>
        ///     Estimated distance (in meters) to the other point
        /// </summary>
        private static double DistanceEstimateInMeter(
            (double lon, double lat) c1,
            (double lon, double lat) c2
        )
        {
            return DistanceEstimateInMeter(c1.lon, c1.lat, c2.lon, c2.lat);
        }

        private static double DistanceEstimateInMeter(
            double longitude1,
            double latitude1,
            double longitude2,
            double latitude2
        )
        {
            var lat1rad = latitude1 / 180.0 * 3.141592653589793;
            var lon1rad = longitude1 / 180.0 * 3.141592653589793;
            var lat2rad = latitude2 / 180.0 * 3.141592653589793;
            var lon2rad = (longitude2 / 180.0 * 3.141592653589793 - lon1rad) * Math.Cos((lat1rad + lat2rad) / 2.0);
            var diff = lat2rad - lat1rad;
            return Math.Sqrt(lon2rad * lon2rad + diff * diff) * 6371000.0;
        }
    }
}