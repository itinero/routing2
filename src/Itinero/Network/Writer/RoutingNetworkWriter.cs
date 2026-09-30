using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Itinero.Geo;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone.Global;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Network.Writer;

/// <summary>
/// A writer to write to a network. This writer will never change existing data, only add new data.
///
/// This writer can:
/// - add new vertices
/// - add new edges.
///
/// This writer cannot mutate existing data, only add new.
/// </summary>
public class RoutingNetworkWriter : IDisposable
{
    private readonly IRoutingNetworkWritable _network;

    /// <summary>
    /// Whether this writer holds the network's exclusive write slot.
    /// </summary>
    /// <remarks>
    /// False for tile-insert writers, of which there can be many at once. They are excluded from
    /// the exclusive slot rather than sharing it, so disposing one must not release a slot it
    /// never took — that would let a mutator open while inserts are still running.
    /// </remarks>
    private readonly bool _exclusive;

    internal RoutingNetworkWriter(IRoutingNetworkWritable network, bool exclusive = true)
    {
        _network = network;
        _exclusive = exclusive;
    }

    /// <summary>
    /// Gets an edge enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    internal RoutingNetworkEdgeEnumerator GetEdgeEnumerator()
    {
        return _network.GetEdgeEnumerator();
    }

    /// <summary>
    /// Runs <paramref name="write"/> holding the lock of every tile it may write.
    /// </summary>
    /// <remarks>
    /// Always ascending by tile id, and that is the single rule keeping concurrent insertion
    /// deadlock-free. It only works if EVERY acquisition anywhere follows it: two writes that
    /// each hold one of the same pair and wait for the other is the only cycle available here, and
    /// a consistent global order makes it unconstructable.
    ///
    /// The set is passed in rather than discovered while locking, because a write that finds out
    /// which tiles it needs as it goes cannot order them.
    ///
    /// Monitor is re-entrant, so nesting these (a caller that already holds one of the locks) is
    /// free rather than a self-deadlock.
    /// </remarks>
    /// <summary>
    /// Holds the write locks for a set of tiles until disposed.
    /// </summary>
    /// <remarks>
    /// Taken ONCE around all of a tile's writes, not per write. An earlier version locked inside
    /// every <c>AddEdge</c>/<c>AddVertex</c> call, which is the wrong granularity: the cost of
    /// acquiring and releasing, plus a closure and an array per call, is paid on every edge while
    /// the work it protects is a few array writes. That measured as roughly a third more CPU per
    /// route than serialised insertion, for identical output.
    ///
    /// The whole set must be passed at once. Acquiring one stripe and then reaching for another is
    /// what creates cycles — a caller holding its own tile's stripe and then asking for a
    /// partner's can deadlock against the mirror-image insert — so every lock a write session
    /// needs has to be known before the first one is taken.
    /// </remarks>
    internal readonly struct TileWriteScope : IDisposable
    {
        private readonly object[]? _taken;

        internal TileWriteScope(IRoutingNetworkWritable network, ReadOnlySpan<uint> tileIds)
        {
            // Stripe indices, deduplicated and ascending. Ordering is over STRIPES, not tile ids:
            // see IRoutingNetworkWritable.TileLockIndex.
            Span<int> indices = stackalloc int[tileIds.Length];
            var count = 0;
            foreach (var tileId in tileIds)
            {
                var index = network.TileLockIndex(tileId);
                var insertAt = count;
                while (insertAt > 0 && indices[insertAt - 1] > index)
                {
                    indices[insertAt] = indices[insertAt - 1];
                    insertAt--;
                }

                if (insertAt > 0 && indices[insertAt - 1] == index) continue;
                if (insertAt < count && indices[insertAt] == index) continue;

                indices[insertAt] = index;
                count++;
            }

            _taken = count == 0 ? null : new object[count];
            for (var i = 0; i < count; i++)
            {
                var stripe = network.GetTileLockByIndex(indices[i]);
                Monitor.Enter(stripe);
                _taken[i] = stripe;
            }
        }

        public void Dispose()
        {
            if (_taken == null) return;

            for (var i = _taken.Length - 1; i >= 0; i--)
            {
                Monitor.Exit(_taken[i]);
            }
        }
    }

