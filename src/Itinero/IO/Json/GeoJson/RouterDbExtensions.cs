using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;

namespace Itinero.IO.Json.GeoJson {
    /// <summary>
    /// Contains router db geojson extensions.
    /// </summary>
    public static class RouterDbExtensions {
        public static string ToGeoJson(this RoutingNetwork routerDb,
            ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
                bottomRight) box) {
            using var stream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(stream)) {
                jsonWriter.WriteFeatureCollectionStart();
                jsonWriter.WriteFeatures(routerDb, box);
                jsonWriter.WriteFeatureCollectionEnd();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes features to the given json writer.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="box">The bounding box.</param>
        /// <param name="jsonWriter">The json writer.</param>
        public static void WriteFeatures(this Utf8JsonWriter jsonWriter, RoutingNetwork routerDb,
            ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
                bottomRight) box) {
            var vertices = new HashSet<VertexId>();
            var edges = new HashSet<EdgeId>();

            var edgeEnumerator = routerDb.SearchEdgesInBox(box);
            while (edgeEnumerator.MoveNext()) {
                var vertex1 = edgeEnumerator.From;
                if (!vertices.Contains(vertex1)) {
                    jsonWriter.WriteVertexFeature(vertex1, routerDb);
                    vertices.Add(vertex1);
                }

                var vertex2 = edgeEnumerator.To;
                if (!vertices.Contains(vertex2)) {
                    jsonWriter.WriteVertexFeature(vertex2, routerDb);
                    vertices.Add(vertex2);
                }

                var edge = edgeEnumerator.Id;
                if (!edges.Contains(edge)) {
                    jsonWriter.WriteEdgeFeature(edgeEnumerator);
                    edges.Add(edge);
                }
            }
        }

        /// <summary>
        /// Writes a vertex as a feature.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="vertexId">The vertex id.</param>
        public static void WriteVertexFeature(this Utf8JsonWriter jsonWriter, VertexId vertexId,
            RoutingNetwork routerDb) {
            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteProperties(new (string key, string value)[] {
                ("tile_id", vertexId.TileId.ToString()),
                ("local_id", vertexId.LocalId.ToString())
            });
            jsonWriter.WritePropertyName("geometry");
            jsonWriter.WritePoint(routerDb.GetVertex(vertexId));
            jsonWriter.WriteFeatureEnd();
        }

        /// <summary>
        /// Writes an edge as a feature.
        /// </summary>
        /// <param name="jsonWriter">The json writer.</param>
        /// <param name="enumerator">The enumerator.</param>
        public static void WriteEdgeFeature(this Utf8JsonWriter jsonWriter,
            IEdgeEnumerator<RoutingNetwork> enumerator) {
            jsonWriter.WriteFeatureStart();
            var attributes = enumerator.Attributes.ToList();
            attributes.AddRange(new (string key, string value)[] {
                ("vertex1_tile_id", enumerator.From.TileId.ToString()),
                ("vertex1_local_id", enumerator.From.LocalId.ToString()),
                ("vertex2_tile_id", enumerator.To.TileId.ToString()),
                ("vertex2_local_id", enumerator.To.LocalId.ToString())
            });
            jsonWriter.WriteProperties(attributes);
            jsonWriter.WritePropertyName("geometry");
            jsonWriter.WriteLineString(enumerator.GetCompleteShape<IEdgeEnumerator<RoutingNetwork>, RoutingNetwork>());
            jsonWriter.WriteFeatureEnd();
        }
    }
}