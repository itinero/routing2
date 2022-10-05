using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Itinero.Routes;

namespace Itinero.IO.Json.GeoJson;

/// <summary>
/// Contains route geojson extensions 
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Returns geojson for the given route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <returns>A geojson string.</returns>
    public static string ToGeoJson(this Route route)
    {
        using (var stream = new MemoryStream())
        {
            using (var jsonWriter = new Utf8JsonWriter(stream))
            {
                jsonWriter.WriteFeatureCollectionStart();
                jsonWriter.WriteFeatures(route);
                jsonWriter.WriteFeatureCollectionEnd();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    public static string ToGeoJson(this IReadOnlyList<Route> routes)
    {
        using (var stream = new MemoryStream())
        {
            using (var jsonWriter = new Utf8JsonWriter(stream))
            {
                jsonWriter.WriteFeatureCollectionStart();
                foreach (var route in routes)
                {
                    jsonWriter.WriteFeatures(route);
                }
                jsonWriter.WriteFeatureCollectionEnd();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Writes a few features representing the given route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="jsonWriter">The json writer.</param>
    public static void WriteFeatures(this Utf8JsonWriter jsonWriter, Route route)
    {
        if (route.Shape != null)
        {
            jsonWriter.WriteFeatureStart();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteStartObject();
            jsonWriter.WriteEndObject();

            jsonWriter.WritePropertyName("geometry");

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("type", "LineString");
            jsonWriter.WritePropertyName("coordinates");
            jsonWriter.WriteStartArray();
            for (var i = 0; i < route.Shape.Count; i++)
            {
                jsonWriter.WriteStartArray();
                jsonWriter.WriteNumberValue(route.Shape[i].longitude);
                jsonWriter.WriteNumberValue(route.Shape[i].latitude);
                //                    if (route.Shape[i].Elevation.HasValue)
                //                    {
                //                        jsonWriter.WriteArrayValue(route.Shape[i].Elevation.Value.ToInvariantString());
                //                    }

                jsonWriter.WriteEndArray();
            }

            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();

            //                if (attributesCallback != null)
            //                {
            //                    jsonWriter.WritePropertyName("properties");
            //                    jsonWriter.WriteOpen();
            //                    var attributes = new AttributeCollection();
            //                    attributesCallback(attributes);
            //                    foreach (var attribute in attributes)
            //                    {
            //                        var raw = isRaw != null &&
            //                                  isRaw(attribute.Key, attribute.Value);
            //                        jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
            //                    }
            //
            //                    jsonWriter.WriteClose();
            //                }

            jsonWriter.WriteFeatureEnd();
        }

        //
        //            if (route.ShapeMeta != null &&
        //                includeShapeMeta)
        //            {
        //                for (var i = 0; i < route.ShapeMeta.Length; i++)
        //                {
        //                    var meta = route.ShapeMeta[i];
        //
        //                    jsonWriter.WriteOpen();
        //                    jsonWriter.WriteProperty("type", "Feature", true, false);
        //                    jsonWriter.WriteProperty("name", "ShapeMeta", true, false);
        //                    jsonWriter.WriteProperty("Shape", meta.Shape.ToInvariantString(), true, false);
        //                    jsonWriter.WritePropertyName("geometry", false);
        //
        //                    var coordinate = route.Shape[meta.Shape];
        //
        //                    jsonWriter.WriteOpen();
        //                    jsonWriter.WriteProperty("type", "Point", true, false);
        //                    jsonWriter.WritePropertyName("coordinates", false);
        //                    jsonWriter.WriteArrayOpen();
        //                    jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
        //                    jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
        //                    if (coordinate.Elevation.HasValue)
        //                    {
        //                        jsonWriter.WriteArrayValue(coordinate.Elevation.Value.ToInvariantString());
        //                    }
        //
        //                    jsonWriter.WriteArrayClose();
        //                    jsonWriter.WriteClose();
        //
        //                    jsonWriter.WritePropertyName("properties");
        //                    jsonWriter.WriteOpen();
        //
        //                    if (meta.Attributes != null)
        //                    {
        //                        var attributes = meta.Attributes;
        //                        if (attributesCallback != null)
        //                        {
        //                            attributes = new AttributeCollection(attributes);
        //                            attributesCallback(attributes);
        //                        }
        //
        //                        foreach (var attribute in attributes)
        //                        {
        //                            var raw = isRaw != null &&
        //                                      isRaw(attribute.Key, attribute.Value);
        //                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
        //                        }
        //                    }
        //
        //                    jsonWriter.WriteClose();
        //
        //                    jsonWriter.WriteClose();
        //                }
        //            }
        //
        //            if (route.Stops != null &&
        //                includeStops)
        //            {
        //                for (var i = 0; i < route.Stops.Length; i++)
        //                {
        //                    var stop = route.Stops[i];
        //
        //                    jsonWriter.WriteOpen();
        //                    jsonWriter.WriteProperty("type", "Feature", true, false);
        //                    jsonWriter.WriteProperty("name", "Stop", true, false);
        //                    jsonWriter.WriteProperty("Shape", stop.Shape.ToInvariantString(), true, false);
        //                    jsonWriter.WritePropertyName("geometry", false);
        //
        //                    jsonWriter.WriteOpen();
        //                    jsonWriter.WriteProperty("type", "Point", true, false);
        //                    jsonWriter.WritePropertyName("coordinates", false);
        //                    jsonWriter.WriteArrayOpen();
        //                    jsonWriter.WriteArrayValue(stop.Coordinate.Longitude.ToInvariantString());
        //                    jsonWriter.WriteArrayValue(stop.Coordinate.Latitude.ToInvariantString());
        //                    if (stop.Coordinate.Elevation.HasValue)
        //                    {
        //                        jsonWriter.WriteArrayValue(stop.Coordinate.Elevation.Value.ToInvariantString());
        //                    }
        //
        //                    jsonWriter.WriteArrayClose();
        //                    jsonWriter.WriteClose();
        //
        //                    jsonWriter.WritePropertyName("properties");
        //                    jsonWriter.WriteOpen();
        //                    if (stop.Attributes != null)
        //                    {
        //                        var attributes = stop.Attributes;
        //                        if (attributesCallback != null)
        //                        {
        //                            attributes = new AttributeCollection(attributes);
        //                            attributesCallback(attributes);
        //                        }
        //
        //                        foreach (var attribute in attributes)
        //                        {
        //                            var raw = isRaw != null &&
        //                                      isRaw(attribute.Key, attribute.Value);
        //                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, !raw, !raw);
        //                        }
        //                    }
        //
        //                    jsonWriter.WriteClose();
        //
        //                    jsonWriter.WriteClose();
        //                }
        //            }
    }
}
