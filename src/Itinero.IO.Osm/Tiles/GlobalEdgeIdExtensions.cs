using System;
using OsmSharp;

namespace Itinero.IO.Osm.Tiles;

internal static class GlobalEdgeIdExtensions
{
    public static (Guid guid, bool forward) GenerateGlobalEdgeIdAndDirection(this Way way, int node1Idx, int node2Idx)
    {
        var forward = node1Idx < node2Idx;
        return forward ? (way.GenerateGlobalEdgeId(node1Idx, node2Idx), true) : (way.GenerateGlobalEdgeId(node2Idx, node1Idx), false);
    }

    public static (Guid guid, bool forward) GenerateGlobalEdgeIdAndDirection(long wayId, int node1Idx, int node2Idx)
    {
        var forward = node1Idx < node2Idx;
        return forward ? (GenerateGlobalEdgeId(wayId, node1Idx, node2Idx), true) : (GenerateGlobalEdgeId(wayId, node2Idx, node1Idx), false);
    }

    public static Guid GenerateGlobalEdgeId(this Way way, int node1Idx, int node2Idx)
    {
        return GenerateGlobalEdgeId(way.Id.Value, node1Idx, node2Idx);
    }

    public static Guid GenerateGlobalEdgeId(long wayId, int node1Idx, int node2Idx)
    {
        if (node1Idx >= node2Idx) throw new ArgumentException("edge is not given in forward direction");

        var data = new byte[16];
        BitConverter.GetBytes(wayId).CopyTo(data.AsSpan());
        BitConverter.GetBytes(node1Idx).CopyTo(data.AsSpan(8));
        BitConverter.GetBytes(node2Idx).CopyTo(data.AsSpan(12));

        return new Guid(data);
    }

    public static void ParseGlobalId(this Guid guid, out long wayId, out int node1Idx, out int node2Idx)
    {
        var bytes = guid.ToByteArray();
        wayId = BitConverter.ToInt64(bytes, 0);
        node1Idx = BitConverter.ToInt32(bytes, 8);
        node2Idx = BitConverter.ToInt32(bytes, 12);
    }
}
