using System;
using System.IO;
using Itinero.Indexes;

namespace Itinero;

/// <summary>
/// Contains settings to deserialize router dbs.
/// </summary>
public class RouterDbReadSettings
{
    /// <summary>
    /// The attribute set index for edge type attributes.
    /// </summary>
    public AttributeSetIndex EdgeTypeAttributeSetIndex { get; set; } = new AttributeSetDictionaryIndex();

    /// <summary>
    /// The attribute set index for turn cost attributes.
    /// </summary>
    public AttributeSetIndex TurnCostAttributeSetIndex { get; set; } = new AttributeSetDictionaryIndex();
}
