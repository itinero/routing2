using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Data;
using Itinero.IO;

namespace Itinero.Indexes;

/// <summary>
/// Maintains an index of distinct sets of attributes.
/// </summary>
/// <remarks>
/// A collection of distinct sets of attributes that:.
/// - Cannot mutate data in existing ids.
/// - Can only grow
/// - 0 represents an empty non-null set.
/// </remarks>
public abstract class AttributeSetIndex
{
    /// <summary>
    /// Gets the number of distinct sets.
    /// </summary>
    public abstract uint Count { get; }

    /// <summary>
    /// Gets the attributes for the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The attributes in the type.</returns>
    public abstract IEnumerable<(string key, string value)> GetById(uint id);

    /// <summary>
    /// Gets the type id for the given attributes set.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>The id, if any.</returns>
    public abstract uint Get(IEnumerable<(string key, string value)> attributes);

    /// <summary>
    /// Writes this index to the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public abstract Task WriteTo(Stream stream);

    /// <summary>
    /// Reads the index from the given stream and replaces the data in the current index.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns></returns>
    public abstract Task ReadFrom(Stream stream);
}
