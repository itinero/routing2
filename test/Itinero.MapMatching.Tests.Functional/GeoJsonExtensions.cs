using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Routes;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.MapMatching.Tests.Functional;

internal static class GeoJsonExtensions
{
    public static string ToGeoJson(this Geometry geometry)
    {
        var features = new FeatureCollection { new Feature(geometry, new AttributesTable()) };
        return features.ToGeoJson();
    }

    public static string ToGeoJson(this FeatureCollection featureCollection)
    {
        var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
        var jsonStream = new StringWriter();
        jsonSerializer.Serialize(jsonStream, featureCollection);
        var json = jsonStream.ToString();
        return json;
    }

    public static MultiLineString ToMultiLineString(this IEnumerable<Route> route)
    {
        return new MultiLineString(route.Select(Itinero.Geo.RouteExtensions.ToLineString).ToArray());
    }

    public static FeatureCollection ToFeatures(this Track track)
    {
        var features = new FeatureCollection();

        var coordinates = new List<Coordinate>();
        foreach (var point in track)
        {
            coordinates.Add(new Coordinate(point.Location.longitude, point.Location.latitude));
        }

        var lineStrings = new LineString(coordinates.ToArray());
        features.Add(new Feature(lineStrings, new AttributesTable()));

        return features;
    }
}
