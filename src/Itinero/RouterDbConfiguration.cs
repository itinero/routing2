using System.Collections.Generic;
using Itinero.Indexes;

namespace Itinero;

/// <summary>
/// Defines router db settings.
/// </summary>
public class RouterDbConfiguration
{
    /// <summary>
    /// Gets or sets the zoom level.
    /// </summary>
    public int Zoom { get; set; } = 14;

    /// <summary>
    /// Gets or sets the initial edge profiles.
    /// </summary>
    public List<IReadOnlyList<(string key, string value)>>? EdgeTypes { get; set; }
    
    /// <summary>
    /// Gets or sets the edge type map.
    /// </summary>
    public AttributeSetMap? EdgeTypeMap { get; set; }

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static readonly RouterDbConfiguration Default = new() { Zoom = 14 };
}