using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;
using Itinero.Network;

namespace Itinero.Data.Providers;

/// <summary>
/// The default provider: holds everything in memory, keyed per way.
/// </summary>
/// <remarks>
/// Resident, no store, no laziness — the right shape for an extract or a test, and the base for
/// one that reads a format. Minting lives here because assigning ids is a provider's business.
/// </remarks>
public class DefaultDataProvider : DataProviderBase
{
    private readonly AttributeSetIndex _edgeTypes;
    private readonly AttributeSetIndex _turnCostTypes;
    private readonly AttributeSetMap _edgeTypeMap;

    private readonly AttributeSetMap _turnCostTypeMap;

    // keyed per way, so a way split across tiles or at junctions has one copy.
    private readonly Dictionary<long, (string key, string value)[]> _complement = new();
    private readonly Dictionary<long, (double longitude, double latitude, float? e)[]> _geometry = new();

    // what an EdgeId resolves to, per tile. This is the whole of what the tile used to carry.
    private readonly Dictionary<uint, TileSegments> _segments = new();

    // edges written one at a time rather than from a way: there is nothing to key them by and
    // nothing to share, so they are kept as given.
    private readonly Dictionary<EdgeId, (string key, string value)[]> _edgeAttributes = new();
    private readonly Dictionary<EdgeId, (double longitude, double latitude, float? e)[]> _edgeShape = new();

    /// <summary>
    /// Creates a new provider.
    /// </summary>
    /// <param name="edgeTypeMap">The map deciding which attributes make an edge type.</param>
    /// <param name="turnCostTypeMap">The map deciding which attributes make a turn cost type.</param>
    public DefaultDataProvider(AttributeSetMap? edgeTypeMap = null,
        AttributeSetMap? turnCostTypeMap = null)
        : this(new AttributeSetDictionaryIndex(), new AttributeSetDictionaryIndex(),
            edgeTypeMap, turnCostTypeMap)
    {
    }

    // the sets are the provider's own: it is the only thing that appends to them, so handing them
    // in from outside would be handing in the one piece it must control.
    private DefaultDataProvider(AttributeSetIndex edgeTypes, AttributeSetIndex turnCostTypes,
        AttributeSetMap? edgeTypeMap, AttributeSetMap? turnCostTypeMap)
        : base(edgeTypes, turnCostTypes)
    {
        _edgeTypes = edgeTypes;
        _turnCostTypes = turnCostTypes;
        _edgeTypeMap = edgeTypeMap ?? AttributeSetMap.Default();
        _turnCostTypeMap = turnCostTypeMap ?? AttributeSetMap.Default();
    }

    /// <summary>
    /// Begins writing, committing when the returned writer is disposed.
    /// </summary>
    /// <remarks>
    /// On this provider rather than the base: one that reads a file knows when it is done and
    /// commits itself, while this one is written to a call at a time and needs a scope to end.
    /// The write methods live on the writer, so they cannot be reached outside one.
    /// </remarks>
    public Writer BeginWrite()
    {
        return new Writer(this);
    }

    /// <summary>
    /// Writes to the provider, and commits when disposed.
    /// </summary>
    public sealed class Writer : IDisposable
    {
        private readonly DefaultDataProvider _provider;
        private readonly NetworkBuilder _builder;

        internal Writer(DefaultDataProvider provider)
        {
            _provider = provider;
            _builder = provider.BeginWriting();
        }

        /// <summary>
        /// Adds a vertex.
        /// </summary>
        public VertexId AddVertex(double longitude, double latitude, float? e = null)
        {
            return _builder.AddVertex(longitude, latitude, e);
        }

        /// <summary>
        /// Adds a vertex.
        /// </summary>
        public VertexId AddVertex((double longitude, double latitude, float? e) location)
        {
            return _builder.AddVertex(location);
        }

