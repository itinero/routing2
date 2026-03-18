using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Itinero.Geo;
using Itinero.MapMatching.Model;
using Itinero.Network;
using Itinero.Snapping;

namespace Itinero.MapMatching.IO.GeoJson;

public static class GraphModelExtensions
{
    public static string ToGeoJson(this GraphModel graphModel, RoutingNetwork routingNetwork)
    {
        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            jsonWriter.WriteFeatureCollectionStart();
            jsonWriter.WriteFeatures(graphModel, routingNetwork);
            jsonWriter.WriteFeatureCollectionEnd();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static void WriteFeatures(this Utf8JsonWriter jsonWriter, GraphModel graphModel, RoutingNetwork routingNetwork)
    {
        var track = graphModel.Track;
        jsonWriter.WriteFeatures(track);

        for (var n = 0; n < graphModel.Count; n++)
        {
            var node = graphModel.GetNode(n);
            if (node.SnapPoint == null) continue;

            var nodeLocation = node.SnapPoint.Value.LocationOnNetwork(routingNetwork);
            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteGeometryStart();
            jsonWriter.WritePoint((nodeLocation));
            jsonWriter.WriteProperties(new (string key, string value)[]
            {
                ("type", "node"),
                ("node-id", n.ToString()),
                ("track-point-id", node.TrackPoint?.ToString() ?? string.Empty),
                ("snappoint-edge-id", node.SnapPoint.Value.EdgeId.ToString()),
                ("snappoint-offset", node.SnapPoint.Value.Offset.ToString()),
                ("cost", node.Cost.ToString(CultureInfo.InvariantCulture))
            });
            jsonWriter.WriteFeatureEnd();

            if (node.TrackPoint == null) continue;
            var trackPoint = track[node.TrackPoint.Value];

            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteGeometryStart();
            jsonWriter.WriteLineString(new[]
            {
                nodeLocation,
                (trackPoint.Location.longitude, trackPoint.Location.latitude, null)
            });
            var length =
                nodeLocation.DistanceEstimateInMeter(
                    (trackPoint.Location.longitude, trackPoint.Location.latitude, null));
            jsonWriter.WriteProperties(new (string key, string value)[]
            {
                ("type", "node-offset"),
                ("node-id", n.ToString()),
                ("track-point-id", node.TrackPoint.Value.ToString()),
                ("snappoint-offset", node.SnapPoint.Value.Offset.ToString()),
                ("length", length.ToString(CultureInfo.InvariantCulture)),
                ("cost", node.Cost.ToString(CultureInfo.InvariantCulture))
            });
            jsonWriter.WriteFeatureEnd();

            var edges = graphModel.GetNeighbours(n);
            foreach (var edge in edges)
            {
                var node1 = graphModel.GetNode(edge.Node1);
                if (node1.SnapPoint == null) continue;
                var node2 = graphModel.GetNode(edge.Node2);
                if (node2.SnapPoint == null) continue;

                var node1Location = node1.SnapPoint.Value.LocationOnNetwork(routingNetwork);
                var node2Location = node2.SnapPoint.Value.LocationOnNetwork(routingNetwork);
                length =
                    node1Location.DistanceEstimateInMeter(
                        node2Location);

                jsonWriter.WriteFeatureStart();
                jsonWriter.WriteGeometryStart();
                jsonWriter.WriteLineString(new[]
                {
                    node1Location,
                    node2Location
                });
                var attributes = new List<(string key, string value)>
                {
                    ("cost", edge.Cost.ToString(CultureInfo.InvariantCulture)),
                    ("node-id1", edge.Node1.ToString()),
                    ("node-id2", edge.Node2.ToString()),
                    ("track-point-1", node1.TrackPoint?.ToString() ?? string.Empty),
                    ("track-point-2", node2.TrackPoint?.ToString() ?? string.Empty),
                    ("length", length.ToString(CultureInfo.InvariantCulture))
                };
                if (edge.Attributes != null) attributes.AddRange(edge.Attributes);
                jsonWriter.WriteProperties(attributes);
                jsonWriter.WriteFeatureEnd();
            }
        }
    }
}
