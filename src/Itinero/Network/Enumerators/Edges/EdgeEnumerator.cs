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
        
        private (double longitude, double latitude) GetVertex(VertexId vertex)
        {
            var tile = _tileEnumerator.Tile;
            if (tile == null || tile.TileId != vertex.TileId)
            {
                tile = Network.GetTileForRead(vertex.TileId);
            }

            if (tile == null)
            {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            if (!tile.TryGetVertex(vertex, out var longitude, out var latitude))
            {
                throw new ArgumentOutOfRangeException(nameof(vertex), $"Vertex {vertex} not found!");
            }

            return (longitude, latitude);
        }

        /// <summary>
        /// Moves the enumerator to the first edge of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            if (_tileEnumerator.TileId == vertex.TileId) return _tileEnumerator.MoveTo(vertex);

            // move to the tile.
            var tile = Network.GetTileForRead(vertex.TileId);
            if (tile == null) return false;
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
            if (_tileEnumerator.TileId == edgeId.TileId) return _tileEnumerator.MoveTo(edgeId, forward);

            // move to the tile.
            var tile = Network.GetTileForRead(edgeId.TileId);
            if (tile == null) return false;
            _tileEnumerator.MoveTo(tile);

            return _tileEnumerator.MoveTo(edgeId, forward);
        }

        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        public void Reset()
        {
            _tileEnumerator.Reset();
        }

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        public bool MoveNext()
        {
            return _tileEnumerator.MoveNext();
        }

        public T Network { get; }

        /// <summary>
        /// Returns true if the edge is from -> to, false otherwise.
        /// </summary>
        public bool Forward => _tileEnumerator.Forward;

        private (double longitude, double latitude)? _fromLocation;

        public (double longitude, double latitude) FromLocation
        {
            get
            {
                _fromLocation ??= this.GetVertex(this.From);

                return _fromLocation.Value;
            }
        }

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        public VertexId From => _tileEnumerator.Vertex1;

        private (double longitude, double latitude)? _toLocation;

        public (double longitude, double latitude) ToLocation
        {
            get
            {
                _toLocation ??= this.GetVertex(this.To);

                return _toLocation.Value;
            }
        }

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        public VertexId To => _tileEnumerator.Vertex2;

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        public EdgeId Id => _tileEnumerator.EdgeId;

        /// <summary>
        /// Gets the shape.
        /// </summary>
        /// <returns>The shape.</returns>
        public IEnumerable<(double longitude, double latitude)> Shape => _tileEnumerator.Shape;

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
        public byte? Head => _tileEnumerator.Head;

        /// <summary>
        /// Gets the tail index.
        /// </summary>
        public byte? Tail => _tileEnumerator.Tail;

        /// <summary>
        /// Gets the turn cost to the current edge given the from order.
        /// </summary>
        /// <param name="fromOrder">The order of the source edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder) =>
            _tileEnumerator.GetTurnCostTo(fromOrder);

        /// <summary>
        /// Gets the turn cost from the current edge given the to order.
        /// </summary>
        /// <param name="toOrder">The order of the target edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder) =>
            _tileEnumerator.GetTurnCostFrom(toOrder);
    }
}