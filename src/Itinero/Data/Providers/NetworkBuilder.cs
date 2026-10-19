using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Mutation;

namespace Itinero.Data.Providers;

/// <summary>
/// The means to write a network, handed to a provider by Itinero.
/// </summary>
/// <remarks>
/// Public so an out-of-tree provider can be given one, with an internal constructor so only
/// Itinero can make one: writing is impossible without being handed the means, and being handed
/// the means is what it is to be a provider.
/// </remarks>
/// <remarks>
/// It holds the network's write slot for as long as it lives, and disposing it is what publishes
/// what was written. So it is always used in a <c>using</c> — there is no separate commit to
/// remember, and no way to leave the slot held.
/// </remarks>
public sealed class NetworkBuilder : IDisposable
{
    private readonly RoutingNetworkMutator _writer;

    internal NetworkBuilder(RoutingNetworkMutator writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Publishes everything written as the db's latest network.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    public VertexId AddVertex(double longitude, double latitude, float? e = null)
    {
        return _writer.AddVertex(longitude, latitude, e);
    }

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    public VertexId AddVertex((double longitude, double latitude, float? e) location)
    {
        return _writer.AddVertex(location.longitude, location.latitude, location.e);
    }

    /// <summary>
    /// Adds a new edge, with the edge type id the provider assigned it.
    /// </summary>
    /// <remarks>
    /// Returns the <see cref="EdgeId"/>, which is the only thing a provider is asked about an edge
    /// later, so recording it against whatever the provider kept back needs no id in the tile.
    /// The shape is measured and discarded: an edge's geometry is the provider's, and its length
    /// is all the search needs.
    /// </remarks>
    public EdgeId AddEdge(VertexId tail, VertexId head, uint edgeTypeId,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null)
    {
        return _writer.AddEdge(tail, head, edgeTypeId, shape);
    }

    /// <summary>
    /// Adds turn costs, with the turn cost type id the provider assigned them.
    /// </summary>
    public bool AddTurnCosts(VertexId vertex,
        EdgeId[] edges, uint[,] costs, uint turnCostType, IEnumerable<EdgeId>? prefix = null)
    {
        return _writer.AddTurnCosts(vertex, edges, costs, turnCostType, prefix);
    }

    /// <summary>
    /// Gets an enumerator over what has been written so far.
    /// </summary>
    /// <remarks>
    /// Needed while writing: resolving a restriction means finding the edges it names.
    /// </remarks>
    public Enumerator GetEdgeEnumerator()
    {
        return new Enumerator(_writer.GetEdgeEnumerator());
    }

    /// <summary>
    /// Walks the edges written so far.
    /// </summary>
    public sealed class Enumerator
    {
        private readonly RoutingNetworkMutatorEdgeEnumerator _enumerator;

        internal Enumerator(RoutingNetworkMutatorEdgeEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <summary>Moves to the given edge, in the given direction.</summary>
        public bool MoveTo(EdgeId edge, bool forward) => _enumerator.MoveTo(edge, forward);

        /// <summary>Moves to the given vertex, before its first edge.</summary>
        public bool MoveTo(VertexId vertex) => _enumerator.MoveTo(vertex);

        /// <summary>Moves to the next edge at the current vertex.</summary>
        public bool MoveNext() => _enumerator.MoveNext();

        /// <summary>The current edge.</summary>
        public EdgeId EdgeId => _enumerator.EdgeId;

        /// <summary>The current edge's tail.</summary>
        public VertexId Tail => _enumerator.Tail;

        /// <summary>The current edge's head.</summary>
        public VertexId Head => _enumerator.Head;
    }

    /// <summary>
    /// Tries to get the location of the given vertex.
    /// </summary>
    public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude,
        out float? elevation)
    {
        return _writer.TryGetVertex(vertex, out longitude, out latitude, out elevation);
    }
}
