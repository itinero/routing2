namespace Itinero.Network.Tiles.Standalone;

public struct BoundaryEdgeId
{
    public BoundaryEdgeId(uint localId)
    {
        this.LocalId = localId;
    }
    
    public uint LocalId { get; }
}