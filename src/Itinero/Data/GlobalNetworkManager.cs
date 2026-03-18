using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Data;

/// <summary>
/// A manager to manage mapping or vertex and edge ids and turn restrictions.
/// </summary>
public class GlobalNetworkManager
{
    /// <summary>
    /// Creates a new global network manager.
    /// </summary>
    public GlobalNetworkManager()
    {
        this.VertexIdSet = new GlobalVertexIdSet();
        this.EdgeIdSet = new GlobalEdgeIdSet();
    }

    /// <summary>
    /// The global vertex id set.
    /// </summary>
    public GlobalVertexIdSet VertexIdSet { get; }

    /// <summary>
    /// The global edge id set.
    /// </summary>
    public GlobalEdgeIdSet EdgeIdSet { get; }

    /// <summary>
    /// Pending boundary crossings keyed by GlobalEdgeId.
    /// When a crossing's matching tile isn't loaded yet, store the local vertex and metadata here.
    /// When the other tile loads and has a crossing with the same GlobalEdgeId, it creates the edge.
    /// </summary>
    public Dictionary<GlobalEdgeId, (VertexId vertex, IEnumerable<(string key, string value)> attributes,
        uint edgeTypeId, bool isIncoming)> PendingBoundaryCrossings { get; } = new();

    /// <summary>
    /// Restrictions that couldn't be resolved yet because not all edges are available.
    /// </summary>
    public List<GlobalRestriction> PendingRestrictions { get; } = new();
}
