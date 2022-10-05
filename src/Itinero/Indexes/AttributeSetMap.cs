using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Indexes;

/// <summary>
/// Represents a mapping from attributes sets to sets that only keep useful attributes.
/// </summary>
public class AttributeSetMap
{
    /// <summary>
    /// Creates a new map.
    /// </summary>
    /// <param name="id">The id.</param>
    protected AttributeSetMap(Guid? id = null)
    {
        this.Id = id ?? Guid.Empty;
    }

    /// <summary>
    /// Gets the id.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Maps the attribute set to a subset of the attributes keeping only the useful attributes.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>A subset of attributes.</returns>
    public virtual IEnumerable<(string key, string value)> Map(IEnumerable<(string key, string value)> attributes)
    {
        return Enumerable.Empty<(string key, string value)>();
    }

    /// <summary>
    /// A default empty map.
    /// </summary>
    public static readonly AttributeSetMap Default = new();
}
