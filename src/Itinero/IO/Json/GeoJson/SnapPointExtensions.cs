using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Itinero.Network;
using Itinero.Snapping;

namespace Itinero.IO.Json.GeoJson
{
    /// <summary>
    /// Contains route extensions 
    /// </summary>
    public static class SnapPointExtensions
    {
        /// <summary>
        /// Returns geojson for the given snap point.
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <returns>A geojson string.</returns>
        public static string ToGeoJson(this SnapPoint snapPoint, RoutingNetwork routerDb)
        {
            using var stream = new MemoryStream();

            using (var jsonWriter = new Utf8JsonWriter(stream)) {
                jsonWriter.WriteFeatureCollectionStart();
                WriteFeatures(jsonWriter, snapPoint, routerDb);
                jsonWriter.WriteFeatureCollectionEnd();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes a geojson feature collection to the given json writer.
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatures(this Utf8JsonWriter jsonWriter, SnapPoint snapPoint, RoutingNetwork routerDb)
        {
            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteProperties(Enumerable.Empty<(string key, string value)>());

            jsonWriter.WritePropertyName("geometry");
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDb);
            jsonWriter.WritePoint(locationOnNetwork);

            jsonWriter.WriteFeatureEnd();
        }
    }
}