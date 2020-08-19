using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Data.Graphs.TurnCosts;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs.Tiles
{
    internal partial class GraphTile
    {
        private uint _turnCostPointer = 0;
        private readonly ArrayBase<uint> _turnCostPointers = new MemoryArray<uint>(0);
        private readonly ArrayBase<byte> _turnCosts = new MemoryArray<byte>(0);

        internal void AddTurnCosts(VertexId vertex, uint turnCostType,
            EdgeId[] edges, uint[,] costs)
        {
            if (edges.Length > OrderCoder.MAX_ORDER_HEAD_TAIL) throw new ArgumentException(
                $"Cannot add turn costs for vertices with more than {OrderCoder.MAX_ORDER_HEAD_TAIL} edges.");
            
            // enumerate the edges associated with the vertex.
            var enumerator = new GraphTileEnumerator();
            enumerator.MoveTo(this);
            if (!enumerator.MoveTo(vertex)) throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.", 
                nameof(vertex));

            // get existing head/tail orders.
            var orders = new byte?[edges.Length];
            while (enumerator.MoveNext())
            {
                // get the given order, if any, skip edge otherwise.
                var givenOrder = Array.IndexOf(edges, enumerator.EdgeId);
                if (givenOrder < 0) continue;
                
                // get the existing order, if any.
                byte? order = null;
                order = enumerator.Forward ? enumerator.Tail : enumerator.Head;
                
                // set order.
                orders[givenOrder] = order;
            }
            
            // assign missing orders if any.
            var min = -1;
            var max = -1;
            for (var o = 0; o < orders.Length; o++)
            {
                var order = orders[o];
                if (order != null)
                {
                    if (max < order.Value) max = order.Value; 
                    continue;
                }

                // find the first n not used.
                var firstUnused = 0;
                for (var n = min + 1; n < orders.Length; n++)
                {
                    if (orders.Contains((byte)n)) continue;

                    firstUnused = n; 
                    min = firstUnused - 1;
                    break;
                }

                // set order.
                order = (byte) firstUnused;
                orders[o] = order;
                min++;
                
                if (max < order.Value) max = order.Value; 
            }
            
            // set head tail orders.
            enumerator.Reset();
            while (enumerator.MoveNext())
            {                
                // get the given order, if any, skip edge otherwise.
                var givenOrder = Array.IndexOf(edges, enumerator.EdgeId);
                if (givenOrder < 0) continue;
                
                // get order and check order.
                var order = orders[givenOrder];
                if (order == null) throw new InvalidDataException("Order should be set by now.");
                if (enumerator.Forward && enumerator.Tail == order) continue;
                if (!enumerator.Forward && enumerator.Head == order) continue;
                
                // set order, it's different.
                this.SetFromOrder(enumerator, order.Value);
            }


            var count = max + 1;
            
            // reversed order array.
            var reversedOrders = new byte?[count];
            for (var i = 0; i < orders.Length; i++)
            {
                var order = orders[i];
                if (order == null) continue;

                reversedOrders[order.Value] = (byte)i;
            }

            // make sure there is space in the pointers table.
            // and initialize new slots with null.
            while (_turnCostPointers.Length < vertex.LocalId)
            {
                _turnCostPointers.Resize(_turnCostPointers.Length + 1024);
            }
            
            // make sure there is space in the turn cost array.
            // and initialize new slots with null.
            var maxLength = _turnCostPointer + 5 + 1 + (count * count) * 5 + 5; 
            while (_turnCosts.Length < maxLength)
            {
                _turnCosts.Resize(_turnCosts.Length + 1024);
            }

            // update pointer to reflect new data.
            var previousPointer = _turnCostPointers[vertex.LocalId].DecodeNullableData();
            _turnCostPointers[vertex.LocalId] = _turnCostPointer.EncodeToNullableData();

            // write data and include previous pointer.
            _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, turnCostType);
            _turnCosts[_turnCostPointer] = (byte)count;
            _turnCostPointer++;
            for (var x = 0; x < count; x++)
            {
                var xOrder = reversedOrders[x];
                for (var y = 0; y < count; y++)
                {
                    var yOrder = reversedOrders[y];

                    // get cost from original matrix.
                    uint cost = 0;
                    if (xOrder != null && yOrder != null) cost = costs[xOrder.Value, yOrder.Value];
                    
                    // write cost.
                    _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, cost);
                }
            }

            _turnCostPointer +=
                (uint) _turnCosts.SetDynamicUInt32(_turnCostPointer, previousPointer.EncodeAsNullableData());
        }

        internal IEnumerable<(uint turnCostType, uint cost)> GetTurnCosts(VertexId vertex, byte fromOrder, byte toOrder)
        {
            if (_turnCostPointers.Length <= vertex.LocalId) yield break;
            
            var pointerNullable = _turnCostPointers[vertex.LocalId].DecodeNullableData();
            if (pointerNullable == null) yield break;
            
            var pointer = pointerNullable.Value;
            while (true)
            {
                pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var turnCostType);
                var max = _turnCosts[pointer];
                pointer++;

                for (var x = 0; x < max; x++)
                {
                    for (var y = 0; y < max; y++)
                    {
                        pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var cost);
                        if (fromOrder != x || toOrder != y) continue;
                        
                        if (cost != 0) yield return (turnCostType, cost);
                    }
                }
            }
        }

        private void SetFromOrder(GraphTileEnumerator enumerator, byte order)
        {
            var pointer = enumerator.EdgeId.LocalId;
            
            // skip over vertices and next-pointers.
            var size = this.DecodeVertex(pointer, out _, out var t1);
            pointer += size;
            size = this.DecodeVertex(pointer, out _, out var t2);
            pointer += size;
            size = this.DecodePointer(pointer, out _);
            pointer += size;
            size = this.DecodePointer(pointer, out _);
            pointer += size;
            
            // skip edge id if needed.
            if (t1 != t2)
            {
                size = this.DecodeEdgeId(pointer, out _);
                pointer += size;
            }
            
            // skip over length/type.
            size = this.DecodeEdgePointerId(pointer, out _);
            pointer += size;
            size = this.DecodeEdgePointerId(pointer, out _);
            pointer += size;
            
            // get existing head/tail order.
            byte? head = null;
            byte? tail = null;
            this.GetTailHeadOrder(pointer, ref tail, ref head);
            if (enumerator.Forward)
            {
                if (tail.HasValue) throw new InvalidOperationException("An edge tail or head order can only be set once.");
                // replace tail.
                tail = order;
            }
            else
            {
                if (head.HasValue) throw new InvalidOperationException("An edge tail or head order can only be set once.");
                // replace head.
                head = order;
            }
            _edges.SetTailHeadOrder(pointer, tail, head);
        }

    }
}