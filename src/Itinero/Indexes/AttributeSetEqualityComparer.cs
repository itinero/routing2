using System.Collections.Generic;

namespace Itinero.Indexes;

/// <summary>
/// An equality comparer to compare attribute sets.
/// </summary>
/// <remarks>
/// - Doesn't care about the order of attributes.
/// </remarks>
public class AttributeSetEqualityComparer : IEqualityComparer<IReadOnlyList<(string key, string value)>>
{
    /// <summary>
    /// The default comparer.
    /// </summary>
    public static readonly AttributeSetEqualityComparer Default = new();

    /// <inheritdoc/>
    public bool Equals(IReadOnlyList<(string key, string value)> x,
        IReadOnlyList<(string key, string value)> y)
    {
        if (x.Count != y.Count)
        {
            return false;
        }

        for (var i = 0; i < x.Count; i++)
        {
            var xPair = x[i];
            var yPair = y[i];

            if (xPair != yPair)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public int GetHashCode(IReadOnlyList<(string key, string value)> obj)
    {
        var hash = obj.Count.GetHashCode();

        foreach (var pair in obj)
        {
            hash ^= pair.GetHashCode();
        }

        return hash;
    }
}
