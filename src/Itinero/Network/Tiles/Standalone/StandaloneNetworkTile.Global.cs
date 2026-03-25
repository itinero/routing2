using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;
using Itinero.Network.Storage;
using Itinero.Network.Tiles.Standalone.Global;
using Itinero.Network.TurnCosts;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    // here we store all the data about global restrictions that could not be turned into turn costs yet because the network is not complete at
    // the edge of the tiles. we store the global edge ids of the restriction along with the type so we can create the turn cost when the network
    // is being completed
    private uint _globalRestrictionsPointer;
    private byte[] _globalRestrictions = new byte[0];

    /// <summary>
    /// Adds a global restriction for processing when the tile is loaded.
    /// </summary>
    /// <param name="sequence">The sequence of global edge ids with local edge ids if they are already known.</param>
    /// <param name="isProhibitory">The type of restriction.</param>
    /// <param name="turnCostTypeId">The turn cost type, already determined.</param>
    /// <param name="attributes">The raw attributes of the restriction.</param>
    public void AddGlobalRestriction(IEnumerable<(GlobalEdgeId globalEdgeId, EdgeId? edge)> sequence,
        bool isProhibitory, uint turnCostTypeId, IEnumerable<(string key, string value)> attributes)
    {
        var edges = sequence.ToList();
        if (edges.Count > OrderCoder.MaxOrderHeadTail) throw new ArgumentException(
                $"Cannot add turn costs for vertices with more than {OrderCoder.MaxOrderHeadTail} edges.");

        // make sure there is space in the turn cost array.
        var maxLength = _globalRestrictionsPointer + 5 + 5 + 5 +
                        (edges.Count * (9 + 5 + 5 + 5));
        while (_globalRestrictions.Length < maxLength)
        {
            Array.Resize(ref _globalRestrictions, (int)(_globalRestrictions.Length + 256));
        }

        // add turn.
        var a = this.SetAttributes(attributes);
        _globalRestrictionsPointer += _globalRestrictions.SetDynamicUInt32(_globalRestrictionsPointer, a);
        if (isProhibitory)
        {
            // isProhibitory if turnCostTypeId is encoded as a positive number.
            _globalRestrictionsPointer += _globalRestrictions.SetDynamicInt32(_globalRestrictionsPointer, (int)(turnCostTypeId + 1));
        }
        else
        {
            // not isProhibitory if turnCostTypeId is encoded as a negative number.
            _globalRestrictionsPointer += _globalRestrictions.SetDynamicInt32(_globalRestrictionsPointer, -(int)(turnCostTypeId + 1));
        }
        _globalRestrictionsPointer += _globalRestrictions.SetDynamicUInt32(_globalRestrictionsPointer, (uint)edges.Count);
        foreach (var (globalEdgeId, edgeId) in edges)
        {
            _globalRestrictionsPointer += _globalRestrictions.SetGlobalEdgeId(_globalRestrictionsPointer, globalEdgeId);
            if (edgeId == null)
            {
                _globalRestrictionsPointer += _globalRestrictions.SetDynamicUInt32Nullable(_globalRestrictionsPointer,
                    null);
            }
            else
            {
                _globalRestrictionsPointer += _globalRestrictions.SetDynamicUInt32Nullable(_globalRestrictionsPointer,
                    edgeId.Value.LocalId);
            }
        }
    }

    /// <summary>
    /// Gets all the global restrictions.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<(IReadOnlyList<(GlobalEdgeId globalEdgeId, EdgeId? edgeId)> edges, bool isProhibitory, uint turnCostTypeId,
        IEnumerable<(string key, string value)> attributes)> GetGlobalRestrictions()
    {
        var pointer = 0L;
        while (pointer < _globalRestrictionsPointer)
        {
            pointer += _globalRestrictions.GetDynamicUInt32(pointer, out var a);
            pointer += _globalRestrictions.GetDynamicInt32(pointer, out var turnCostTypeSigned);
            uint turnCostType;
            bool isProhibitory;
            if (turnCostTypeSigned > 0)
            {
                isProhibitory = true;
                turnCostType = (uint)turnCostTypeSigned - 1;
            }
            else
            {
                isProhibitory = false;
                turnCostType = (uint)(-turnCostTypeSigned - 1);
            }
            pointer += _globalRestrictions.GetDynamicUInt32(pointer, out var edgeCount);
            var edges = new (GlobalEdgeId globalEdgeId, EdgeId? edge)[edgeCount];
            for (var i = 0; i < edgeCount; i++)
            {
                pointer += _globalRestrictions.GetGlobalEdgeId(pointer, out var globalEdgeId);
                pointer += _globalRestrictions.GetDynamicUInt32Nullable(pointer,
                    out var localId);

                EdgeId? edgeId = null;
                if (localId != null)
                {
                    edgeId = new EdgeId(this.TileId, localId.Value);
                }

                edges[i] = (globalEdgeId, edgeId);
            }

            yield return (edges, isProhibitory, turnCostType, this.GetAttributes(a));
        }
    }

    private void WriteGlobal(Stream stream)
    {
        stream.WriteVarUInt32(_globalRestrictionsPointer);
        for (var i = 0; i < _globalRestrictionsPointer; i++)
        {
            stream.WriteByte(_globalRestrictions[i]);
        }
    }

    private void ReadGlobal(Stream stream)
    {
        _globalRestrictionsPointer = stream.ReadVarUInt32();
        _globalRestrictions = new byte[_globalRestrictionsPointer];
        for (var i = 0; i < _globalRestrictionsPointer; i++)
        {
            _globalRestrictions[i] = (byte)stream.ReadByte();
        }
    }

    private void ReadGlobal(byte[] data, ref int offset)
    {
        _globalRestrictionsPointer = BitCoderBuffer.GetVarUInt32(data, ref offset);
        _globalRestrictions = new byte[_globalRestrictionsPointer];
        Buffer.BlockCopy(data, offset, _globalRestrictions, 0, (int)_globalRestrictionsPointer);
        offset += (int)_globalRestrictionsPointer;
    }

    private void WriteGlobal(byte[] data, ref int offset)
    {
        BitCoderBuffer.SetVarUInt32(data, ref offset, _globalRestrictionsPointer);
        Buffer.BlockCopy(_globalRestrictions, 0, data, offset, (int)_globalRestrictionsPointer);
        offset += (int)_globalRestrictionsPointer;
    }
}
