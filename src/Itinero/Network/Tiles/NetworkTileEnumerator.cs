using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Storage;
using Itinero.Network.Tiles.Standalone;

namespace Itinero.Network.Tiles
{
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
        public uint TileId => Tile?.TileId ?? uint.MaxValue;

        /// <summary>
        /// Moves to the given tile.
        /// </summary>
        /// <param name="graphTile">The graph tile to move to.</param>
        /// <returns>True if the move succeeds.</returns>
        public void MoveTo(NetworkTile graphTile)
        {
            Tile = graphTile;

            Reset();
        }

        /// <summary>
        /// Move to the vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the move succeeds and the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            if (Tile == null)
            {
                throw new InvalidOperationException("Move to graph tile first.");
            }

            if (vertex.LocalId >= Tile.VertexCount)
            {
                return false;
            }

            _localId = vertex.LocalId;
            _nextEdgePointer = uint.MaxValue;
            this.EdgePointer = uint.MaxValue;

            Tail = vertex;
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
            if (Tile == null)
            {
                throw new InvalidOperationException("Move to graph tile first.");
            }

            if (TileId != edge.TileId)
            {
                throw new ArgumentOutOfRangeException(nameof(edge),
                    "Cannot move to edge not in current tile, move to the tile first.");
            }

            _nextEdgePointer = edge.LocalId;
            if (edge.LocalId >= EdgeId.MinCrossId)
            {
                _nextEdgePointer = Tile.GetEdgeCrossPointer(edge.LocalId - EdgeId.MinCrossId);
            }
            this.EdgePointer = _nextEdgePointer.Value;

            // decode edge data.
            EdgeId = edge;
            var size = Tile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
            var vertex1 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = Tile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
            var vertex2 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out var vp1);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out var vp2);
            _nextEdgePointer += size;

            if (vertex1.TileId != vertex2.TileId)
            {
                size = Tile.DecodeEdgeCrossId(_nextEdgePointer.Value, out var edgeCrossId);
                _nextEdgePointer += size;

                EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
            }

            // get edge profile id.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            EdgeTypeId = edgeProfileId;

            // get length.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            Length = length;

            // get tail and head order.
            Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tailOrder, ref _headOrder);
            _nextEdgePointer++;

            size = Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

            if (forward)
            {
                Tail = vertex1;
                Head = vertex2;
                Forward = true;

                _nextEdgePointer = vp1;
            }
            else
            {
                Tail = vertex2;
                Head = vertex1;
                Forward = false;

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
            if (Tile == null)
            {
                throw new InvalidOperationException("Cannot reset an empty enumerator.");
            }

            this.EdgePointer = uint.MaxValue;
            _nextEdgePointer = uint.MaxValue;
        }

        public bool IsEmpty => Tile == null;

        internal uint EdgePointer { get; private set; } = uint.MaxValue;

        /// <summary>
        /// Moves to the next edge for the current vertex.
        /// </summary>
        /// <returns>True when there is a new edge.</returns>
        public bool MoveNext()
        {
            this.EdgePointer = uint.MaxValue;

            if (Tile == null)
            {
                throw new InvalidOperationException("Move to graph tile first.");
            }

            if (_nextEdgePointer == uint.MaxValue)
            {
                // move to the first edge.
                _nextEdgePointer = Tile.VertexEdgePointer(_localId).DecodeNullableData();
            }

            if (_nextEdgePointer == null)
            {
                // no more data available.
                return false;
            }

            // decode edge data.
            this.EdgePointer = _nextEdgePointer.Value;
            EdgeId = new EdgeId(Tile.TileId, _nextEdgePointer.Value);
            var size = Tile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
            var vertex1 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = Tile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
            var vertex2 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out var vp1);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out var vp2);
            _nextEdgePointer += size;

            if (vertex1.TileId != vertex2.TileId)
            {
                size = Tile.DecodeEdgeCrossId(_nextEdgePointer.Value, out var edgeCrossId);
                _nextEdgePointer += size;

                EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
            }

            // get edge profile id.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            EdgeTypeId = edgeProfileId;

            // get length.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            Length = length;

            // get tail and head order.
            Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tailOrder, ref _headOrder);
            _nextEdgePointer++;

            // 
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

            if (vertex1.TileId == Tile.TileId &&
                vertex1.LocalId == _localId)
            {
                _nextEdgePointer = vp1;

                Head = vertex2;
                Forward = true;
            }
            else
            {
                _nextEdgePointer = vp2;

                Head = vertex1;
                Forward = false;

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
                if (Tile == null)
                {
                    throw new InvalidOperationException("Move to graph tile first.");
                }

                if (!Forward)
                {
                    return Tile.GetShape(_shapePointer).Reverse();
                }

                return Tile.GetShape(_shapePointer);
            }
        }

        /// <summary>
        /// Gets the attributes of the given edge.
        /// </summary>
        public IEnumerable<(string key, string value)> Attributes
        {
            get
            {
                if (Tile == null)
                {
                    throw new InvalidOperationException("Move to graph tile first.");
                }

                return Tile.GetAttributes(_attributesPointer);
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
                _tailLocation ??= GetVertex(Tail);

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
                _headLocation ??= GetVertex(Head);

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
            if (Tile == null)
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

            var order = _tailOrder;
            return order == null
                ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
                : Tile.GetTurnCosts(Tail, sourceOrder, order.Value);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([head -> tail] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
            byte targetOrder)
        {
            if (Tile == null)
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

            var order = _tailOrder;
            return order == null
                ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
                : Tile.GetTurnCosts(Tail, order.Value, targetOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [head -> tail]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
            byte sourceOrder)
        {
            if (Tile == null)
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

            var order = _headOrder;
            return order == null
                ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
                : Tile.GetTurnCosts(Head, sourceOrder, order.Value);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([tail -> head] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
            byte targetOrder)
        {
            if (Tile == null)
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;

            var order = _headOrder;
            return order == null
                ? ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty
                : Tile.GetTurnCosts(Head, order.Value, targetOrder);
        }

        private (double longitude, double latitude, float? e) GetVertex(VertexId vertex)
        {
            if (Tile == null)
            {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            if (!Tile.TryGetVertex(vertex, out var longitude, out var latitude, out var e))
            {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            return (longitude, latitude, e);
        }
    }
}
