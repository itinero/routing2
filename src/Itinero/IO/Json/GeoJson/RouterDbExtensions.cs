using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;

namespace Itinero.IO.Json.GeoJson;

/// <summary>
/// Contains router db geojson extensions.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Gets a geojson representation of the data in the routerdb.
    /// </summary>
    /// <param name="routerDb"></param>
    /// <param name="box"></param>
    /// <returns></returns>
    public static string ToGeoJson(this RoutingNetwork routerDb,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight)? box = null)
    {
        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
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
            bottomRight)? box)
    {
        var vertices = new HashSet<VertexId>();
        var edges = new HashSet<EdgeId>();

        if (box == null)
        {
            var vertexEnumerator = routerDb.GetVertexEnumerator();
            var edgeEnumerator = routerDb.GetEdgeEnumerator();
            while (vertexEnumerator.MoveNext())
            {
                edgeEnumerator.MoveTo(vertexEnumerator.Current);

                while (edgeEnumerator.MoveNext())
                {
                    var vertex1 = edgeEnumerator.Tail;
                    if (!vertices.Contains(vertex1))
                    {
                        jsonWriter.WriteVertexFeature(vertex1, routerDb);
                        vertices.Add(vertex1);
                    }

                    var vertex2 = edgeEnumerator.Head;
                    if (!vertices.Contains(vertex2))
                    {
                        jsonWriter.WriteVertexFeature(vertex2, routerDb);
                        vertices.Add(vertex2);
                    }

                    var edge = edgeEnumerator.EdgeId;
                    if (!edges.Contains(edge))
                    {
                        jsonWriter.WriteEdgeFeature(edgeEnumerator);
                        edges.Add(edge);
                    }
                }
            }
        }
        else
        {
            var edgeEnumerator = routerDb.SearchEdgesInBox(box.Value);
            while (edgeEnumerator.MoveNext())
            {
                var vertex1 = edgeEnumerator.Tail;
                if (!vertices.Contains(vertex1))
                {
                    jsonWriter.WriteVertexFeature(vertex1, routerDb);
                    vertices.Add(vertex1);
                }

                var vertex2 = edgeEnumerator.Head;
                if (!vertices.Contains(vertex2))
                {
                    jsonWriter.WriteVertexFeature(vertex2, routerDb);
                    vertices.Add(vertex2);
                }

                var edge = edgeEnumerator.EdgeId;
                if (!edges.Contains(edge))
                {
                    jsonWriter.WriteEdgeFeature(edgeEnumerator);
                    edges.Add(edge);
                }
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
        RoutingNetwork routerDb)
    {
        jsonWriter.WriteVertexFeature(vertexId, routerDb.GetVertex(vertexId));
    }

    /// <summary>
    /// Writes a vertex as a feature.
    /// </summary>
    /// <param name="jsonWriter">The json writer.</param>
    /// <param name="vertexId">The vertex id.</param>
    /// <param name="location">The location.</param>
    public static void WriteVertexFeature(this Utf8JsonWriter jsonWriter, VertexId vertexId,
        (double longitude, double latitude, float? e) location)
    {
        jsonWriter.WriteFeatureStart();
        jsonWriter.WriteProperties(new (string key, string value)[]
        {
            ("tile_id", vertexId.TileId.ToString()), ("local_id", vertexId.LocalId.ToString())
        });
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WritePoint(location);
        jsonWriter.WriteFeatureEnd();
    }

    /// <summary>
    /// Writes an edge as a feature.
    /// </summary>
    /// <param name="jsonWriter">The json writer.</param>
    /// <param name="enumerator">The enumerator.</param>
    public static void WriteEdgeFeature(this Utf8JsonWriter jsonWriter,
        IEdgeEnumerator enumerator)
    {
        jsonWriter.WriteFeatureStart();
        var attributes = enumerator.Attributes.ToList();
        attributes.AddRange(new (string key, string value)[]
        {
            ("vertex1_tile_id", enumerator.Tail.TileId.ToString()),
            ("vertex1_local_id", enumerator.Tail.LocalId.ToString()),
            ("vertex2_tile_id", enumerator.Head.TileId.ToString()),
            ("vertex2_local_id", enumerator.Head.LocalId.ToString()), ("edge_id", enumerator.EdgeId.ToString())
        });
        jsonWriter.WriteProperties(attributes);
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WriteLineString(enumerator.GetCompleteShape());
        jsonWriter.WriteFeatureEnd();
    }
}
