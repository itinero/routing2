using System;
using System.Collections.Generic;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs.Tiles
{
    internal partial class GraphTile
    {
        private readonly ArrayBase<byte> _turnCostPointers = new MemoryArray<byte>(0);
        private readonly ArrayBase<byte> _turnCosts = new MemoryArray<byte>(0);
        
        /// <summary>
        /// Sets a turn cost type on a given vertex.
        /// </summary>
        /// <param name="vertexId">The vertex id.</param>
        /// <param name="turnCostTypeId">The turn cost type id.</param>
        /// <param name="turnCostTableId">The turn cost table id.</param>
        internal void SetTurnCost(VertexId vertexId, uint turnCostTypeId, uint turnCostTableId)
        {
            if (vertexId.TileId != _tileId) throw new ArgumentException(
                $"Vertex is not part of this tile.", nameof(vertexId));
            
            // // check if the index is there.
            // _turnCosts ??= new List<TurnCostIndex>(1);
            //
            // // find or build index.
            // var i = _turnCosts.FindIndex(x => x.Id == turnCostTypeId);
            // if (i < 0)
            // {
            //     _turnCosts.Add(new TurnCostIndex(turnCostTypeId));
            //     i = 0;
            // }
            // var index = _turnCosts[i];
            //
            // // set turn cost table id.
            // index[vertexId.LocalId] = turnCostTableId;
        }

        /// <summary>
        /// Gets turn costs for the given vertex.
        /// </summary>
        /// <param name="vertexId">The vertex id.</param>
        /// <returns>All turn costs associated with the given vertex.</returns>
        internal IEnumerable<(uint type, uint table)> GetTurnCosts(VertexId vertexId)
        {
            if (vertexId.TileId != _tileId) throw new ArgumentException(
                $"Vertex is not part of this tile.", nameof(vertexId));
            
            yield break;
            // // check if there are turn costs.
            // if (_turnCosts == null || _turnCosts.Count == 0) yield break;
            //
            // // return the turning costs.
            // foreach (var turnCostIndex in _turnCosts)
            // {
            //     var turnCostTableId = turnCostIndex[vertexId.LocalId];
            //     if (turnCostTableId == uint.MaxValue) continue;
            //
            //     yield return (turnCostIndex.Id, turnCostTableId);
            // }
        }
    }
}