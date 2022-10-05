using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;
using Itinero.Network.Storage;
using Itinero.Network.TurnCosts;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    private uint _turnCostPointer = 0;
    private readonly ArrayBase<uint> _turnCostPointers = new MemoryArray<uint>(0);
    private readonly ArrayBase<byte> _turnCosts = new MemoryArray<byte>(0);

    internal void AddTurnCosts(VertexId vertex, uint turnCostType,
        EdgeId[] edges, uint[,] costs, IEnumerable<(string key, string value)> attributes,
        IEnumerable<EdgeId>? prefix = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        if (edges.Length > OrderCoder.MAX_ORDER_HEAD_TAIL)
        {
            throw new ArgumentException(
                $"Cannot add turn costs for vertices with more than {OrderCoder.MAX_ORDER_HEAD_TAIL} edges.");
        }

        // enumerate the edges associated with the vertex.
        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(this);
        if (!enumerator.MoveTo(vertex))
        {
            throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.",
                nameof(vertex));
        }

        // get existing head/tail orders.
        var orders = new byte?[edges.Length];
        while (enumerator.MoveNext())
        {
            // get the given order, if any, skip edge otherwise.
            var givenOrder = Array.IndexOf(edges, enumerator.EdgeId);
            if (givenOrder < 0)
            {
                continue;
            }

            // get the existing order at the restricted vertex, if any.
            byte? order = null;
            order = enumerator.TailOrder;

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
                if (max < order.Value)
                {
                    max = order.Value;
                }

                continue;
            }

            // find the first n not used.
            var firstUnused = 0;
            for (var n = min + 1; n < orders.Length; n++)
            {
                if (orders.Contains((byte)n))
                {
                    continue;
                }

                firstUnused = n;
                min = firstUnused - 1;
                break;
            }

            // set order.
            order = (byte)firstUnused;
            orders[o] = order;
            min++;

            if (max < order.Value)
            {
                max = order.Value;
            }
        }

        // set orders on adjacent edges.
        enumerator.Reset();
        while (enumerator.MoveNext())
        {
            // get the given order, if any, skip edge otherwise.
            var givenOrder = Array.IndexOf(edges, enumerator.EdgeId);
            if (givenOrder < 0)
            {
                continue;
            }

            // get order and check order.
            var order = orders[givenOrder];
            if (order == null)
            {
                throw new InvalidDataException("Order should be set by now.");
            }

            // check if an update is needed.
            if (enumerator.TailOrder == order) continue;

            // set order, it's different.
            if (enumerator.Forward)
            {
                // if the edge is forward the tail in the enumerator is also the tail in the edge.
                SetTailHeadOrder(enumerator.EdgePointer, order.Value, null);
            }
            else
            {
                // if the edge is backward the tail in the enumerator is head in the edge.
                SetTailHeadOrder(enumerator.EdgePointer, null, order.Value);
            }
        }

        var count = max + 1;

        // reversed order array.
        var reversedOrders = new byte?[count];
        for (var i = 0; i < orders.Length; i++)
        {
            var order = orders[i];
            if (order == null)
            {
                continue;
            }

            reversedOrders[order.Value] = (byte)i;
        }

        // make sure there is space in the pointers table.
        // and initialize new slots with null.
        while (_turnCostPointers.Length <= vertex.LocalId)
        {
            _turnCostPointers.Resize(_nextVertexId);
        }

        // make sure there is space in the turn cost array.
        // and initialize new slots with null.
        var maxLength = _turnCostPointer + 5 + 1 + count * count * 5 + 5;
        while (_turnCosts.Length <= maxLength)
        {
            _turnCosts.Resize(_turnCosts.Length + DefaultSizeIncrease);
        }

        // update pointer to reflect new data.
        var previousPointer = _turnCostPointers[vertex.LocalId].DecodeNullableData();
        _turnCostPointers[vertex.LocalId] = _turnCostPointer.EncodeToNullableData();

        // write turn cost types.
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, turnCostType);

        // write attributes.
        var a = this.SetAttributes(attributes);
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, a);

        // write prefix sequence.
        var prefixEdges = new List<EdgeId>(prefix);
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, (uint)prefixEdges.Count);
        foreach (var prefixEdge in prefixEdges)
        {
            if (prefixEdge.TileId == _tileId)
            {
                _turnCostPointer += (uint)_turnCosts.SetDynamicInt32(_turnCostPointer, (int)prefixEdge.LocalId);
            }
            else
            {
                _turnCostPointer += (uint)_turnCosts.SetDynamicInt32(_turnCostPointer, (int)-(prefixEdge.LocalId + 1));
                _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, prefixEdge.TileId);
            }
        }

        // write turn costs.
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
                if (xOrder != null && yOrder != null)
                {
                    cost = costs[xOrder.Value, yOrder.Value];
                }

                // write cost.
                _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, cost);
            }
        }

        // write previous turn cost pointer at the end.
        _turnCostPointer +=
            (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, previousPointer.EncodeAsNullableData());
    }

    internal IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost,
            IEnumerable<EdgeId> prefixEdges)>
        GetTurnCosts(VertexId vertex, byte fromOrder, byte toOrder)
    {
        if (_turnCostPointers.Length <= vertex.LocalId)
        {
            yield break;
        }

        var pointerNullable = _turnCostPointers[vertex.LocalId].DecodeNullableData();
        if (pointerNullable == null)
        {
            yield break;
        }

        var pointer = pointerNullable.Value;
        while (true)
        {
            // return turn cost type.
            pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var turnCostType);

            // read attributes.
            pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var a);
            var attributes = this.GetAttributes(a);

            // read prefix edges.
            IEnumerable<EdgeId> prefixEdges = ArraySegment<EdgeId>.Empty;
            pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var prefixEdgeCount);
            if (prefixEdgeCount > 0)
            {
                var prefixEdgesList = new List<EdgeId>();
                while (prefixEdgeCount > 0)
                {
                    pointer += (uint)_turnCosts.GetDynamicInt32(pointer, out var signedLocalId);
                    if (signedLocalId >= 0)
                    {
                        prefixEdgesList.Add(new EdgeId(this.TileId, (uint)signedLocalId));
                    }
                    else
                    {
                        pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var tileId);
                        prefixEdgesList.Add(new EdgeId(tileId, (uint)((-signedLocalId) - 1)));
                    }

                    prefixEdgeCount--;
                }

                prefixEdges = prefixEdgesList;
            }

            // read turn cost table.
            var max = _turnCosts[pointer];
            pointer++;

            for (var x = 0; x < max; x++)
            {
                for (var y = 0; y < max; y++)
                {
                    pointer += (uint)_turnCosts.GetDynamicUInt32(pointer, out var cost);
                    if (fromOrder != x || toOrder != y)
                    {
                        continue;
                    }

                    if (cost != 0)
                    {
                        yield return (turnCostType, attributes, cost, prefixEdges);
                    }
                }
            }

            // get pointer to next turn cost table.
            _turnCosts.GetDynamicUInt32(pointer, out var p);
            pointerNullable = p.DecodeNullableData();
            if (pointerNullable == null)
            {
                break;
            }

            pointer = pointerNullable.Value;
        }
    }

    private void SetTailHeadOrder(uint pointer, byte? tailOrder, byte? headOrder)
    {
        // skip over vertices and next-pointers.
        var size = DecodeVertex(pointer, out _, out var t1);
        pointer += size;
        size = DecodeVertex(pointer, out _, out var t2);
        pointer += size;
        size = DecodePointer(pointer, out _);
        pointer += size;
        size = DecodePointer(pointer, out _);
        pointer += size;

        // skip edge id if needed.
        if (t1 != t2)
        {
            size = (uint)_edges.GetDynamicUInt32(pointer, out _);
            pointer += size;
        }

        // skip over length/type.
        size = DecodeEdgePointerId(pointer, out _);
        pointer += size;
        size = DecodeEdgePointerId(pointer, out _);
        pointer += size;

        // get existing head/tail order.
        byte? existingTailOrder = null;
        byte? existingHeadOrder = null;
        GetTailHeadOrder(pointer, ref existingTailOrder, ref existingHeadOrder);

        // set tail order if there is a value.
        if (tailOrder.HasValue)
        {
            if (existingTailOrder.HasValue)
                throw new InvalidOperationException("An edge tail or head order can only be set once.");
            existingTailOrder = tailOrder;
        }

        // set head order if there is a value.
        if (headOrder.HasValue)
        {
            if (existingHeadOrder.HasValue)
                throw new InvalidOperationException("An edge tail or head order can only be set once.");
            existingHeadOrder = headOrder;
        }

        _edges.SetTailHeadOrder(pointer, existingTailOrder, existingHeadOrder);
    }

    private void WriteTurnCostsTo(Stream stream)
    {
        stream.WriteVarUInt32((uint)_turnCostPointers.Length);
        for (var i = 0; i < _turnCostPointers.Length; i++)
        {
            stream.WriteVarUInt32(_turnCostPointers[i]);
        }

        stream.WriteVarUInt32(_turnCostPointer);
        for (var i = 0; i < _turnCostPointer; i++)
        {
            stream.WriteByte(_turnCosts[i]);
        }
    }

    private void ReadTurnCostsFrom(Stream stream)
    {
        var turnCostPointersSize = stream.ReadVarUInt32();
        _turnCostPointers.Resize(turnCostPointersSize);
        for (var i = 0; i < turnCostPointersSize; i++)
        {
            _turnCostPointers[i] = stream.ReadVarUInt32();
        }

        _turnCostPointer = stream.ReadVarUInt32();
        _turnCosts.Resize(_turnCostPointer);
        for (var i = 0; i < _turnCostPointer; i++)
        {
            _turnCosts[i] = (byte)stream.ReadByte();
        }
    }
}
