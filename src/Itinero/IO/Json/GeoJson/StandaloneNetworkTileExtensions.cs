using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone;

namespace Itinero.IO.Json.GeoJson;

/// <summary>
/// Contains extension methods to serialize standalone network tiles to geojson.
/// </summary>
public static class StandaloneNetworkTileExtensions
{
    /// <summary>
    /// Gets a geojson version of the network tile.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>A string with geojson.</returns>
    public static string ToGeoJson(this StandaloneNetworkTile tile)
    {
        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            jsonWriter.WriteFeatureCollectionStart();
            jsonWriter.WriteFeatures(tile);
            jsonWriter.WriteFeatureCollectionEnd();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Writes features to the given json writer.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <param name="jsonWriter">The json writer.</param>
    public static void WriteFeatures(this Utf8JsonWriter jsonWriter, StandaloneNetworkTile tile)
    {
        var edges = new HashSet<EdgeId>();

        var vertex = new VertexId(tile.TileId, 0);
        var edgeEnumerator = tile.GetEnumerator();
        var secondEdgeEnumerator = tile.GetEnumerator();
        while (edgeEnumerator.MoveTo(vertex))
        {
            // write vertex features.
            jsonWriter.WriteVertexFeature(vertex, edgeEnumerator.TailLocation);

            // iterate over all edges.
            while (edgeEnumerator.MoveNext())
            {
                if (edges.Contains(edgeEnumerator.EdgeId)) continue;
                edges.Add(edgeEnumerator.EdgeId);

                // write edge features.
                jsonWriter.WriteEdgeFeature(edgeEnumerator);

                if (edgeEnumerator.HeadOrder != null)
                {
                    // check out turn costs at head location.
                    secondEdgeEnumerator.MoveTo(edgeEnumerator.Head);
                    while (secondEdgeEnumerator.MoveNext())
                    {
                        if (secondEdgeEnumerator.EdgeId == edgeEnumerator.EdgeId) continue;
                        if (secondEdgeEnumerator.TailOrder == null) continue;

                        foreach (var turnCost in
                                 edgeEnumerator.GetTurnCostFromHead(secondEdgeEnumerator.TailOrder.Value))
                        {
                            if (turnCost.cost == 0) continue;

                            jsonWriter.WriteFeatureStart();
                            var attributes = turnCost.attributes.ToList();
                            attributes.AddRange(new (string key, string value)[]
                            {
                                ("edge1_tile_id", edgeEnumerator.EdgeId.TileId.ToString()),
                                ("edge1_local_id", edgeEnumerator.EdgeId.LocalId.ToString()),
                                ("edge2_tile_id", secondEdgeEnumerator.EdgeId.TileId.ToString()),
                                ("edge2_local_id", secondEdgeEnumerator.EdgeId.LocalId.ToString()),
                                ("edges_prefix", string.Join(",", turnCost.prefixEdges.Select(x => x.ToString()))),
                                ("cost", turnCost.cost.ToString())
                            });
                            jsonWriter.WriteProperties(attributes);
                            jsonWriter.WritePropertyName("geometry");
                            jsonWriter.WriteLineString(edgeEnumerator.GetCompleteShape()
                                .Concat(secondEdgeEnumerator.GetCompleteShape()));
                            jsonWriter.WriteFeatureEnd();
                        }
                    }
                }

                if (edgeEnumerator.TailOrder != null)
                {
                    // check out turn costs at tail location.
                    secondEdgeEnumerator.MoveTo(edgeEnumerator.Tail);
                    while (secondEdgeEnumerator.MoveNext())
                    {
                        if (secondEdgeEnumerator.EdgeId == edgeEnumerator.EdgeId) continue;
                        if (secondEdgeEnumerator.TailOrder == null) continue;

                        foreach (var turnCost in
                                 edgeEnumerator.GetTurnCostFromTail(secondEdgeEnumerator.TailOrder.Value))
                        {
                            if (turnCost.cost == 0) continue;

                            jsonWriter.WriteFeatureStart();
                            var attributes = turnCost.attributes.ToList();
                            attributes.AddRange(new (string key, string value)[]
                            {
                                ("edge1_tile_id", edgeEnumerator.EdgeId.TileId.ToString()),
                                ("edge1_local_id", edgeEnumerator.EdgeId.LocalId.ToString()),
                                ("edge2_tile_id", secondEdgeEnumerator.EdgeId.TileId.ToString()),
                                ("edge2_local_id", secondEdgeEnumerator.EdgeId.LocalId.ToString()),
                                ("edges_prefix", string.Join(",", turnCost.prefixEdges.Select(x => x.ToString()))),
                                ("cost", turnCost.cost.ToString())
                            });
                            jsonWriter.WriteProperties(attributes);
                            jsonWriter.WritePropertyName("geometry");
                            jsonWriter.WriteLineString(edgeEnumerator.GetCompleteShape().Reverse()
                                .Concat(secondEdgeEnumerator.GetCompleteShape()));
                            jsonWriter.WriteFeatureEnd();
                        }
                    }
                }
            }

            vertex = new VertexId(vertex.TileId, vertex.LocalId + 1);
        }
    }
}
