using System;
using System.Collections.Generic;
using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Edges
{
    public abstract class EdgeEnumerator<T> :
        IEdgeEnumerator<T> where T : IEdgeEnumerable
    {
        private readonly NetworkTileEnumerator _tileEnumerator;

        internal EdgeEnumerator(T graph)
        {
            Network = graph;

            _tileEnumerator = new NetworkTileEnumerator();
        }

        private (double longitude, double latitude, float? e) GetVertex(VertexId vertex)
        {
            var tile = _tileEnumerator.Tile;
            if (tile == null || tile.TileId != vertex.TileId) {
                tile = Network.GetTileForRead(vertex.TileId);
            }

            if (tile == null) {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            if (!tile.TryGetVertex(vertex, out var longitude, out var latitude, out var e)) {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            return (longitude, latitude, e);
        }

        /// <summary>
        /// Moves the enumerator to the first edge of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            _tailLocation = null;
            _headLocation = null;

            if (_tileEnumerator.TileId == vertex.TileId) {
                return _tileEnumerator.MoveTo(vertex);
            }

            // move to the tile.
            var tile = Network.GetTileForRead(vertex.TileId);
            if (tile == null) {
                return false;
            }

            _tileEnumerator.MoveTo(tile);

            return _tileEnumerator.MoveTo(vertex);
        }

        /// <summary>
        /// Moves the enumerator to the given edge. 
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <param name="forward">The forward flag, when false the enumerator is in a state as it was enumerated to the edge via its last vertex. When true the enumerator is in a state as it was enumerated to the edge via its first vertex.</param>
        public bool MoveToEdge(EdgeId edgeId, bool forward = true)
        {
            _tailLocation = null;
            _headLocation = null;

            if (_tileEnumerator.TileId == edgeId.TileId) {
                return _tileEnumerator.MoveTo(edgeId, forward);
            }

            // move to the tile.
            var tile = Network.GetTileForRead(edgeId.TileId);
            if (tile == null) {
                return false;
            }

            _tileEnumerator.MoveTo(tile);

            return _tileEnumerator.MoveTo(edgeId, forward);
        }

        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        public void Reset()
        {
            _tailLocation = null;
            _headLocation = null;

            _tileEnumerator.Reset();
        }

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        public bool MoveNext()
        {
            _tailLocation = null;
            _headLocation = null;

            return _tileEnumerator.MoveNext();
        }

        /// <inheritdoc/>
        public T Network { get; }

        /// <summary>
        /// Returns true if the edge is from -> to, false otherwise.
        /// </summary>
        public bool Forward => _tileEnumerator.Forward;

        private (double longitude, double latitude, float? e)? _tailLocation;

        public (double longitude, double latitude, float? e) TailLocation {
            get {
                _tailLocation ??= GetVertex(Tail);

                return _tailLocation.Value;
            }
        }

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        public VertexId Tail => _tileEnumerator.Tail;

        private (double longitude, double latitude, float? e)? _headLocation;

        public (double longitude, double latitude, float? e) HeadLocation {
            get {
                _headLocation ??= GetVertex(Head);

                return _headLocation.Value;
            }
        }

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        public VertexId Head => _tileEnumerator.Head;

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        public EdgeId EdgeId => _tileEnumerator.EdgeId;

        /// <summary>
        /// Gets the shape.
        /// </summary>
        /// <returns>The shape.</returns>
        public IEnumerable<(double longitude, double latitude, float? e)> Shape => _tileEnumerator.Shape;

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns>The attributes.</returns>
        public IEnumerable<(string key, string value)> Attributes => _tileEnumerator.Attributes;

        /// <summary>
        /// Gets the edge profile id.
        /// </summary>
        public uint? EdgeTypeId => _tileEnumerator.EdgeTypeId;

        /// <summary>
        /// Gets the length in centimeters, if any.
        /// </summary>
        public uint? Length => _tileEnumerator.Length;

        /// <summary>
        /// Gets the head index.
        /// </summary>
        public byte? HeadOrder => _tileEnumerator.HeadOrder;

        /// <summary>
        /// Gets the tail index.
        /// </summary>
        public byte? TailOrder => _tileEnumerator.TailOrder;
        
        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [tail -> head]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToTail(
            byte sourceOrder)
        {
            return _tileEnumerator.GetTurnCostToTail(sourceOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([head -> tail] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
            byte targetOrder)
        {
            return _tileEnumerator.GetTurnCostFromTail(targetOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [head -> tail]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
            byte sourceOrder)
        {
            return _tileEnumerator.GetTurnCostToHead(sourceOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([tail -> head] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
            byte targetOrder)
        {
            return _tileEnumerator.GetTurnCostFromHead(targetOrder);
        }
    }
}