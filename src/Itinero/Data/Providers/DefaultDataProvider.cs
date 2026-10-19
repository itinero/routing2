using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone.Global;

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
            attributes ??= Array.Empty<(string key, string value)>();

            return _builder.AddEdge(tail, head, _provider.AddEdgeType(attributes), shape, attributes);
        }

        /// <summary>
        /// Adds turn costs, minting their type from the attributes.
        /// </summary>
        public bool AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            return _builder.AddTurnCosts(vertex, attributes, edges, costs,
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
    public override IEnumerable<(string key, string value)> GetAttributes(EdgeId edge, GlobalEdgeId globalId)
    {
        return _complement.TryGetValue(globalId.EdgeId, out var attributes)
            ? attributes
            : Enumerable.Empty<(string key, string value)>();
    }

    /// <inheritdoc/>
    public override void GetShape(EdgeId edge, GlobalEdgeId globalId,
        List<(double longitude, double latitude, float? e)> into)
    {
        if (!_geometry.TryGetValue(globalId.EdgeId, out var nodes)) return;

        // the shape is the way's nodes strictly between the edge's two, in the edge's direction.
        var (from, to) = (globalId.Tail, globalId.Head);
        if (from <= to)
        {
            for (var n = from + 1; n < to && n < nodes.Length; n++) into.Add(nodes[n]);
        }
        else
        {
            for (var n = from - 1; n > to && n < nodes.Length; n--) into.Add(nodes[n]);
        }
    }
}
