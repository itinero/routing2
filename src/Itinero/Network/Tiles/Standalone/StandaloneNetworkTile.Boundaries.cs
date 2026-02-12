using System;
using System.Collections.Generic;
using Itinero.Network.Storage;
using Itinero.Network.Tiles.Standalone.Global;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    private readonly ArrayBase<byte> _crossings = new MemoryArray<byte>(1024);
    private uint _crossingsPointer;

    internal void AddBoundaryCrossing(bool isIncoming, GlobalEdgeId globalEdgeId, VertexId vertex,
        IEnumerable<(string key, string value)> attributes, uint edgeTypeId)
    {
        if (vertex.TileId != this.NetworkTile.TileId)
            throw new ArgumentException("Can only add boundary crossings that cross into the tile");

        _crossings.EnsureMinimumSize(_crossingsPointer + 36);
        if (isIncoming)
        {
            // incoming if vertex is encoded as a positive number.
            _crossingsPointer += _crossings.SetDynamicInt32(_crossingsPointer, (int)(vertex.LocalId + 1));
        }
        else
        {
            // outgoing if vertex is encode as a negative number.
            _crossingsPointer += _crossings.SetDynamicInt32(_crossingsPointer, -(int)(vertex.LocalId + 1));
        }
        _crossingsPointer += _crossings.SetDynamicUInt32(_crossingsPointer, edgeTypeId);
        _crossingsPointer += _crossings.SetGlobalEdgeId(_crossingsPointer, globalEdgeId);

        var a = this.SetAttributes(attributes);
        _crossingsPointer += _crossings.SetDynamicUInt32(_crossingsPointer, a);
    }

    /// <summary>
    /// Gets all boundary crossing edges.
    /// </summary>
    /// <returns>An enumerable with all boundary crossing edges.</returns>
    public IEnumerable<(bool isIncoming, GlobalEdgeId globalEdgeId, VertexId vertex,
        IEnumerable<(string key, string value)> attributes, uint edgeTypeId)> GetBoundaryCrossings()
    {
        var pointer = 0L;
        while (pointer < _crossingsPointer)
        {
            _crossingsPointer += _crossings.GetDynamicInt32(_crossingsPointer, out var localIdSigned);
            bool isIncoming;
            uint localId;
            if (localIdSigned > 0)
            {
                localId = (uint)(localIdSigned - 1);
                isIncoming = true;
            }
            else
            {
                localId = (uint)(-localIdSigned - 1);
                isIncoming = false;
            }
            _crossingsPointer += _crossings.GetDynamicUInt32(_crossingsPointer, out var edgeTypeId);
            _crossingsPointer += _crossings.GetGlobalEdgeId(_crossingsPointer, out var globalEdgeId);
            _crossingsPointer += _crossings.GetDynamicUInt32(_crossingsPointer, out var a);

            yield return (isIncoming, globalEdgeId, new VertexId(this.TileId, localId), this.GetAttributes(a),
                edgeTypeId);
        }
    }
}
