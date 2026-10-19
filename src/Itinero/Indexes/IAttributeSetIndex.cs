using System.Collections.Generic;

namespace Itinero.Indexes;

/// <summary>
/// A read-only view of a set of distinct attribute sets, addressed by id.
/// </summary>
/// <remarks>
/// Read-only on purpose: ids are minted by whatever builds the data, never by Itinero. An id this
/// index does not know is a fault to report, not a set to intern.
/// </remarks>
public interface IAttributeSetIndex
{
    /// <summary>
    /// Gets the number of distinct sets.
    /// </summary>
    uint Count { get; }

    /// <summary>
    /// Gets the attributes for the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The attributes in the set.</returns>
    IEnumerable<(string key, string value)> GetById(uint id);
}
