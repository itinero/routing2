using Itinero.Algorithms.Search;
using Itinero.LocalGeo;

namespace Itinero
{
    /// <summary>
    /// Extensions related to the routerdb.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routerDb">The routerdb.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The snap point.</returns>
        public static SnapPoint Snap(this RouterDb routerDb, double longitude, double latitude)
        {
            return routerDb.Network.SnapInBox(
                (longitude - 0.001, latitude - 0.001, longitude + 0.001, latitude + 0.001));
        }

        /// <summary>
        /// Gets the length of an edge.
        /// </summary>
        /// <param name="routerDb">The routerdb.</param>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The length in deci meters.</returns>
        public static uint EdgeLength(this RouterDb routerDb, uint edgeId)
        {
            var enumerator = routerDb.Network.Graph.GetEnumerator();
            if (!enumerator.MoveToEdge(edgeId)) return 0;

            var vertex1 = routerDb.Network.Graph.GetVertex(enumerator.From);
            var vertex2 = routerDb.Network.Graph.GetVertex(enumerator.To);
            
            var distance = 0.0;

            // compose geometry.
            var previous = new Coordinate(vertex1.Longitude, vertex1.Latitude));
            var shape = routerDb.GetShape(edgeId);
            if (shape != null)
            {
                foreach (var shapePoint in shape)
                {
                    var current = new Coordinate(shapePoint.Longitude, shapePoint.Latitude);
                    distance += Coordinate.DistanceEstimateInMeter(previous, current);
                    previous = current;
                }
            }
            distance += Coordinate.DistanceEstimateInMeter(previous, new Coordinate(vertex2.Longitude, vertex2.Latitude));

            return (uint) System.Math.Round(distance / 10);
        }
    }
}