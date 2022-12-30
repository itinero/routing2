using System;
using System.Collections.Generic;
using System.Globalization;
using NetTopologySuite.Features;

namespace Itinero.Geo;

/// <summary>
/// Extension methods related to attributes tables
/// </summary>
public static class AttributesTableExtensions
{
    /// <summary>
    /// Converts the given attributes table to attributes for Itinero.
    /// </summary>
    /// <param name="attributesTable">The attributes table.</param>
    /// <returns>Attributes as string key-value pairs.</returns>
    public static IEnumerable<(string key, string value)> ToAttributes(this IAttributesTable attributesTable)
    {
        var names = attributesTable.GetNames();
        foreach (var name in names)
        {
            yield return (name, attributesTable[name].ToInvariantString());
        }
    }

    /// <summary>
    /// Returns a string representing the object in a culture invariant way.
    /// </summary>
    private static string ToInvariantString(this object obj)
    {
        return obj switch
        {
            IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => obj.ToString()
        };
    }
}
