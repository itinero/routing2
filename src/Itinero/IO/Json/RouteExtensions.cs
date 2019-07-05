using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data.Attributes;
using Itinero.LocalGeo;

namespace Itinero.IO.Json
{
    /// <summary>
    /// Contains extension methods to serialize a route to json.
    /// </summary>
    public static class RouteExtensions
    {
        /// <summary>
        /// Returns a geojson.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="includeShapeMeta">Include meta data about the shapes.</param>
        /// <param name="includeStops">Include stops as points..</param>
        /// <param name="groupByShapeMeta">Group the route by shape meta data.</param>
        /// <param name="attributesCallback">A callback to add extra attributes.</param>
        /// <param name="isRaw">A callback to customize how attributes are serialized.</param>
        /// <returns>A geojson string representing the route.</returns>
        public static string ToGeoJson(this Route route, bool includeShapeMeta = false, bool includeStops = true, bool groupByShapeMeta = false,
            Action<IAttributeCollection> attributesCallback = null, Func<string, string, bool> isRaw = null)
        {
            var stringWriter = new StringWriter();
            route.WriteGeoJson(stringWriter, includeShapeMeta, includeStops, groupByShapeMeta, attributesCallback, isRaw);
            return stringWriter.ToInvariantString();
        }
        
        internal static void WriteGeoJson(this Route route, Stream stream, bool includeShapeMeta = true, bool includeStops = true, bool groupByShapeMeta = true,
            Action<IAttributeCollection> attributesCallback = null, Func<string, string, bool> isRaw = null)
        {
            route.WriteGeoJson(new StreamWriter(stream), includeShapeMeta, includeStops, groupByShapeMeta, attributesCallback, isRaw);
        }
        
        internal static void WriteGeoJson(this Route route, TextWriter writer, bool includeShapeMeta = true, bool includeStops = true, bool groupByShapeMeta = true,
            Action<IAttributeCollection> attributesCallback = null, Func<string, string, bool> isRaw = null)
        {
            if (route == null) { throw new ArgumentNullException(nameof(route)); }
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }

            var jsonWriter = new IO.Json.JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            route.WriteGeoJsonFeatures(jsonWriter, includeShapeMeta, includeStops, groupByShapeMeta, attributesCallback, isRaw);

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();
        }
        
