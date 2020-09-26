using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs.Mutation
{
    internal interface IGraphMutable
    {
        int Zoom { get; }
        
        RouterDb RouterDb { get; }
        
        SparseArray<(GraphTile? tile, int edgeTypesId)> Tiles {get;}
        
        GraphTurnCostTypeIndex GraphTurnCostTypeIndex { get; }
        
        GraphEdgeTypeIndex GraphEdgeTypeIndex { get; }
        
        void ClearMutator();
    }
}