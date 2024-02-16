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
    /// Gets or sets the maximum size of islands.
    /// </summary>
    public int MaxIslandSize { get; set; } = 256;

    /// <summary>
    /// Gets or sets the initial edge types.
    /// </summary>
    public AttributeSetIndex EdgeTypeIndex { get; set; } = new AttributeSetDictionaryIndex();

    /// <summary>
    /// Gets or sets the edge type map.
    /// </summary>
    public AttributeSetMap? EdgeTypeMap { get; set; }

    /// <summary>
    /// Gets or sets the initial turn cost types.
    /// </summary>
    public AttributeSetIndex TurnCostTypeIndex { get; set; } = new AttributeSetDictionaryIndex();

    /// <summary>
    /// Gets or sets the turn cost type map.
    /// </summary>
    public AttributeSetMap? TurnCostTypeMap { get; set; }

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static RouterDbConfiguration Default() { return new() { Zoom = 14, MaxIslandSize = 1024 }; }
}
