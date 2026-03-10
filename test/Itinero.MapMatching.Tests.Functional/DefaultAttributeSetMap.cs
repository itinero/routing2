using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;

namespace Itinero.MapMatching.Tests.Functional;

public class DefaultAttributeSetMap : AttributeSetMap
{
    /// <summary>
    /// Maps the attribute set to a subset of the attributes keeping only the useful attributes.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>A subset of attributes.</returns>
    public override IEnumerable<(string key, string value)> Map(IEnumerable<(string key, string value)> attributes)
    {
        return attributes;
    }
}
