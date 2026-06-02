using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.DataStructures;

namespace Itinero.Routing.Flavours.Dijkstra;

internal static class PathTreeExtensions
{
    // TODO: this can all be represented much more compact.

    // data6 bit layout:
    //   bit 0 = forward
    //   bit 1 = leftMain (state-bit for access-aware edge-based Dijkstra)
    //   bit 2 = localAccess
    // Backward-compat: callers that don't track state-bits pack only bit 0,
    // so the existing GetVisit forward read continues to work via the mask.
    private const uint ForwardBit = 1U;
    private const uint LeftMainBit = 2U;
    private const uint LocalAccessBit = 4U;

    /// <summary>
    /// Adds a new visit the path tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="vertex">The vertex.</param>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The edge direction.</param>
    /// <param name="head">The head index.</param>
    /// <param name="previousPointer">The pointer to the previous entry.</param>
    /// <returns>A pointer to the visit.</returns>
    public static uint AddVisit(this PathTree tree, VertexId vertex, EdgeId edge, bool forward, byte? head,
        uint previousPointer)
        => AddVisit(tree, vertex, edge, forward, leftMain: false, localAccess: false, head, previousPointer);

    /// <summary>
    /// Access-aware overload. Packs the <paramref name="leftMain"/> and
    /// <paramref name="localAccess"/> state-bits alongside the direction so the
    /// edge-based Dijkstra can recover them O(1) on pop.
    /// </summary>
    public static uint AddVisit(this PathTree tree, VertexId vertex, EdgeId edge, bool forward,
        bool leftMain, bool localAccess, byte? head, uint previousPointer)
    {
        var data0 = vertex.TileId;
        var data1 = vertex.LocalId;
        var data2 = edge.TileId;
        var data3 = edge.LocalId;
        var data4 = head ?? uint.MaxValue;
        var data5 = previousPointer;
        var data6 = (forward ? ForwardBit : 0U)
                  | (leftMain ? LeftMainBit : 0U)
                  | (localAccess ? LocalAccessBit : 0U);

        return tree.Add(data0, data1, data2, data3, data4, data5, data6);
    }

    /// <summary>
    /// Adds a new visit to the path tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="enumerator">The current edge.</param>
    /// <param name="previousPointer">The pointer to the previous entry.</param>
    /// <returns>A pointer to the visit.</returns>
    public static uint AddVisit(this PathTree tree, RoutingNetworkEdgeEnumerator enumerator, uint previousPointer)
    {
        return tree.AddVisit(enumerator.Head, enumerator.EdgeId, enumerator.Forward, enumerator.HeadOrder,
            previousPointer);
    }

    /// <summary>
    /// Access-aware overload of <see cref="AddVisit(PathTree, RoutingNetworkEdgeEnumerator, uint)"/>.
    /// </summary>
    public static uint AddVisit(this PathTree tree, RoutingNetworkEdgeEnumerator enumerator,
        bool leftMain, bool localAccess, uint previousPointer)
    {
        return tree.AddVisit(enumerator.Head, enumerator.EdgeId, enumerator.Forward,
            leftMain, localAccess, enumerator.HeadOrder, previousPointer);
    }

    /// <summary>
    /// Gets the visit at the given location.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="pointer">The pointer.</param>
    /// <returns>The visit.</returns>
    public static (VertexId vertex, EdgeId edge, bool forward, byte? head, uint previousPointer) GetVisit(this PathTree tree,
        uint pointer)
    {
        tree.Get(pointer, out var data0, out var data1, out var data2, out var data3, out var data4, out var data5, out var data6);

        var head = data4 == uint.MaxValue ? null : (byte?)data4;

        return (new VertexId(data0, data1), new EdgeId(data2, data3), (data6 & ForwardBit) != 0, head, data5);
    }

    /// <summary>
    /// Access-aware overload of <see cref="GetVisit"/> that also returns the
    /// packed <c>leftMain</c> and <c>localAccess</c> state-bits.
    /// </summary>
    public static (VertexId vertex, EdgeId edge, bool forward, bool leftMain, bool localAccess, byte? head, uint previousPointer)
        GetVisitWithState(this PathTree tree, uint pointer)
    {
        tree.Get(pointer, out var data0, out var data1, out var data2, out var data3, out var data4, out var data5, out var data6);

        var head = data4 == uint.MaxValue ? null : (byte?)data4;

        return (new VertexId(data0, data1), new EdgeId(data2, data3),
            (data6 & ForwardBit) != 0,
            (data6 & LeftMainBit) != 0,
            (data6 & LocalAccessBit) != 0,
            head, data5);
    }

    /// <summary>
    /// Enumerates the edges backwards from the visit at the given pointer.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="pointer">The pointer.</param>
    /// <returns>The edges and their turn.</returns>
    public static IEnumerable<(EdgeId edge, byte? turn)> GetPreviousEdges(this PathTree tree, uint pointer)
    {
        while (pointer != uint.MaxValue)
        {
            var (_, edge, _, head, next) = tree.GetVisit(pointer);

            yield return (edge, head);

            pointer = next;
        }
    }

#if DEBUG
    public static IEnumerable<(EdgeId edge, bool forward, byte? turn)> GetPathDebug(this PathTree tree, uint pointer)
    {
        while (pointer != uint.MaxValue)
        {
            var (_, edge, forward, head, next) = tree.GetVisit(pointer);

            yield return (edge, forward, head);

            pointer = next;
        }
    }
#endif
}
