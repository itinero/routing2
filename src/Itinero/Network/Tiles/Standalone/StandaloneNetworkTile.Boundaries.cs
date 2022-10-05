using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Itinero.Data;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    private readonly ArrayBase<byte> _crossings = new MemoryArray<byte>(1024);
    private uint _crossingsPointer = 0;

    internal BoundaryEdgeId AddBoundaryCrossing(bool isToTile, long globalIdFrom, long globalIdTo, VertexId vertex,
        IEnumerable<(string key, string value)> attributes, uint edgeTypeId, uint length)
    {
        if (vertex.TileId != this.NetworkTile.TileId)
            throw new ArgumentException("Can only add boundary crossings that cross into the tile");

        var id = new BoundaryEdgeId(_crossingsPointer);
        _crossings.EnsureMinimumSize(_crossingsPointer + 36);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, isToTile ? (uint)1 : 0);
        _crossingsPointer += (uint)_crossings.SetDynamicInt64(_crossingsPointer, globalIdFrom);
        _crossingsPointer += (uint)_crossings.SetDynamicInt64(_crossingsPointer, globalIdTo);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, vertex.LocalId);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, edgeTypeId);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, length);

        var a = this.SetAttributes(attributes);
        _crossingsPointer += (uint)_crossings.SetDynamicUInt32(_crossingsPointer, a);

        return id;
    }

    /// <summary>
    /// Gets all boundary crossing edges.
    /// </summary>
    /// <returns>An enumerable with all boundary crossing edges.</returns>
    public IEnumerable<(BoundaryEdgeId id, bool isToTile, long globalIdFrom, long globalIdTo, VertexId vertex, uint edgeTypeId, uint
        length,
        IEnumerable<(string key, string value)> attributes)> GetBoundaryCrossings()
    {
        var pointer = 0L;
        while (pointer < _crossingsPointer)
        {
            var id = new BoundaryEdgeId((uint)pointer);
            pointer += _crossings.GetDynamicUInt32(pointer, out var direction);
            pointer += _crossings.GetDynamicInt64(pointer, out var globalIdFrom);
            pointer += _crossings.GetDynamicInt64(pointer, out var globalIdTo);
            pointer += _crossings.GetDynamicUInt32(pointer, out var vertexLocalId);
            pointer += _crossings.GetDynamicUInt32(pointer, out var edgeTypeId);
            pointer += _crossings.GetDynamicUInt32(pointer, out var length);
            pointer += _crossings.GetDynamicUInt32(pointer, out var a);

            yield return (id, direction != 0, globalIdFrom, globalIdTo,
                new VertexId(this.NetworkTile.TileId, vertexLocalId), edgeTypeId, length, this.GetAttributes(a));
        }
    }
}
