using System.Collections.Generic;
using System.Text.Json;

namespace Itinero.IO.Json.GeoJson
{
    /// <summary>
    /// Geojson writing extensions.
    /// </summary>
    public static class GeoJsonExtensions
    {
        /// <summary>
        /// Writes feature collection start.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatureCollectionStart(this Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("type", "FeatureCollection");
            jsonWriter.WritePropertyName("features");
            jsonWriter.WriteStartArray();
        }

        /// <summary>
        /// Writes feature collection end.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatureCollectionEnd(this Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        /// <summary>
        /// Writes a feature start.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatureStart(this Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("type", "Feature");
        }

        /// <summary>
        /// Writes feature end.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatureEnd(this Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteEndObject();
        }

        /// <summary>
        /// Writes a point.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        /// <param name="location">The location.</param>
        public static void WritePoint(this Utf8JsonWriter jsonWriter,
            (double longitude, double latitude, float? e) location)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("type", "Point");
            jsonWriter.WritePropertyName("coordinates");

            jsonWriter.WriteStartArray();
            jsonWriter.WriteNumberValue(location.longitude);
            jsonWriter.WriteNumberValue(location.latitude);
            if (location.e != null)
            {
                jsonWriter.WriteNumberValue(location.e.Value);
            }

            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        /// <summary>
        /// Writes a line string.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        /// <param name="coordinates">The coordinates.</param>
        public static void WriteLineString(this Utf8JsonWriter jsonWriter,
            IEnumerable<(double longitude, double latitude, float? e)> coordinates)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("type", "LineString");
            jsonWriter.WritePropertyName("coordinates");

            jsonWriter.WriteStartArray();
            foreach (var c in coordinates)
            {
                jsonWriter.WriteStartArray();
                jsonWriter.WriteNumberValue(c.longitude);
                jsonWriter.WriteNumberValue(c.latitude);
                //                    if (route.Shape[i].Elevation.HasValue)
                //                    {
                //                        jsonWriter.WriteArrayValue(route.Shape[i].Elevation.Value.ToInvariantString());
                //                    }
                jsonWriter.WriteEndArray();
            }

            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        /// <summary>
        /// Writes properties.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        /// <param name="attributes">The attributes to write as properties.</param>
        public static void WriteProperties(this Utf8JsonWriter jsonWriter,
            IEnumerable<(string key, string value)> attributes)
        {
            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteStartObject();
            foreach (var a in attributes)
            {
                jsonWriter.WriteString(a.key, a.value);
            }

            jsonWriter.WriteEndObject();
        }
    }
}
