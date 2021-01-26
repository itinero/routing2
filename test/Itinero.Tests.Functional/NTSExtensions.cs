using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.IO;

namespace Itinero.Tests.Functional {
    public static class NTSExtensions {
        public static void AddRange(this FeatureCollection featureCollection, IEnumerable<Feature> features) {
            foreach (var feature in features) {
                featureCollection.Add(feature);
            }
        }

        public static string ToGeoJson(this FeatureCollection featureCollection) {
            return new GeoJsonWriter().Write(featureCollection);
        }
    }
}