using System.Collections.Generic;
using System.IO;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.Tests.Functional.Staging
{
    public static class RouterDbExtensions
    {
        public static Feature ToFeature(this RouterDb routerDb, uint edgeId)
        {
            var enumerator = routerDb.Network.Graph.GetEnumerator();
            if (!enumerator.MoveToEdge(edgeId)) return null;

            var vertex1 = routerDb.Network.Graph.GetVertex(enumerator.From);
            var vertex2 = routerDb.Network.Graph.GetVertex(enumerator.To);

            // compose geometry.
            var coordinates = new List<Coordinate>();
            coordinates.Add(new Coordinate(vertex1.Longitude, vertex1.Latitude));
            var shape = routerDb.GetShape(edgeId);
            if (shape != null)
            {
                foreach (var shapePoint in shape)
                {
                    coordinates.Add(new Coordinate(shapePoint.Longitude, shapePoint.Latitude));
                }
            }
            coordinates.Add(new Coordinate(vertex2.Longitude, vertex2.Latitude));

            return new Feature(new LineString(coordinates.ToArray()), new AttributesTable());
        }
        
        public static Feature ToFeature(this RouterDb routerDb, SnapPoint snapPoint)
        {
            return routerDb.ToFeature(snapPoint.EdgeId);
        }

        public static FeatureCollection ToFeatureCollection(this RouterDb routerDb,
            Itinero.Algorithms.DataStructures.Path path)
        {
            var features = new FeatureCollection();

            foreach (var e in path)
            {
                features.Add(routerDb.ToFeature(e.edge));
            }

            return features;
        }
    }
}