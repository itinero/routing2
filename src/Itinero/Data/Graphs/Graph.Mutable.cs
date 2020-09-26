using System;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Mutation;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs
{
    public sealed partial class Graph : IGraphMutable
    {
        private readonly object _mutatorSync = new object();
        private GraphMutator? _graphMutator;
        
        internal GraphMutator GetAsMutable()
        {
            lock (_mutatorSync)
            {
                if (_graphMutator != null)
                    throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");

                _graphMutator = new GraphMutator(this);
                return _graphMutator;
            }
        }

        SparseArray<(GraphTile? tile, int edgeTypesId)> IGraphMutable.Tiles => _tiles;

        GraphTurnCostTypeIndex IGraphMutable.GraphTurnCostTypeIndex => _graphTurnCostTypeIndex;

        GraphEdgeTypeIndex IGraphMutable.GraphEdgeTypeIndex => _graphEdgeTypeIndex;

        void IGraphMutable.ClearMutator()
        {
            _graphMutator = null;
        }
    }
}