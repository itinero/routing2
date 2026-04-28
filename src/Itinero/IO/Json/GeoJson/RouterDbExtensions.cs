using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Itinero.Network;
using Itinero.Network.Attributes;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Network.Search.Edges;
using Itinero.Network.TurnCosts;
using Itinero.Profiles;

namespace Itinero.IO.Json.GeoJson;

/// <summary>
/// Contains router db geojson extensions.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Gets a geojson representation of the data in the router db.
    /// </summary>
    /// <param name="routerDb">The router db.</param>
    /// <param name="box">The bbox of the part of the network to extract.</param>
    /// <param name="profiles">The profiles, if output related to profiles is needed.</param>
    /// <returns>A string with geojson.</returns>
    public static string ToGeoJson(this RoutingNetwork routerDb,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight)? box = null, IEnumerable<Profile>? profiles = null)
    {
        profiles ??= ArraySegment<Profile>.Empty;

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
    /// Gets a geojson representation of given edge in the routing network.
    /// </summary>
    /// <param name="routerDb">The routing network.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <returns>A string with geojson.</returns>
    public static string ToGeoJson(this RoutingNetwork routerDb, EdgeId edgeId)
    {
        var edgeEnumerator = routerDb.GetEdgeEnumerator();
        if (!edgeEnumerator.MoveTo(edgeId)) throw new Exception("Edge not found");

        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            jsonWriter.WriteFeatureCollectionStart();
            jsonWriter.WriteEdgeFeature(edgeEnumerator);
            jsonWriter.WriteFeatureCollectionEnd();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Writes features to the given json writer.
    /// </summary>
    /// <param name="jsonWriter">The json writer.</param>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="box">The bounding box.</param>
    public static void WriteFeatures(this Utf8JsonWriter jsonWriter, RoutingNetwork routingNetwork,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight)? box)
    {
        var vertices = new HashSet<VertexId>();
        var edges = new HashSet<EdgeId>();

        if (box == null)
        {
            var vertexEnumerator = routingNetwork.GetVertexEnumerator();
            var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
            while (vertexEnumerator.MoveNext())
            {
                edgeEnumerator.MoveTo(vertexEnumerator.Current);

                while (edgeEnumerator.MoveNext())
                {
                    var tail = edgeEnumerator.Tail;
                    if (!vertices.Contains(tail))
                    {
                        jsonWriter.WriteVertexFeature(tail, routingNetwork);
                        vertices.Add(tail);
                    }

                    var head = edgeEnumerator.Head;
                    if (!vertices.Contains(head))
                    {
                        jsonWriter.WriteVertexFeature(head, routingNetwork);
                        vertices.Add(head);
                    }

                    var edge = edgeEnumerator.EdgeId;
                    if (!edges.Contains(edge))
                    {
                        jsonWriter.WriteEdgeFeature(routingNetwork, edgeEnumerator);
                        edges.Add(edge);
                    }
                }
            }
        }
        else
        {
            var edgeEnumerator = routingNetwork.SearchEdgesInBox(box.Value);
            while (edgeEnumerator.MoveNext())
            {
                var tail = edgeEnumerator.Tail;
                if (!vertices.Contains(tail))
                {
                    jsonWriter.WriteVertexFeature(tail, routingNetwork);
                    vertices.Add(tail);
                }

                var head = edgeEnumerator.Head;
                if (!vertices.Contains(head))
                {
                    jsonWriter.WriteVertexFeature(head, routingNetwork);
                    vertices.Add(head);
                }

                var edge = edgeEnumerator.EdgeId;
                if (!edges.Contains(edge))
                {
                    jsonWriter.WriteEdgeFeature(routingNetwork, edgeEnumerator);
                    edges.Add(edge);
                }
            }
        }

        // write turn cost features.
        jsonWriter.WriteTurnCostFeatures(routingNetwork, edges);
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
            ("_tile_id", vertexId.TileId.ToString()), ("_local_id", vertexId.LocalId.ToString())
        });
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WritePoint(location);
        jsonWriter.WriteFeatureEnd();
    }

    /// <summary>
    /// Writes an edge as a feature.
    /// </summary>
    /// <param name="jsonWriter">The json writer.</param>
    /// <param name="routingNetwork">The routing network db.</param>
    /// <param name="enumerator">The enumerator.</param>
    public static void WriteEdgeFeature(this Utf8JsonWriter jsonWriter,
        RoutingNetwork routingNetwork, IEdgeEnumerator enumerator)
    {
        jsonWriter.WriteFeatureStart();
        var attributes = enumerator.Attributes.ToList();
        if (enumerator.Forward)
        {
            attributes.AddRange(new (string key, string value)[]
            {
                ("_tail_tile_id", enumerator.Tail.TileId.ToString()),
                ("_tail_local_id", enumerator.Tail.LocalId.ToString()),
                ("_head_tile_id", enumerator.Head.TileId.ToString()),
                ("_head_local_id", enumerator.Head.LocalId.ToString()),
                ("_edge_id", enumerator.EdgeId.ToString())
            });
        }
        else
        {
            attributes.AddRange(new (string key, string value)[]
            {
                ("_head_tile_id", enumerator.Tail.TileId.ToString()),
                ("_head_local_id", enumerator.Tail.LocalId.ToString()),
                ("_tail_tile_id", enumerator.Head.TileId.ToString()),
                ("_tail_local_id", enumerator.Head.LocalId.ToString()),
                ("_edge_id", enumerator.EdgeId.ToString())
            });
        }

        if (enumerator.TailOrder.HasValue) attributes.AddOrReplace("_tail_order", enumerator.TailOrder.Value.ToString());
        if (enumerator.HeadOrder.HasValue) attributes.AddOrReplace("_head_order", enumerator.HeadOrder.Value.ToString());

        foreach (var profileName in routingNetwork.RouterDb.ProfileConfiguration.GetProfileNames())
        {
            if (!routingNetwork.RouterDb.ProfileConfiguration.TryGetProfileHandlerEdgeTypesCache(profileName, out var edgeFactorCache,
                out _)) continue;

            if (edgeFactorCache == null) continue;
            if (!enumerator.EdgeTypeId.HasValue) continue;

            var factor = edgeFactorCache.Get(enumerator.EdgeTypeId.Value);
            if (factor == null) continue;

            attributes.AddOrReplace($"_{profileName}_factor_forward",
                factor.Value.ForwardFactor.ToString(System.Globalization.CultureInfo.InvariantCulture));
            attributes.AddOrReplace($"_{profileName}_factor_backward",
                factor.Value.ForwardFactor.ToString(System.Globalization.CultureInfo.InvariantCulture));
            attributes.AddOrReplace($"_{profileName}_speed_forward",
                factor.Value.ForwardSpeedMeterPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture));
            attributes.AddOrReplace($"_{profileName}_speed_backward",
                factor.Value.BackwardSpeedMeterPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!routingNetwork.IslandManager.TryGetIslandsFor(profileName, out var islands)) continue;

            if (factor.Value.ForwardFactor > 0 || factor.Value.BackwardFactor > 0)
            {
                if (islands.GetTileDone(enumerator.Tail.TileId))
                {
                    attributes.AddOrReplace($"_{profileName}_island",
                        islands.IsEdgeOnIsland(enumerator.EdgeId).ToString().ToLowerInvariant());
                }
                else
                {
                    var status = routingNetwork.IslandManager.IsEdgeOnIsland(profileName, enumerator.EdgeId);
                    attributes.AddOrReplace($"_{profileName}_island",
                        status switch
                        {
                            true => "true",
                            false => "false",
                            null => "unknown"
                        });
                }
            }
        }

        jsonWriter.WriteProperties(attributes);
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WriteLineString(enumerator.GetCompleteShape());
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
            ("tail_tile_id", enumerator.Tail.TileId.ToString()),
            ("tail_local_id", enumerator.Tail.LocalId.ToString()),
            ("head_tile_id", enumerator.Head.TileId.ToString()),
            ("head_local_id", enumerator.Head.LocalId.ToString()), ("edge_id", enumerator.EdgeId.ToString())
        });
        if (enumerator.TailOrder.HasValue) attributes.AddOrReplace("tail_order", enumerator.TailOrder.Value.ToString());
        if (enumerator.HeadOrder.HasValue) attributes.AddOrReplace("head_order", enumerator.HeadOrder.Value.ToString());
        jsonWriter.WriteProperties(attributes);
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WriteLineString(enumerator.GetCompleteShape());
        jsonWriter.WriteFeatureEnd();
    }

    /// <summary>
    /// Writes turn cost features for all edges in the set.
    /// </summary>
    public static void WriteTurnCostFeatures(this Utf8JsonWriter jsonWriter, RoutingNetwork routingNetwork,
        HashSet<EdgeId> edges)
    {
        var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
        var secondEdgeEnumerator = routingNetwork.GetEdgeEnumerator();

        foreach (var edgeId in edges)
        {
            if (!edgeEnumerator.MoveTo(edgeId)) continue;

            // turn costs from head (edge traversed tail→head, then turning).
            if (edgeEnumerator.HeadOrder != null)
            {
                secondEdgeEnumerator.MoveTo(edgeEnumerator.Head);
                while (secondEdgeEnumerator.MoveNext())
                {
                    if (secondEdgeEnumerator.EdgeId == edgeEnumerator.EdgeId) continue;
                    if (secondEdgeEnumerator.TailOrder == null) continue;

                    foreach (var turnCost in
                             edgeEnumerator.GetTurnCostFromHead(secondEdgeEnumerator.TailOrder.Value))
                    {
                        if (turnCost.cost == 0) continue;

                        var shape = edgeEnumerator.GetCompleteShape()
                            .Concat(secondEdgeEnumerator.GetCompleteShape());
                        jsonWriter.WriteTurnCostFeature(edgeEnumerator.EdgeId, secondEdgeEnumerator.EdgeId,
                            turnCost, OffsetRight(shape, 5.0));
                    }
                }
            }

            // turn costs from tail (edge traversed head→tail, then turning).
            if (edgeEnumerator.TailOrder != null)
            {
                secondEdgeEnumerator.MoveTo(edgeEnumerator.Tail);
                while (secondEdgeEnumerator.MoveNext())
                {
                    if (secondEdgeEnumerator.EdgeId == edgeEnumerator.EdgeId) continue;
                    if (secondEdgeEnumerator.TailOrder == null) continue;

                    foreach (var turnCost in
                             edgeEnumerator.GetTurnCostFromTail(secondEdgeEnumerator.TailOrder.Value))
                    {
                        if (turnCost.cost == 0) continue;

                        var shape = edgeEnumerator.GetCompleteShape().Reverse()
                            .Concat(secondEdgeEnumerator.GetCompleteShape());
                        jsonWriter.WriteTurnCostFeature(edgeEnumerator.EdgeId, secondEdgeEnumerator.EdgeId,
                            turnCost, OffsetRight(shape, 5.0));
                    }
                }
            }
        }
    }

    private static void WriteTurnCostFeature(this Utf8JsonWriter jsonWriter,
        EdgeId fromEdge, EdgeId toEdge,
        (uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost,
            IEnumerable<EdgeId> prefixEdges) turnCost,
        IEnumerable<(double longitude, double latitude, float? e)> shape)
    {
        jsonWriter.WriteFeatureStart();
        var attributes = turnCost.attributes.ToList();
        attributes.AddRange(new (string key, string value)[]
        {
            ("_type", "turn_cost"),
            ("_from_edge", fromEdge.ToString()),
            ("_to_edge", toEdge.ToString()),
            ("_prefix", string.Join(",", turnCost.prefixEdges.Select(x => x.ToString()))),
            ("_cost", turnCost.cost.ToString()),
            ("_turn_cost_type", turnCost.turnCostType.ToString())
        });
        jsonWriter.WriteProperties(attributes);
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WriteLineString(shape);
        jsonWriter.WriteFeatureEnd();
    }

    private static IEnumerable<(double longitude, double latitude, float? e)> OffsetRight(
        IEnumerable<(double longitude, double latitude, float? e)> coordinates, double offsetMeters)
    {
        var coords = coordinates.ToList();
        if (coords.Count < 2)
        {
            foreach (var c in coords) yield return c;
            yield break;
        }

        for (var i = 0; i < coords.Count; i++)
        {
            var (lon, lat, e) = coords[i];

            // get direction from adjacent points.
            double dx, dy;
            if (i == 0)
            {
                dx = coords[i + 1].longitude - lon;
                dy = coords[i + 1].latitude - lat;
            }
            else if (i == coords.Count - 1)
            {
                dx = lon - coords[i - 1].longitude;
                dy = lat - coords[i - 1].latitude;
            }
            else
            {
                dx = coords[i + 1].longitude - coords[i - 1].longitude;
                dy = coords[i + 1].latitude - coords[i - 1].latitude;
            }

            // convert to meters for proper normalization.
            var latRad = lat * Math.PI / 180.0;
            var cosLat = Math.Cos(latRad);
            var dxM = dx * 111320.0 * cosLat;
            var dyM = dy * 111320.0;

            var lenM = Math.Sqrt(dxM * dxM + dyM * dyM);
            if (lenM < 0.001)
            {
                yield return (lon, lat, e);
                continue;
            }

            // right perpendicular in meters (rotate 90° clockwise).
            var perpXM = dyM / lenM * offsetMeters;
            var perpYM = -dxM / lenM * offsetMeters;

            // convert back to degrees.
            yield return (lon + perpXM / (111320.0 * cosLat), lat + perpYM / 111320.0, e);
        }
    }
}
