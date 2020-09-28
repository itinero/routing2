using Itinero.Network.Indexes.TurnCosts;
using Itinero.Network.Tiles;

namespace Itinero.Network.Writer
{
    internal interface IRoutingNetworkWritable
    {
        int Zoom { get; }

        bool TryGetVertex(VertexId vertexId, out double longitude, out double latitude);
        
        TurnCostTypeIndex GraphTurnCostTypeIndex { get; }
        
        NetworkTile GetTileForWrite(uint localTileId);
        
        void ClearWriter();
    }
}