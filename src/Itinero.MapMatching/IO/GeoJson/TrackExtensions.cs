using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Itinero.Geo;

namespace Itinero.MapMatching.IO.GeoJson;

public static class TrackExtensions
{
    public static string ToGeoJson(this Track track)
    {
        using var stream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            jsonWriter.WriteFeatureCollectionStart();
            jsonWriter.WriteFeatures(track);
            jsonWriter.WriteFeatureCollectionEnd();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static void WriteFeatures(this Utf8JsonWriter jsonWriter, Track track)
    {
        for (var n = 0; n < track.Count; n++)
        {
            var trackPoint = track[n];

            var nodeLocation = (trackPoint.Location.longitude, trackPoint.Location.latitude, (float?)null);
            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteGeometryStart();
            jsonWriter.WritePoint((nodeLocation));
            jsonWriter.WriteProperties(new (string key, string value)[]
            {
                ("type", "track-point"), ("track-point-id", n.ToString())
            });
            jsonWriter.WriteFeatureEnd();

            if (n == 0) continue;

            var previousTrackPoint = track[n - 1];
            var previousLocation = (previousTrackPoint.Location.longitude, previousTrackPoint.Location.latitude,
                (float?)null);
            var length =
                nodeLocation.DistanceEstimateInMeter(
                    previousLocation);
            jsonWriter.WriteFeatureStart();
            jsonWriter.WriteGeometryStart();
            jsonWriter.WriteLineString(new (double longitude, double latitude, float? e)[] { previousLocation, nodeLocation });
            jsonWriter.WriteProperties(new (string key, string value)[]
            {
                ("type", "track"), ("length", length.ToString(CultureInfo.InvariantCulture))
            });
            jsonWriter.WriteFeatureEnd();
        }
    }
}