    /// <summary>
    /// Locks every tile a write session will touch, in a deadlock-free order.
    /// </summary>
    internal TileWriteScope LockTiles(ReadOnlySpan<uint> tileIds) => new(_network, tileIds);

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>The vertex id.</returns>
    public VertexId AddVertex(double longitude, double latitude, float? elevation = null)
    {
        // get the local tile id.
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
        var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

        // get the tile (or create it).
        var (tile, _) = _network.GetTileForWrite(localTileId);

        return tile.AddVertex(longitude, latitude, elevation);
    }

    /// <summary>
    /// Computes the edge length in centimeters from vertex locations and shape.
    /// </summary>
    public uint ComputeEdgeLength(VertexId tail, VertexId head,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null)
    {
        if (!_network.TryGetVertex(tail, out var lon1, out var lat1, out var e1))
        {
            throw new ArgumentOutOfRangeException(nameof(tail), $"Vertex {tail} not found.");
        }

        if (!_network.TryGetVertex(head, out var lon2, out var lat2, out var e2))
        {
            throw new ArgumentOutOfRangeException(nameof(head), $"Vertex {head} not found.");
        }

        return (uint)((lon1, lat1, e1).DistanceEstimateInMeterShape(
            (lon2, lat2, e2), shape) * 100);
    }