        /// <summary>
        /// Adds an edge, minting its edge type from the attributes.
        /// </summary>
        /// <remarks>
        /// The id and the attributes cannot disagree, because the same call produces both.
        /// </remarks>
        public EdgeId AddEdge(VertexId tail, VertexId head,
            IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            var all = attributes as ICollection<(string key, string value)> ??
                      attributes?.ToArray() ?? Array.Empty<(string key, string value)>();
            var points = shape as ICollection<(double longitude, double latitude, float? e)> ??
                         shape?.ToArray();

            // the shape still goes to the builder, which needs it to measure the edge.
            var edge = _builder.AddEdge(tail, head, _provider.AddEdgeType(all), points);

            _provider.SetEdgeData(edge, all, points);

            return edge;
        }

        /// <summary>
        /// Adds turn costs, minting their type from the attributes.
        /// </summary>
        public bool AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            return _builder.AddTurnCosts(vertex, edges, costs,
                _provider.AddTurnCostType(attributes), prefix);
        }

        /// <summary>
        /// Gets an enumerator over what has been written so far.
        /// </summary>
        public NetworkBuilder.Enumerator GetEdgeEnumerator()
        {
            return _builder.GetEdgeEnumerator();
        }

        /// <summary>
        /// Publishes everything written in this scope.
        /// </summary>
        public void Dispose()
        {
            _builder.Dispose();
        }
    }

    /// <summary>
    /// Gets the id for the given attributes, appending a new edge type if this set is unseen.
    /// </summary>
    public uint AddEdgeType(IEnumerable<(string key, string value)> attributes)
    {
        return _edgeTypes.Get(_edgeTypeMap.Map(attributes));
    }

    /// <summary>
    /// Gets the id for the given attributes, appending a new turn cost type if this set is unseen.
    /// </summary>
    public uint AddTurnCostType(IEnumerable<(string key, string value)> attributes)
    {
        return _turnCostTypes.Get(_turnCostTypeMap.Map(attributes));
    }

    /// <summary>
    /// Records an edge's own attributes and shape, for an edge that came from no way.
    /// </summary>
    /// <remarks>
    /// Only the complement is kept, as for a way: what the edge type map took is already in the
    /// type index, and <c>GetAttributes</c> is unioned with it.
    /// </remarks>
    internal void SetEdgeData(EdgeId edge, ICollection<(string key, string value)> attributes,
        ICollection<(double longitude, double latitude, float? e)>? shape)
    {
        if (attributes.Count > 0)
        {
            var kept = new HashSet<(string key, string value)>(_edgeTypeMap.Map(attributes));
            var complement = attributes.Where(a => !kept.Contains(a)).ToArray();

            if (complement.Length > 0) _edgeAttributes[edge] = complement;
        }

        if (shape is { Count: > 0 }) _edgeShape[edge] = shape.ToArray();
    }

    /// <summary>
    /// Records which way segment an edge came from, so the edge's own data can be found again.
    /// </summary>
    /// <remarks>
    /// Called with the <see cref="EdgeId"/> the builder just returned. Edges are appended in
    /// increasing <c>LocalId</c> within a tile, so the array is sorted by construction and a read
    /// is a binary search rather than a hash lookup.
    /// </remarks>
    public void SetEdgeSegment(EdgeId edge, long wayId, ushort tailIndex, ushort headIndex)
    {
        if (!_segments.TryGetValue(edge.TileId, out var segments))
        {
            segments = new TileSegments();
            _segments[edge.TileId] = segments;
        }

        segments.Append(edge.LocalId, wayId, tailIndex, headIndex);
    }

    /// <summary>
    /// Records a way's attributes, keeping only what the edge type map dropped.
    /// </summary>
    /// <remarks>
    /// The complement, not the full set: what the map keeps is already in the edge type index, so
    /// storing both would store it twice and materialisation unions the two.
    /// </remarks>
    public void SetWayAttributes(long wayId, IEnumerable<(string key, string value)> attributes)
    {
        var all = attributes as ICollection<(string key, string value)> ?? attributes.ToArray();
        var kept = new HashSet<(string key, string value)>(_edgeTypeMap.Map(all));

        _complement[wayId] = all.Where(a => !kept.Contains(a)).ToArray();
    }

    /// <summary>
    /// Records a way's node geometry, from which an edge's shape is a slice.
    /// </summary>
    public void SetWayGeometry(long wayId,
        IEnumerable<(double longitude, double latitude, float? e)> nodes)
    {
        _geometry[wayId] = nodes.ToArray();
    }

    /// <inheritdoc/>
    public override IEnumerable<(string key, string value)> GetAttributes(EdgeId edge)
    {
        if (_edgeAttributes.TryGetValue(edge, out var own)) return own;

        if (!this.TryGetSegment(edge, out var wayId, out _, out _))
        {
            return Enumerable.Empty<(string key, string value)>();
        }

        return _complement.TryGetValue(wayId, out var attributes)
            ? attributes
            : Enumerable.Empty<(string key, string value)>();
    }

    /// <inheritdoc/>
    public override void GetShape(EdgeId edge,
        List<(double longitude, double latitude, float? e)> into)
    {
        if (_edgeShape.TryGetValue(edge, out var own))
        {
            into.AddRange(own);

            return;
        }

        if (!this.TryGetSegment(edge, out var wayId, out var from, out var to)) return;
        if (!_geometry.TryGetValue(wayId, out var nodes)) return;

        // the shape is the way's nodes strictly between the edge's two, in the edge's direction.
        if (from <= to)
        {
            for (var n = from + 1; n < to && n < nodes.Length; n++) into.Add(nodes[n]);
        }
        else
        {
            for (var n = from - 1; n > to && n < nodes.Length; n--) into.Add(nodes[n]);
        }
    }

    private bool TryGetSegment(EdgeId edge, out long wayId, out ushort tailIndex, out ushort headIndex)
    {
        wayId = default;
        tailIndex = default;
        headIndex = default;

        return _segments.TryGetValue(edge.TileId, out var segments) &&
               segments.TryGet(edge.LocalId, out wayId, out tailIndex, out headIndex);
    }

    /// One tile's edges, in the order they were written.
    private sealed class TileSegments
    {
        private uint[] _localIds = new uint[16];
        private long[] _ways = new long[16];
        private ushort[] _tails = new ushort[16];
        private ushort[] _heads = new ushort[16];
        private int _count;

        public void Append(uint localId, long wayId, ushort tailIndex, ushort headIndex)
        {
            // sorted by construction is the whole reason this is an array: an out-of-order append
            // would make the search below miss silently, so it is a fault rather than a resort.
            if (_count > 0 && localId <= _localIds[_count - 1])
            {
                throw new ArgumentOutOfRangeException(nameof(localId),
                    $"Edges have to be recorded in the order they were written: {localId} came " +
                    $"after {_localIds[_count - 1]}.");
            }

            if (_count == _localIds.Length)
            {
                Array.Resize(ref _localIds, _count * 2);
                Array.Resize(ref _ways, _count * 2);
                Array.Resize(ref _tails, _count * 2);
                Array.Resize(ref _heads, _count * 2);
            }

            _localIds[_count] = localId;
            _ways[_count] = wayId;
            _tails[_count] = tailIndex;
            _heads[_count] = headIndex;
            _count++;
        }

        public bool TryGet(uint localId, out long wayId, out ushort tailIndex, out ushort headIndex)
        {
            var found = Array.BinarySearch(_localIds, 0, _count, localId);
            if (found < 0)
            {
                wayId = default;
                tailIndex = default;
                headIndex = default;

                return false;
            }

            wayId = _ways[found];
            tailIndex = _tails[found];
            headIndex = _heads[found];

            return true;
        }
    }
}
