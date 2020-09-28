using Itinero.Network.DataStructures;
using Itinero.Network.Indexes.EdgeTypes;
using Itinero.Network.Indexes.TurnCosts;
using Itinero.Network.Tiles;

namespace Itinero.Network.Mutation
{
    internal interface IRoutingNetworkMutable
    {
        int Zoom { get; }
        
        RouterDb RouterDb { get; }
        
        SparseArray<(NetworkTile? tile, int edgeTypesId)> Tiles {get;}
        
        TurnCostTypeIndex GraphTurnCostTypeIndex { get; }
        
        EdgeTypeIndex GraphEdgeTypeIndex { get; }
        
        void ClearMutator();
    }
}