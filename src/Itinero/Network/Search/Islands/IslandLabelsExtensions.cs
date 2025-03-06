using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Itinero.IO.Json.GeoJson;
using Itinero.Network.Attributes;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search.Islands;

internal static class IslandLabelsExtensions
{
    internal static (uint label, uint size, bool final) GetOrCreateLabel(this IslandLabels labels, RoutingNetworkEdgeEnumerator edgeEnumerator,
        Func<IEdgeEnumerator, bool?>? isOnIslandAlready = null)
    {
        if (labels.TryGetWithDetails(edgeEnumerator.EdgeId, out var neighbourLabelDetails)) return neighbourLabelDetails;

        // check if the neighbour has a status already we can use.
        var onIslandAlready = isOnIslandAlready?.Invoke(edgeEnumerator);
        if (onIslandAlready != null)
        {
            // check and verify status
            if (onIslandAlready.Value)
            {
                // neighbour has a known status and is on an island.
                neighbourLabelDetails = labels.AddNew(edgeEnumerator.EdgeId, true);
            }
            else
            {
                // neighbour has a known status and is not on an island.
                neighbourLabelDetails = labels.AddTo(IslandLabels.NotAnIslandLabel, edgeEnumerator.EdgeId);
            }
        }
        else
        {
            // no label was found, assign a new one.
            neighbourLabelDetails = labels.AddNew(edgeEnumerator.EdgeId);
        }

        return neighbourLabelDetails;
    }

    public static async Task<string> ToGeoJson(this IslandLabels islandLabels, RoutingNetwork network)
    {
        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            jsonWriter.WriteFeatureCollectionStart();

            var edgeEnumerator = network.GetEdgeEnumerator();
            foreach (var (edge, label) in islandLabels)
            {
                if (!edgeEnumerator.MoveTo(edge)) continue;
                if (!islandLabels.TryGetWithDetails(edge, out var edgeDetails)) continue;

                jsonWriter.WriteEdgeFeatureWithIslandDetails(network, edgeEnumerator, edgeDetails);
            }

            jsonWriter.WriteFeatureCollectionEnd();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static void WriteEdgeFeatureWithIslandDetails(this Utf8JsonWriter jsonWriter,
        RoutingNetwork routingNetwork, RoutingNetworkEdgeEnumerator enumerator, (uint label, uint size, bool final) islandDetails)
    {
        jsonWriter.WriteFeatureStart();
        var attributes = enumerator.Attributes.ToList();
        if (enumerator.Forward)
        {
            attributes.AddRange([
                ("_tail_tile_id", enumerator.Tail.TileId.ToString()),
                ("_tail_local_id", enumerator.Tail.LocalId.ToString()),
                ("_head_tile_id", enumerator.Head.TileId.ToString()),
                ("_head_local_id", enumerator.Head.LocalId.ToString()),
                ("_edge_id", enumerator.EdgeId.ToString())
            ]);
        }
        else
        {
            attributes.AddRange([
                ("_head_tile_id", enumerator.Tail.TileId.ToString()),
                ("_head_local_id", enumerator.Tail.LocalId.ToString()),
                ("_tail_tile_id", enumerator.Head.TileId.ToString()),
                ("_tail_local_id", enumerator.Head.LocalId.ToString()),
                ("_edge_id", enumerator.EdgeId.ToString())
            ]);
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
        }

        var isIsland = islandDetails.size < routingNetwork.IslandManager.MaxIslandSize;
        attributes.AddOrReplace("_island_details_label", islandDetails.label.ToString(System.Globalization.CultureInfo.InvariantCulture));
        attributes.AddOrReplace("_island_details_size", islandDetails.size.ToString(System.Globalization.CultureInfo.InvariantCulture));
        attributes.AddOrReplace("_island_details_final", islandDetails.final.ToString(System.Globalization.CultureInfo.InvariantCulture));
        attributes.AddOrReplace("_island_is_island", isIsland.ToString(System.Globalization.CultureInfo.InvariantCulture));

        jsonWriter.WriteProperties(attributes);
        jsonWriter.WritePropertyName("geometry");
        jsonWriter.WriteLineString(enumerator.GetCompleteShape());
        jsonWriter.WriteFeatureEnd();
    }
}