        internal static void WriteGeoJsonFeatures(this Route route, JsonWriter jsonWriter, bool includeShapeMeta = true, bool includeStops = true, bool groupByShapeMeta = true,
            Action<IAttributeCollection> attributesCallback = null, Func<string, string, bool> isRaw = null)
        {
            if (route == null) { throw new ArgumentNullException(nameof(route)); }
            if (jsonWriter == null) { throw new ArgumentNullException(nameof(jsonWriter)); }

            if (groupByShapeMeta)
            { // group by shape meta.
                if (route.Shape != null && route.ShapeMeta != null)
                {
                    for (var i = 0; i < route.ShapeMeta.Count; i++)
                    {
                        var shapeMeta = route.ShapeMeta[i];
                        var lowerShape = -1;
                        if (i > 0)
                        {
                            lowerShape = route.ShapeMeta[i - 1].Shape;
                        }
                        var higherShape = route.ShapeMeta[i].Shape;
                        if (lowerShape >= higherShape)
                        {
                            throw new Exception($"Invalid route: Shapes overlap.");
                        }

                        var coordinates = new List<Coordinate>();
                        for (var shape = lowerShape; shape <= higherShape; shape++)
                        {
                            if (shape >= 0 && shape < route.Shape.Count)
                            {
                                coordinates.Add(route.Shape[shape]);
                            }
                        }

                        if (coordinates.Count >= 2)
                        {
                            jsonWriter.WriteOpen();
                            jsonWriter.WriteProperty("type", "Feature", true, false);
                            jsonWriter.WriteProperty("name", "ShapeMeta", true, false);
                            jsonWriter.WritePropertyName("geometry", false);

                            jsonWriter.WriteOpen();
                            jsonWriter.WriteProperty("type", "LineString", true, false);
                            jsonWriter.WritePropertyName("coordinates", false);
                            jsonWriter.WriteArrayOpen();

                            foreach (var t in coordinates)
                            {
                                jsonWriter.WriteArrayOpen();
                                jsonWriter.WriteArrayValue(t.Longitude.ToInvariantString());
                                jsonWriter.WriteArrayValue(t.Latitude.ToInvariantString());
                                if (t.Elevation.HasValue)
                                {
                                    jsonWriter.WriteArrayValue(t.Elevation.Value.ToInvariantString());
                                }
                                jsonWriter.WriteArrayClose();
                            }

                            jsonWriter.WriteArrayClose();
                            jsonWriter.WriteClose();

                            jsonWriter.WritePropertyName("properties");
                            jsonWriter.WriteOpen();
                            if (shapeMeta.Attributes != null)
                            {
                                var attributes = shapeMeta.Attributes;
                                if (attributesCallback != null)
                                {
                                    attributes = new AttributeCollection(attributes);
                                    attributesCallback(attributes);
                                }
                                foreach (var attribute in attributes)
                                {
                                    var raw = isRaw != null &&
                                        isRaw(attribute.Key, attribute.Value);
                                    jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
                                }
                            }
                            jsonWriter.WriteClose();

                            jsonWriter.WriteClose();
                        }
                    }
                }

                if (route.Stops != null &&
                    includeStops)
                {
                    foreach (var stop in route.Stops)
                    {
                        jsonWriter.WriteOpen();
                        jsonWriter.WriteProperty("type", "Feature", true, false);
                        jsonWriter.WriteProperty("name", "Stop", true, false);
                        jsonWriter.WriteProperty("Shape", stop.Shape.ToInvariantString(), true, false);
                        jsonWriter.WritePropertyName("geometry", false);

                        jsonWriter.WriteOpen();
                        jsonWriter.WriteProperty("type", "Point", true, false);
                        jsonWriter.WritePropertyName("coordinates", false);
                        jsonWriter.WriteArrayOpen();
                        jsonWriter.WriteArrayValue(stop.Coordinate.Longitude.ToInvariantString());
                        jsonWriter.WriteArrayValue(stop.Coordinate.Latitude.ToInvariantString());
                        if (stop.Coordinate.Elevation.HasValue)
                        {
                            jsonWriter.WriteArrayValue(stop.Coordinate.Elevation.Value.ToInvariantString());
                        }
                        jsonWriter.WriteArrayClose();
                        jsonWriter.WriteClose();

                        jsonWriter.WritePropertyName("properties");
                        jsonWriter.WriteOpen();
                        if (stop.Attributes != null)
                        {
                            var attributes = stop.Attributes;
                            if (attributesCallback != null)
                            {
                                attributes = new AttributeCollection(attributes);
                                attributesCallback(attributes);
                            }
                            foreach (var attribute in attributes)
                            {
                                var raw = isRaw != null &&
                                          isRaw(attribute.Key, attribute.Value);
                                jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
                            }
                        }
                        jsonWriter.WriteClose();

                        jsonWriter.WriteClose();
                    }
                }
            }
            else
            { // include shape meta as points if requested.
                if (route.Shape != null)
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Feature", true, false);
                    jsonWriter.WriteProperty("name", "Shape", true, false);
                    jsonWriter.WritePropertyName("properties");
                    jsonWriter.WriteOpen();
                    jsonWriter.WriteClose();
                    jsonWriter.WritePropertyName("geometry", false);

                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "LineString", true, false);
                    jsonWriter.WritePropertyName("coordinates", false);
                    jsonWriter.WriteArrayOpen();
                    foreach (var t in route.Shape)
                    {
                        jsonWriter.WriteArrayOpen();
                        jsonWriter.WriteArrayValue(t.Longitude.ToInvariantString());
                        jsonWriter.WriteArrayValue(t.Latitude.ToInvariantString());
                        if (t.Elevation.HasValue)
                        {
                            jsonWriter.WriteArrayValue(t.Elevation.Value.ToInvariantString());
                        }
                        jsonWriter.WriteArrayClose();
                    }
                    jsonWriter.WriteArrayClose();
                    jsonWriter.WriteClose();

                    if (attributesCallback != null)
                    {
                        jsonWriter.WritePropertyName("properties");
                        jsonWriter.WriteOpen();
                        var attributes = new AttributeCollection();
                        attributesCallback(attributes);
                        foreach (var attribute in attributes)
                        {
                            var raw = isRaw != null &&
                                      isRaw(attribute.Key, attribute.Value);
                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
                        }
                        jsonWriter.WriteClose();
                    }

                    jsonWriter.WriteClose();
                }

