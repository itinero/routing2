using System;
using System.Collections.Generic;

namespace Itinero.IO.Osm.Tiles;

internal static class AttributesExtensions
{
    public static IEnumerable<(string key, string value)> Concat(
        this IEnumerable<(string key, string value)> attributes, Guid globalEdgeId)
    {
        foreach (var attribute in attributes)
        {
            yield return attribute;
        }

        yield return ("_segment_guid", globalEdgeId.ToString());
    }
}
