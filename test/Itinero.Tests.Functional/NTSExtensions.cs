using System.Collections.Generic;
using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;

namespace Itinero.Tests.Functional;

public static class NTSExtensions
{
    private static readonly JsonSerializerOptions GeoJsonOptions = new()
    {
        Converters = { new GeoJsonConverterFactory() },
        WriteIndented = true
    };

    public static void AddRange(this FeatureCollection featureCollection, IEnumerable<Feature> features)
    {
        foreach (var feature in features)
        {
            featureCollection.Add(feature);
        }
    }

    public static string ToGeoJson(this FeatureCollection featureCollection)
    {
        return JsonSerializer.Serialize(featureCollection, GeoJsonOptions);
    }
}
