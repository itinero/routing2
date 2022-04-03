using System;
using System.Collections.Generic;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    private readonly ArrayBase<byte> _crossings = new MemoryArray<byte>(1024);
    private uint _crossingsPointer = 0;

    internal void AddBoundaryCrossing(bool isToTile, long globalIdFrom, long globalIdTo, VertexId vertex,
        IEnumerable<(string key, string value)> attributes, uint edgeTypeId,
        uint length)
    {
        if (vertex.TileId != _networkTile.TileId)
            throw new ArgumentException("Can only add boundary crossings that cross into the tile");
        
        _crossings.EnsureMinimumSize(_crossingsPointer + 36);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, isToTile ? (uint)1 : 0);
        _crossingsPointer += (uint)_crossings.SetDynamicInt64(_crossingsPointer, globalIdFrom);
        _crossingsPointer += (uint)_crossings.SetDynamicInt64(_crossingsPointer, globalIdTo);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, vertex.LocalId);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, edgeTypeId);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, length);
        
        var a = this.SetAttributes(attributes);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, a);
    }

    internal IEnumerable<(bool isToTile, long globalIdFrom, long globalIdTo, VertexId vertex, uint edgeTypeId, uint length, 
        IEnumerable<(string key, string value)> attributes)> GetBoundaryCrossings()
    {
        var pointer = 0L;
        while (pointer < _crossingsPointer) {
            pointer += _crossings.GetDynamicUInt32(pointer, out var direction);
            pointer += _crossings.GetDynamicInt64(pointer, out var globalIdFrom);
            pointer += _crossings.GetDynamicInt64(pointer, out var globalIdTo);
            pointer += _crossings.GetDynamicUInt32(pointer, out var vertexLocalId);
            pointer += _crossings.GetDynamicUInt32(pointer, out var edgeTypeId);
            pointer += _crossings.GetDynamicUInt32(pointer, out var length);
            pointer += _crossings.GetDynamicUInt32(pointer, out var a);

            yield return (direction != 0, globalIdFrom, globalIdTo,
                new VertexId(_networkTile.TileId, vertexLocalId), edgeTypeId, length, this.GetAttributes(a));
        }
    }
}