                if (route.ShapeMeta != null &&
                    includeShapeMeta)
                {
                    foreach (var meta in route.ShapeMeta)
                    {
                        jsonWriter.WriteOpen();
                        jsonWriter.WriteProperty("type", "Feature", true, false);
                        jsonWriter.WriteProperty("name", "ShapeMeta", true, false);
                        jsonWriter.WriteProperty("Shape", meta.Shape.ToInvariantString(), true, false);
                        jsonWriter.WritePropertyName("geometry", false);

                        if (route.Shape != null)
                        {
                            var coordinate = route.Shape[meta.Shape];

                            jsonWriter.WriteOpen();
                            jsonWriter.WriteProperty("type", "Point", true, false);
                            jsonWriter.WritePropertyName("coordinates", false);
                            jsonWriter.WriteArrayOpen();
                            jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                            jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                            if (coordinate.Elevation.HasValue)
                            {
                                jsonWriter.WriteArrayValue(coordinate.Elevation.Value.ToInvariantString());
                            }
                        }

                        jsonWriter.WriteArrayClose();
                        jsonWriter.WriteClose();

                        jsonWriter.WritePropertyName("properties");
                        jsonWriter.WriteOpen();

                        if (meta.Attributes != null)
                        {
                            var attributes = meta.Attributes;
                            if (attributesCallback != null)
                            {
                                attributes = new AttributeCollection(attributes);
                                attributesCallback(attributes);
                            }
                            foreach (var attribute in attributes)
                            {
                                var raw = isRaw != null &&
                                          isRaw(attribute.Key, attribute.Value);
                                jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
                            }
                        }

                        jsonWriter.WriteClose();

                        jsonWriter.WriteClose();
                    }
                }

                if (route.Stops != null &&
                    includeStops)
                {
                    foreach (var stop in route.Stops)
                    {
                        jsonWriter.WriteOpen();
                        jsonWriter.WriteProperty("type", "Feature", true, false);
                        jsonWriter.WriteProperty("name", "Stop", true, false);
                        jsonWriter.WriteProperty("Shape", stop.Shape.ToInvariantString(), true, false);
                        jsonWriter.WritePropertyName("geometry", false);

                        jsonWriter.WriteOpen();
                        jsonWriter.WriteProperty("type", "Point", true, false);
                        jsonWriter.WritePropertyName("coordinates", false);
                        jsonWriter.WriteArrayOpen();
                        jsonWriter.WriteArrayValue(stop.Coordinate.Longitude.ToInvariantString());
                        jsonWriter.WriteArrayValue(stop.Coordinate.Latitude.ToInvariantString());
                        if (stop.Coordinate.Elevation.HasValue)
                        {
                            jsonWriter.WriteArrayValue(stop.Coordinate.Elevation.Value.ToInvariantString());
                        }
                        jsonWriter.WriteArrayClose();
                        jsonWriter.WriteClose();

                        jsonWriter.WritePropertyName("properties");
                        jsonWriter.WriteOpen();
                        if (stop.Attributes != null)
                        {
                            var attributes = stop.Attributes;
                            if (attributesCallback != null)
                            {
                                attributes = new AttributeCollection(attributes);
                                attributesCallback(attributes);
                            }
                            foreach (var attribute in attributes)
                            {
                                var raw = isRaw != null &&
                                          isRaw(attribute.Key, attribute.Value);
                                jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
                            }
                        }
                        jsonWriter.WriteClose();

                        jsonWriter.WriteClose();
                    }
                }
            }
        }
    }
}