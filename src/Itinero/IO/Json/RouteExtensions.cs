using System;
using System.IO;

namespace Itinero.IO.Json
{
    /// <summary>
    /// Contains extension methods to serialize a route to json.
    /// </summary>
    public static class RouteExtensions
    {
        /// <summary>
        /// Converts the given route to geojson.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <returns>The geojson string.</returns>
        public static string ToGeoJson(this Route route)
        {
            var stringWriter = new StringWriter();
            route.WriteJson(stringWriter);
            return stringWriter.ToInvariantString();
        }

        /// <summary>
        /// Writes the route as json.
        /// </summary>
        internal static void WriteJson(this Route route, TextWriter writer)
        {
            if (route == null) { throw new ArgumentNullException(nameof(route)); }
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }

            var jsonWriter = new IO.Json.JsonWriter(writer);
            jsonWriter.WriteOpen();
            if (route.Attributes != null)
            {
                jsonWriter.WritePropertyName("Attributes");
                jsonWriter.WriteOpen();
                foreach (var attribute in route.Attributes)
                {
                    jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                }

                jsonWriter.WriteClose();
            }
            if (route.Shape != null)
            {
                jsonWriter.WritePropertyName("Shape");
                jsonWriter.WriteArrayOpen();
                foreach (var t in route.Shape)
                {
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(t.Longitude.ToInvariantString());
                    jsonWriter.WriteArrayValue(t.Latitude.ToInvariantString());
                    jsonWriter.WriteArrayClose();
                }
                jsonWriter.WriteArrayClose();
            }

            if (route.ShapeMeta != null)
            {
                jsonWriter.WritePropertyName("ShapeMeta");
                jsonWriter.WriteArrayOpen();
                foreach (var meta in route.ShapeMeta)
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WritePropertyName("Shape");
                    jsonWriter.WritePropertyValue(meta.Shape.ToInvariantString());

                    if (meta.Attributes != null)
                    {
                        jsonWriter.WritePropertyName("Attributes");
                        jsonWriter.WriteOpen();
                        foreach (var attribute in meta.Attributes)
                        {
                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                        }
                        jsonWriter.WriteClose();
                    }
                    jsonWriter.WriteClose();
                }
                jsonWriter.WriteArrayClose();
            }

            if (route.Stops != null)
            {
                jsonWriter.WritePropertyName("Stops");
                jsonWriter.WriteArrayOpen();
                foreach (var t in route.Stops)
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WritePropertyName("Shape");
                    jsonWriter.WritePropertyValue(t.Shape.ToInvariantString());
                    jsonWriter.WritePropertyName("Coordinates");
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(t.Coordinate.Longitude.ToInvariantString());
                    jsonWriter.WriteArrayValue(t.Coordinate.Latitude.ToInvariantString());
                    jsonWriter.WriteArrayClose();

                    if (t.Attributes != null)
                    {
                        jsonWriter.WritePropertyName("Attributes");
                        jsonWriter.WriteOpen();
                        foreach (var attribute in t.Attributes)
                        {
                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                        }
                        jsonWriter.WriteClose();
                    }
                    jsonWriter.WriteClose();
                }
                jsonWriter.WriteArrayClose();
            }

            if (route.Branches != null)
            {
                jsonWriter.WritePropertyName("Branches");
                jsonWriter.WriteArrayOpen();
                foreach (var t in route.Branches)
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WritePropertyName("Shape");
                    jsonWriter.WritePropertyValue(t.Shape.ToInvariantString());
                    jsonWriter.WritePropertyName("Coordinates");
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(t.Coordinate.Longitude.ToInvariantString());
                    jsonWriter.WriteArrayValue(t.Coordinate.Latitude.ToInvariantString());
                    jsonWriter.WriteArrayClose();

                    if (t.Attributes != null)
                    {
                        jsonWriter.WritePropertyName("Attributes");
                        jsonWriter.WriteOpen();
                        foreach (var attribute in t.Attributes)
                        {
                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                        }
                        jsonWriter.WriteClose();
                    }
                    jsonWriter.WriteClose();
                }
                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteClose();
        }
    }
}