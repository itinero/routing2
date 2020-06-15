using System;
using System.Collections.Generic;
using Itinero.Data.Graphs.TurnCosts;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs.Tiles
{
    internal partial class GraphTile
    {
        private uint _nextTurnCostPointer = 0;
        private readonly ArrayBase<byte> _turnCosts;
        
        /// <summary>
        /// Adds a new turn cost table.
        /// </summary>
        /// <param name="turnCostTable">The turn cost table.</param>
        /// <returns>The new local turn cost id.</returns>
        public TurnCostId AddTurnCosts(IReadOnlyList<uint> turnCostTable)
        {
            var newId = _nextTurnCostPointer;
            
            _nextAttributePointer += (uint) 
                _turnCosts.SetDynamicUInt32(_nextAttributePointer, (uint)turnCostTable.Count);
            foreach (var t1 in turnCostTable)
            {
                _nextAttributePointer += (uint) 
                    _turnCosts.SetDynamicUInt32Nullable(_nextAttributePointer, t1);
            }

            return new TurnCostId(_tileId, newId);
        }
        
        internal GraphTile SetTurnCosts(VertexId vertexId, EdgeId[] edgeOrder, TurnCostId turnCostId)
        {        
            var edges = new MemoryArray<byte>(_edges.Length);
            var pointers = new MemoryArray<uint>(_pointers.Length);
            var nextEdgeId = _nextEdgeId;
            var p = 0U;
            var newP = 0U;
            while (p < nextEdgeId)
            {
                // read edge data.
                p += DecodeVertex(p, out var local1Id, out var tile1Id);
                var vertex1 = new VertexId(tile1Id, local1Id);
                p += DecodeVertex(p, out var local2Id, out var tile2Id);
                var vertex2 = new VertexId(tile2Id, local2Id);
                p += DecodePointer(p, out _);
                p += DecodePointer(p, out _);
                EdgeId? edgeId = null;
                if (tile1Id != tile2Id)
                {
                    p += DecodeEdgeId(p, out edgeId);
                }
                p += (uint)_edges.GetDynamicInt32Nullable(p, out var edgeTypeId);
                p += (uint)_edges.GetDynamicInt32Nullable(p, out var length);
                p += (uint)_edges.GetDynamicInt32Nullable(p, out var tailOrder);
                p += (uint)_edges.GetDynamicInt32Nullable(p, out var headOrder);
                p += DecodePointer(p, out var shapePointer);
                p += DecodePointer(p, out var attributePointer);
                
                // update head or tail order if needed.
                if (vertex1 == vertexId)
                {
                    tailOrder = null;
                    var oldEdgeId = edgeId ?? new EdgeId(_tileId, _nextEdgeId);
                    var index = Array.FindIndex(edgeOrder, e => oldEdgeId == e);
                    if (index >= 0) tailOrder = (uint) index;
                }
                else if (vertex2 == vertexId)
                {
                    headOrder = null;
                    var oldEdgeId = edgeId ?? new EdgeId(_tileId, _nextEdgeId);
                    var index = Array.FindIndex(edgeOrder, e => oldEdgeId == e);
                    if (index >= 0) headOrder = (uint) index;
                }
                
                // write edge data again.
                var newEdgePointer = newP;
                newP += EncodeVertex(edges, _tileId, newP, vertex1);
                newP += EncodeVertex(edges, _tileId, newP, vertex2);
                uint? v1p = null;
                if (vertex1.TileId == _tileId)
                {
                    v1p = pointers[vertex1.LocalId].DecodeNullableData();
                    pointers[vertex1.LocalId] = newEdgePointer.EncodeToNullableData();
                }
                uint? v2p = null;
                if (vertex2.TileId == _tileId)
                {
                    v2p = pointers[vertex2.LocalId].DecodeNullableData();
                    pointers[vertex2.LocalId] = newEdgePointer.EncodeToNullableData();
                }

                newP += EncodePointer(edges, newP, v1p);
                newP += EncodePointer(edges, newP, v2p);
                if (edgeId != null)
                {
                    newP += EncodeEdgeId(edges, _tileId, newP, edgeId.Value);
                }
                newP += (uint)edges.SetDynamicUInt32Nullable(newP, edgeTypeId);
                newP += (uint)edges.SetDynamicUInt32Nullable(newP, length);
                newP += (uint)edges.SetDynamicUInt32Nullable(newP, tailOrder);
                newP += (uint)edges.SetDynamicUInt32Nullable(newP, headOrder);
                newP += EncodePointer(edges, newP, shapePointer);
                newP += EncodePointer(edges, newP, attributePointer);
            }
            
            return new GraphTile(_zoom, _tileId, pointers, edges, _coordinates,
                _shapes, _attributes, _strings, _turnCosts, _nextVertexId, _nextEdgeId, _nextAttributePointer, _nextShapePointer, _nextStringId);
        }
    }
}