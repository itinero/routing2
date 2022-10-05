using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Storage;
using Itinero.Network.Tiles.Standalone;

namespace Itinero.Network.Tiles;

internal class NetworkTileEnumerator : INetworkTileEdge, IStandaloneNetworkTileEnumerator
{
    private uint _localId;
    private uint? _nextEdgePointer;
    private uint? _shapePointer;
    private uint? _attributesPointer;
    private byte? _tailOrder;
    private byte? _headOrder;

    /// <summary>
    /// Creates a new graph tile enumerator.
    /// </summary>
    internal NetworkTileEnumerator()
    {
    }

    public NetworkTile? Tile { get; private set; }

    /// <summary>
    /// Gets the tile id.
    /// </summary>
    public uint TileId => this.Tile?.TileId ?? uint.MaxValue;

    /// <summary>
    /// Moves to the given tile.
    /// </summary>
    /// <param name="graphTile">The graph tile to move to.</param>
    /// <returns>True if the move succeeds.</returns>
    public void MoveTo(NetworkTile graphTile)
    {
        this.Tile = graphTile;

        this.Reset();
    }

    /// <summary>
    /// Move to the vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <returns>True if the move succeeds and the vertex exists.</returns>
    public bool MoveTo(VertexId vertex)
    {
        if (this.Tile == null)
        {
            throw new InvalidOperationException("Move to graph tile first.");
        }

        if (vertex.LocalId >= this.Tile.VertexCount)
        {
            return false;
        }

        _localId = vertex.LocalId;
        _nextEdgePointer = uint.MaxValue;
        this.EdgePointer = uint.MaxValue;

        this.Tail = vertex;
        return true;
    }

