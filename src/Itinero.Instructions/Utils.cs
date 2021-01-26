using System;
using Itinero.Routes;

namespace Itinero.Instructions {
    internal class Box<T> {
        public T Content { get; set; }
    }

    internal static class Utils {
        private const double RADIUS_OF_EARTH = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// Stolen from https://github.com/itinero/routing/blob/1764afc75db43a1459789592de175283f642123f/src/Itinero/LocalGeo/Coordinate.cs
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        public static double DistanceEstimateInMeter((double lon, double lat, float? e) c1,
            (double lon, double lat, float? e) c2) {
            var lat1Rad = c1.lat / 180d * Math.PI;
            var lon1Rad = c1.lon / 180d * Math.PI;
            var lat2Rad = c2.lat / 180d * Math.PI;
            var lon2Rad = c2.lon / 180d * Math.PI;

            var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = Math.Sqrt(x * x + y * y) * RADIUS_OF_EARTH;

            return m;
        }

        /// <summary>
        /// Given two WGS84 coordinates, if walking from c1 to c2, it gives the angle that one would be following.
        /// 0° is north, 90° is east, -90° is west, both 180 and -180 are south
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static double AngleBetween((double lon, double lat, float? e) c1,
            (double lon, double lat, float? e) c2) {
            var dy = c2.lat - c1.lat;
            var dx = Math.Cos(Math.PI / 180 * c1.lat) * (c2.lon - c1.lon);
            // phi is the angle we search, but with 0 pointing eastwards and in radians
            var phi = Math.Atan2(dy, dx);
            var angle =
                (phi - Math.PI / 2) // Rotate 90° to have the north up
                * 180 / Math.PI; // Convert to degrees

            // A bit of normalization below:
            if (angle <= -180) {
                angle += 360;
            }

            if (angle > 180) {
                angle -= 180;
            }

            return angle;
        }


        public static string GetAttributeOrNull(this Route.Meta meta, string key) {
            return meta.GetAttributeOrDefault(key, null);
        }

        /**
         * Normalizes degrees to be between -180 (incl) and 180 (excl)
         */
        public static int NormalizeDegrees(this double degrees) {
            if (degrees <= -180) {
                degrees += 360;
            }

            if (degrees > 180) {
                degrees -= 360;
            }

            return (int) degrees;
        }

        public static string GetAttributeOrDefault(this Route.Meta meta, string key, string deflt) {
            if (meta == null) {
                return deflt;
            }

            foreach (var (k, value) in meta.Attributes) {
                if (k.Equals(key)) {
                    return value;
                }
            }

            return deflt;
        }

        public static string DegreesToText(this int degrees) {
            var cutoff = 30;
            if (-cutoff < degrees && degrees < cutoff) {
                return "straight on";
            }

            var direction = "left";
            if (degrees < 0) {
                direction = "right";
            }

            degrees = Math.Abs(degrees);
            if (degrees > 180 - cutoff) {
                return "sharp " + direction;
            }

            if (degrees < 2 * cutoff) {
                return "slightly " + direction;
            }

            return direction;
        }
    }
}