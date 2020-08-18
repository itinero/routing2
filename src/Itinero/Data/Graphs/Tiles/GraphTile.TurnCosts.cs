using System;
using System.Collections.Generic;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs.Tiles
{
    internal partial class GraphTile
    {
        private readonly ArrayBase<uint> _turnCostPointers = new MemoryArray<uint>(0);
        private readonly ArrayBase<byte> _turnCosts = new MemoryArray<byte>(0);

        private void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            // enumerate the edges associated with the vertex.
            var enumerator = new GraphTileEnumerator();
            enumerator.MoveTo(this);
            enumerator.MoveTo(vertex);

            while (enumerator.MoveNext())
            {
                
            }
        }
    }
}