    /// <summary>
    /// Move to the given edge.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The forward flag.</param>
    /// <returns>True if the move succeeds and the edge exists.</returns>
    public bool MoveTo(EdgeId edge, bool forward)
    {
        if (this.Tile == null)
        {
            throw new InvalidOperationException("Move to graph tile first.");
        }

        if (this.TileId != edge.TileId)
        {
            throw new ArgumentOutOfRangeException(nameof(edge),
                "Cannot move to edge not in current tile, move to the tile first.");
        }

        _nextEdgePointer = edge.LocalId;
        if (edge.LocalId >= EdgeId.MinCrossId)
        {
            _nextEdgePointer = this.Tile.GetEdgeCrossPointer(edge.LocalId - EdgeId.MinCrossId);
        }
        this.EdgePointer = _nextEdgePointer.Value;

        // decode edge data.
        this.EdgeId = edge;
        var size = this.Tile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
        var vertex1 = new VertexId(tileId, localId);
        _nextEdgePointer += size;
        size = this.Tile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
        var vertex2 = new VertexId(tileId, localId);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out var vp1);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out var vp2);
        _nextEdgePointer += size;

        if (vertex1.TileId != vertex2.TileId)
        {
            size = this.Tile.DecodeEdgeCrossId(_nextEdgePointer.Value, out var edgeCrossId);
            _nextEdgePointer += size;

            this.EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
        }

        // get edge profile id.
        size = this.Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
        _nextEdgePointer += size;
        this.EdgeTypeId = edgeProfileId;

        // get length.
        size = this.Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
        _nextEdgePointer += size;
        this.Length = length;

        // get tail and head order.
        this.Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tailOrder, ref _headOrder);
        _nextEdgePointer++;

        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

        if (forward)
        {
            this.Tail = vertex1;
            this.Head = vertex2;
            this.Forward = true;

            _nextEdgePointer = vp1;
        }
        else
        {
            this.Tail = vertex2;
            this.Head = vertex1;
            this.Forward = false;

            (_headOrder, _tailOrder) = (_tailOrder, _headOrder);

            _nextEdgePointer = vp2;
        }

        return true;
    }

    /// <summary>
    /// Resets this enumerator.
    /// </summary>
    /// <remarks>
    /// Reset this enumerator to:
    /// - the first vertex for the currently selected edge.
    /// - the first vertex for the graph tile if there is no selected edge.
    /// </remarks>
    public void Reset()
    {
        if (this.Tile == null)
        {
            throw new InvalidOperationException("Cannot reset an empty enumerator.");
        }

        this.EdgePointer = uint.MaxValue;
        _nextEdgePointer = uint.MaxValue;
    }

    public bool IsEmpty => this.Tile == null;

    internal uint EdgePointer { get; private set; } = uint.MaxValue;

    /// <summary>
    /// Moves to the next edge for the current vertex.
    /// </summary>
    /// <returns>True when there is a new edge.</returns>
    public bool MoveNext()
    {
        this.EdgePointer = uint.MaxValue;

        if (this.Tile == null)
        {
            throw new InvalidOperationException("Move to graph tile first.");
        }

        if (_nextEdgePointer == uint.MaxValue)
        {
            // move to the first edge.
            _nextEdgePointer = this.Tile.VertexEdgePointer(_localId).DecodeNullableData();
        }

        if (_nextEdgePointer == null)
        {
            // no more data available.
            return false;
        }

        // decode edge data.
        this.EdgePointer = _nextEdgePointer.Value;
        this.EdgeId = new EdgeId(this.Tile.TileId, _nextEdgePointer.Value);
        var size = this.Tile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
        var vertex1 = new VertexId(tileId, localId);
        _nextEdgePointer += size;
        size = this.Tile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
        var vertex2 = new VertexId(tileId, localId);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out var vp1);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out var vp2);
        _nextEdgePointer += size;

        if (vertex1.TileId != vertex2.TileId)
        {
            size = this.Tile.DecodeEdgeCrossId(_nextEdgePointer.Value, out var edgeCrossId);
            _nextEdgePointer += size;

            this.EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
        }

        // get edge profile id.
        size = this.Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
        _nextEdgePointer += size;
        this.EdgeTypeId = edgeProfileId;

        // get length.
        size = this.Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
        _nextEdgePointer += size;
        this.Length = length;

        // get tail and head order.
        this.Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tailOrder, ref _headOrder);
        _nextEdgePointer++;

        // 
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
        _nextEdgePointer += size;
        size = this.Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

        if (vertex1.TileId == this.Tile.TileId &&
            vertex1.LocalId == _localId)
        {
            _nextEdgePointer = vp1;

            this.Head = vertex2;
            this.Forward = true;
        }
        else
        {
            _nextEdgePointer = vp2;

            this.Head = vertex1;
            this.Forward = false;

            (_headOrder, _tailOrder) = (_tailOrder, _headOrder);
        }

        return true;
    }

    /// <summary>
    /// Gets the shape of the given edge (not including vertex locations).
    /// </summary>
    public IEnumerable<(double longitude, double latitude, float? e)> Shape
    {
        get
        {
            if (this.Tile == null)
            {
                throw new InvalidOperationException("Move to graph tile first.");
            }

            if (!this.Forward)
            {
                return this.Tile.GetShape(_shapePointer).Reverse();
            }

            return this.Tile.GetShape(_shapePointer);
        }
    }

    /// <summary>
    /// Gets the attributes of the given edge.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes
    {
        get
        {
            if (this.Tile == null)
            {
                throw new InvalidOperationException("Move to graph tile first.");
            }

            return this.Tile.GetAttributes(_attributesPointer);
        }
    }

    /// <summary>
    /// Gets the first vertex.
    /// </summary>
    public VertexId Tail { get; private set; }

    private (double longitude, double latitude, float? e)? _tailLocation;

    /// <inheritdoc/>
    public (double longitude, double latitude, float? e) TailLocation
    {
        get
        {
            _tailLocation ??= this.GetVertex(this.Tail);

            return _tailLocation.Value;
        }
    }

    /// <summary>
    /// Gets the second vertex.
    /// </summary>
    public VertexId Head { get; private set; }

    private (double longitude, double latitude, float? e)? _headLocation;

    /// <inheritdoc/>
    public (double longitude, double latitude, float? e) HeadLocation
    {
        get
        {
            _headLocation ??= this.GetVertex(this.Head);

            return _headLocation.Value;
        }
    }

    /// <summary>
    /// Gets the local edge id.
    /// </summary>
    public EdgeId EdgeId { get; private set; }

    /// <summary>
    /// Gets the forward/backward flag.
    /// </summary>
    /// <remarks>
    /// When true the attributes can be interpreted normally, when false they represent the direction from tail -> head.
    /// </remarks>
    public bool Forward { get; private set; }

    /// <summary>
    /// Gets the edge profile id, if any.
    /// </summary>
    public uint? EdgeTypeId { get; private set; }

    /// <summary>
    /// Gets the length in centimeters, if any.
    /// </summary>
    public uint? Length { get; private set; }

    /// <summary>
    /// Gets the head index of this edge in the turn cost table.
    /// </summary>
    public byte? HeadOrder => _headOrder;

    /// <summary>
    /// Gets the tail index of this edge in the turn cost table.
    /// </summary>
    public byte? TailOrder => _tailOrder;

    /// <summary>
    /// Gets the turn cost at the tail turn (source -> [tail -> head]).
    /// </summary>
    /// <param name="sourceOrder">The order of the source edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToTail(
        byte sourceOrder)
    {
        if (this.Tile == null)
            return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

        var order = _tailOrder;
        return order == null
            ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
            : this.Tile.GetTurnCosts(this.Tail, sourceOrder, order.Value);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn ([head -> tail] -> target).
    /// </summary>
    /// <param name="targetOrder">The order of the target edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
        byte targetOrder)
    {
        if (this.Tile == null)
            return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

        var order = _tailOrder;
        return order == null
            ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
            : this.Tile.GetTurnCosts(this.Tail, order.Value, targetOrder);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn (source -> [head -> tail]).
    /// </summary>
    /// <param name="sourceOrder">The order of the source edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
        byte sourceOrder)
    {
        if (this.Tile == null)
            return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

        var order = _headOrder;
        return order == null
            ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
            : this.Tile.GetTurnCosts(this.Head, sourceOrder, order.Value);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn ([tail -> head] -> target).
    /// </summary>
    /// <param name="targetOrder">The order of the target edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
        byte targetOrder)
    {
        if (this.Tile == null)
            return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

        var order = _headOrder;
        return order == null
            ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
            : this.Tile.GetTurnCosts(this.Head, order.Value, targetOrder);
    }

    private (double longitude, double latitude, float? e) GetVertex(VertexId vertex)
    {
        if (this.Tile == null)
        {
            throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
        }

        if (!this.Tile.TryGetVertex(vertex, out var longitude, out var latitude, out var e))
        {
            throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
        }

        return (longitude, latitude, e);
    }
}
