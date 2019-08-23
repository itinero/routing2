using System.Collections.Generic;
using System.IO;
using GeoAPI.Geometries;
using Itinero.Algorithms.Search;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Path = Itinero.Algorithms.DataStructures.Path;

namespace Itinero.Tests.Functional.Staging
{
    public static class RouterDbExtensions
    {
        public static Feature ToFeature(this RouterDb routerDb, uint edgeId)
        {
            var enumerator = routerDb.Network.GetEnumerator();
            if (!enumerator.MoveToEdge(edgeId)) return null;

            var vertex1 = routerDb.GetVertex(enumerator.From);
            var vertex2 = routerDb.GetVertex(enumerator.To);

            // compose geometry.
            var coordinates = new List<Coordinate>();
            coordinates.Add(new Coordinate(vertex1.Longitude, vertex1.Latitude));
            var shape = enumerator.GetShape();
            if (shape != null)
            {
                foreach (var shapePoint in shape)
                {
                    coordinates.Add(new Coordinate(shapePoint.Longitude, shapePoint.Latitude));
                }
            }
            coordinates.Add(new Coordinate(vertex2.Longitude, vertex2.Latitude));

            var attributes = new AttributesTable();
            var tags = routerDb.GetAttributes(edgeId);
            foreach (var tag in tags)
            {
                attributes.Add(tag.Key, tag.Value);
            }

            return new Feature(new LineString(coordinates.ToArray()), attributes);
        }
        
        public static FeatureCollection ToFeatureCollection(this RouterDb routerDb, SnapPoint snapPoint)
        {
            var features = new FeatureCollection();
            features.Add(routerDb.ToFeature(snapPoint.EdgeId));
            var locationOnNetwork = routerDb.LocationOnNetwork(snapPoint);
            features.Add(new Feature(new Point(new Coordinate(locationOnNetwork.Longitude, locationOnNetwork.Latitude)), new AttributesTable()));
            return features;
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

        public static IEnumerable<Feature> ToFeaturesVertices(this RouterDb routerDb,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            foreach (var vertexAndLocation in routerDb.Network.SearchVerticesInBox(box))
            {
                var attributes = new AttributesTable();
                attributes.Add("tile_id", vertexAndLocation.vertex.TileId);
                attributes.Add("vertex_id", vertexAndLocation.vertex.LocalId);
                var feature = new Feature(new Point(new Coordinate(vertexAndLocation.location.Longitude, vertexAndLocation.location.Latitude)),
                    attributes);

                yield return feature;
            }
        }

        public static string ToGeoJson(this RouterDb routerDb, Path path)
        {
            return (routerDb.ToFeatureCollection(path)).ToGeoJson();
        }

        public static string ToGeoJson(this RouterDb routerDb)
        {
            // write the network to shape.
            var featureCollection = new FeatureCollection();
            var features = new RouterDbFeatures(routerDb);
            foreach (var feature in features)
            {
                featureCollection.Add(feature);
            }

            return (new GeoJsonWriter()).Write(featureCollection);
        }

        public static FeatureCollection ToFeatureCollection(this RouterDb routerDb, Route route)
        {
            var features = new FeatureCollection();

            var coordinates = new List<Coordinate>();
            foreach (var e in route.Shape)
            {
                coordinates.Add(new Coordinate(e.Longitude, e.Latitude));
            }
            features.Add(new Feature(new LineString(coordinates.ToArray()), new AttributesTable()));

            return features;
        }

        public static string ToGeoJson(this RouterDb routerDb, Route route)
        {
            return (routerDb.ToFeatureCollection(route)).ToGeoJson();
        }
    }
}