    /// <summary>
    /// Adds a new edge.
    /// </summary>
    /// <param name="tail">The tail vertex.</param>
    /// <param name="head">The head vertex.</param>
    /// <param name="shape">The shape, if any.</param>
    /// <param name="attributes">The attributes, if any.</param>
    /// <param name="edgeTypeId">The edge type id, if any.</param>
    /// <param name="length">The length in centimeters. Use <see cref="ComputeEdgeLength"/> if not known.</param>
    /// <param name="globalEdgeId">The global edge id, if any.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public EdgeId AddEdge(VertexId tail, VertexId head,
        IEnumerable<(double longitude, double latitude, float? e)>? shape,
        IEnumerable<(string key, string value)>? attributes, uint? edgeTypeId,
        uint length, GlobalEdgeId? globalEdgeId = null)
    {
        // get the tile (or create it).
        var (tile, edgeTypeMap) = _network.GetTileForWrite(tail.TileId);
        if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

        // get the edge type id.
        edgeTypeId ??= attributes != null ? edgeTypeMap(attributes) : null;

        var edge1 = tile.AddEdge(tail, head, shape, attributes, null, edgeTypeId, length, globalEdgeId);
        if (tail.TileId == head.TileId)
        {
            return edge1;
        }

        // this edge crosses tiles, also add an extra edge to the other tile.
        (tile, _) = _network.GetTileForWrite(head.TileId);
        tile.AddEdge(tail, head, shape, attributes, edge1, edgeTypeId, length, globalEdgeId);

        return edge1;
    }

    /// Returns false when the vertex has no order budget left, see OrderCoder.
    public bool AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
        EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix, uint? turnCostType = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        // Which tiles this will write, decided before any lock is taken. Beyond the vertex's own
        // tile, the order bytes are synced into every neighbouring tile that shares a cross-tile
        // edge at this vertex — so the set is not predictable from the vertex alone and has to be
        // read off the network first. A write that discovered these while already holding locks
        // could not order them, and ordering is the whole deadlock argument.
        var tileIds = new List<uint> { vertex.TileId };
        {
            var (vertexTile, _) = _network.GetTileForWrite(vertex.TileId);
            if (vertexTile == null)
            {
                throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
            }

            var scan = new NetworkTileEnumerator();
            scan.MoveTo(vertexTile);
            if (scan.MoveTo(vertex))
            {
                while (scan.MoveNext())
                {
                    if (scan.Tail.TileId == scan.Head.TileId) continue;
                    tileIds.Add(scan.Head.TileId);
                }
            }
        }

        // Locks here rather than at the caller, because which tiles this touches is only
        // discoverable by reading the network. Restrictions are rare compared to edges, so paying
        // per call is acceptable where it would not be for AddEdge.
        using var scope = this.LockTiles(System.Runtime.InteropServices.CollectionsMarshal
            .AsSpan(tileIds));

        return this.AddTurnCostsCore(vertex, attributes, edges, costs, prefix, turnCostType);
    }

    /// The turn-cost write itself. Caller holds every tile lock it touches.
    private bool AddTurnCostsCore(VertexId vertex, IEnumerable<(string key, string value)> attributes,
        EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId> prefix, uint? turnCostType)
    {
        // get the tile (or create it).
        var (tile, _) = _network.GetTileForWrite(vertex.TileId);
        if (tile == null)
        {
            throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
        }

        // get the turn cost type id.
        var turnCostMap = _network.RouterDb.GetTurnCostTypeMap();
        turnCostType ??= turnCostMap.func(attributes);

        // add the turn cost table using the type id.
        if (!tile.AddTurnCosts(vertex, turnCostType.Value, edges, costs, attributes, prefix))
        {
            return false;
        }

        // for cross-tile edges, the order was set on this tile's copy.
        // sync the order to the other tile's copy so routing from either side sees it.
        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(tile);
        if (enumerator.MoveTo(vertex))
        {
            while (enumerator.MoveNext())
            {
                // only cross-tile edges need syncing.
                if (enumerator.Tail.TileId == enumerator.Head.TileId) continue;

                // Head is always the other vertex (Tail = turn cost vertex we enumerated from).
                var (otherTile, _) = _network.GetTileForWrite(enumerator.Head.TileId);
                if (otherTile == null) continue;

                // find the same edge in the other tile by iterating from the other vertex.
                var otherEnumerator = new NetworkTileEnumerator();
                otherEnumerator.MoveTo(otherTile);
                if (!otherEnumerator.MoveTo(enumerator.Head)) continue;

                while (otherEnumerator.MoveNext())
                {
                    if (otherEnumerator.EdgeId != enumerator.EdgeId) continue;

                    // found the same edge — copy the order bytes.
                    // SetTailHeadOrder takes STORED tail/head orders (for vertex1/vertex2 as encoded).
                    // enumerator.TailOrder = order at turn cost vertex, HeadOrder = order at other vertex.
                    // Map these to stored positions based on otherEnumerator.Forward:
                    // Forward=true: vertex1=otherVertex → stored tail=HeadOrder, stored head=TailOrder
                    // Forward=false: vertex1=turnCostVertex → stored tail=TailOrder, stored head=HeadOrder
                    otherTile.SetTailHeadOrder(otherEnumerator.EdgePointer,
                        otherEnumerator.Forward ? enumerator.HeadOrder : enumerator.TailOrder,
                        otherEnumerator.Forward ? enumerator.TailOrder : enumerator.HeadOrder);
                    break;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Shrinks a tile's buffers to what they hold, taking its lock.
    /// </summary>
    internal void TrimTile(uint localTileId)
    {
        using var scope = this.LockTiles(stackalloc[] { localTileId });
        var (tile, _) = _network.GetTileForWrite(localTileId);
        tile?.Trim();
    }

    internal void AddTile(NetworkTile tile)
    {
        _network.SetTile(tile);
    }

    internal bool HasTile(uint localTileId)
    {
        return _network.HasTile(localTileId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_exclusive)
        {
            _network.ClearWriter();
        }
        else
        {
            _network.ReleaseTileWriter();
        }
    }
}
