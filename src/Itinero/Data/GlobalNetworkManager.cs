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
}