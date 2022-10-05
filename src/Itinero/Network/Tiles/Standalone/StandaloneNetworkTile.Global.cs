using System;
using System.Collections.Generic;
using Itinero.Network.Storage;
using Itinero.Network.TurnCosts;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    private uint _turnCostPointer = 0;
    private readonly ArrayBase<byte> _turnCosts = new MemoryArray<byte>(0);

    internal void AddGlobalTurnCosts((Guid globalEdgeId, bool forward)[] edges, uint[,] costs, uint turnCostType,
        IEnumerable<(string key, string value)> attributes)
    {
        if (edges.Length > OrderCoder.MAX_ORDER_HEAD_TAIL)
        {
            throw new ArgumentException(
                $"Cannot add turn costs for vertices with more than {OrderCoder.MAX_ORDER_HEAD_TAIL} edges.");
        }

        // make sure there is space in the turn cost array.
        var maxLength = _turnCostPointer + 5 + 5 + 5 +
                        (edges.Length * (16 + 5)) +
                        (costs.GetLength(0) * costs.GetLength(1) * 5);
        while (_turnCosts.Length < maxLength)
        {
            _turnCosts.Resize(_turnCosts.Length + 256);
        }

        // add turn.
        var a = this.SetAttributes(attributes);
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, a);
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, turnCostType);
        _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, (uint)edges.Length);
        for (var i = 0; i < edges.Length; i++)
        {
            var (globalEdgeId, forward) = edges[i];

            _turnCostPointer += (uint)_turnCosts.SetGuid(_turnCostPointer, globalEdgeId);
            _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, (uint)(forward ? 1 : 0));
        }

        for (var x = 0; x < costs.GetLength(0); x++)
            for (var y = 0; y < costs.GetLength(1); y++)
            {
                _turnCostPointer += (uint)_turnCosts.SetDynamicUInt32(_turnCostPointer, costs[x, y]);
            }
    }

    /// <summary>
    /// Gets all the global turn costs.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<((Guid globalEdgeId, bool forward)[] edges, uint[,] costs, uint turnCostType,
        IEnumerable<(string key, string value)> attributes)> GetGlobalTurnCost()
    {
        var pointer = 0L;
        while (pointer < _turnCostPointer)
        {
            pointer += _turnCosts.GetDynamicUInt32(pointer, out var a);
            pointer += _turnCosts.GetDynamicUInt32(pointer, out var turnCostType);
            pointer += _turnCosts.GetDynamicUInt32(pointer, out var edgeCount);
            var edges = new (Guid globalEdgeId, bool forward)[edgeCount];
            for (var i = 0; i < edgeCount; i++)
            {
                _turnCostPointer += (uint)_turnCosts.GetGuid(_turnCostPointer, out var globalEdgeId);
                _turnCostPointer += (uint)_turnCosts.GetDynamicUInt32(_turnCostPointer, out var forwardValue);
                var forward = forwardValue == 1;

                edges[i] = (globalEdgeId, forward);
            }

            var costs = new uint[edges.Length, edges.Length];
            for (var x = 0; x < costs.GetLength(0); x++)
                for (var y = 0; y < costs.GetLength(1); y++)
                {
                    pointer += _crossings.GetDynamicUInt32(pointer, out var cost);
                    costs[x, y] = cost;
                }

            yield return (edges, costs, turnCostType, this.GetAttributes(a));
        }
    }

    private uint _globalIdPointer = 0;
    private readonly ArrayBase<byte> _globalIds = new MemoryArray<byte>(0);

    internal void AddGlobalIdFor(EdgeId edgeId, Guid globalEdgeId)
    {
        // make sure there is space.
        var maxLength = _globalIdPointer + 16 + 5;
        while (_globalIds.Length < maxLength)
        {
            _globalIds.Resize(_globalIds.Length + 64);
        }

        _globalIdPointer += (uint)_globalIds.SetGuid(_globalIdPointer, globalEdgeId);
        _globalIdPointer += (uint)_globalIds.SetDynamicInt32(_globalIdPointer, (int)edgeId.LocalId);
    }

    internal void AddGlobalIdFor(BoundaryEdgeId boundaryEdgeId, Guid globalEdgeId)
    {
        // make sure there is space.
        var maxLength = _globalIdPointer + 16 + 5;
        while (_globalIds.Length < maxLength)
        {
            _globalIds.Resize(_globalIds.Length + 64);
        }

        _globalIdPointer += (uint)_globalIds.SetGuid(_globalIdPointer, globalEdgeId);
        _globalIdPointer += (uint)_globalIds.SetDynamicInt32(_globalIdPointer, (int)-(boundaryEdgeId.LocalId + 1));
    }

    /// <summary>
    /// Gets all the global edge ids.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<(Guid globalEdgeId, EdgeId? edgeId, BoundaryEdgeId? boundaryEdgeId)> GetGlobalEdgeIds()
    {
        var pointer = 0L;
        while (pointer < _turnCostPointer)
        {
            pointer += _turnCosts.GetGuid(pointer, out var globalEdgeId);
            pointer += _turnCosts.GetDynamicInt32(pointer, out var localId);

            if (localId >= 0)
            {
                yield return (globalEdgeId, new EdgeId(this.TileId, (uint)localId), null);
            }
            else
            {
                yield return (globalEdgeId, null, new BoundaryEdgeId((uint)((-localId) - 1)));
            }
        }
    }
}
