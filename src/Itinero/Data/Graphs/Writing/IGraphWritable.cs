using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs.Writing
{
    /// <summary>
    /// Abstract representation of the functionality need to write to a graph.
    /// </summary>
    internal interface IGraphWritable
    {
        int Zoom { get; }

        bool TryGetVertex(VertexId vertexId, out double longitude, out double latitude);
        
        GraphTurnCostTypeIndex GraphTurnCostTypeIndex { get; }
        
        GraphTile GetTileForWrite(uint localTileId);
        
        void ClearWriter();
    }
}