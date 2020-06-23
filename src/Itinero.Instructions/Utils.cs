using System;
using System.Collections.Generic;

namespace Itinero.Instructions
{
    public static class Utils
    {
        private const double RadiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// Stolen from https://github.com/itinero/routing/blob/1764afc75db43a1459789592de175283f642123f/src/Itinero/LocalGeo/Coordinate.cs
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        public static double DistanceEstimateInMeter((double lon, double lat) c1,
            (double lon, double lat) c2)
        {
            var lat1Rad = c1.lat / 180d * Math.PI;
            var lon1Rad = c1.lon / 180d * Math.PI;
            var lat2Rad = c2.lat / 180d * Math.PI;
            var lon2Rad = c2.lon / 180d * Math.PI;

            var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = Math.Sqrt(x * x + y * y) * RadiusOfEarth;

            return m;
        }

        /// <summary>
        /// Given two WGS84 coordinates, if walking from c1 to c2, it gives the angle that one would be following.
        /// 0° is north, 90° is east, -90° is west, both 180 and -180 are south
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static double AngleBetween((double lon, double lat) c1, (double lon, double lat) c2)
        {
            var dy = c2.lat - c1.lat;
            var dx = Math.Cos(Math.PI / 180 * c1.lat) * (c2.lon - c1.lon);
            // phi is the angle we search, but with 0 pointing eastwards and in radians
            var phi = Math.Atan2(dy, dx);
            var angle =
                (phi - Math.PI / 2) // Rotate 90° to have the north up
                * 180 / Math.PI; // Convert to degrees

            // A bit of normalization below:
            if (angle <= -180)
            {
                angle += 360;
            }

            if (angle > 180)
            {
                angle -= 180;
            }

            return angle;
        }

        public static List<List<Route.Branch>> GetBranchesList(this Route route)
        {
            var branches = new List<List<Route.Branch>>();

            foreach (var _ in route.Shape)
            {
                branches.Add(new List<Route.Branch>());
            }
            
            foreach(var branch in route.Branches)
            {
                branches[branch.Shape].Add(branch);
            }
            
            return branches;
        }
        
        public static List<Route.Meta> MetaList(this Route route)
        {
            var metas = new List<Route.Meta>();

            var currentMeta = route.ShapeMeta[0];
            var currentMetaIndex = 0;
            for (var i = 0; i < route.Shape.Count; i++)
            {
                if (route.ShapeMeta.Count > (currentMetaIndex + 1)
                    && route.ShapeMeta[currentMetaIndex + 1].Shape == i)
                {
                    currentMetaIndex++;
                    currentMeta = route.ShapeMeta[currentMetaIndex];
                }

                metas.Add(currentMeta);
            }


            return metas;
        }

        public static string GetAttributeOrNull(this Route.Meta meta, string key)
        {
            if (meta == null)
            {
                return null;
            }

            foreach (var (k, value) in meta.Attributes)
            {
                if (k.Equals(key))
                {
                    return value;
                }
            }

            return null;
        }
    }
}