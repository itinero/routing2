using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Storage;

namespace Itinero.Network.Tiles
{
    internal class NetworkTileEnumerator
    {
        private uint _localId;
        private uint? _nextEdgePointer;
        private uint? _shapePointer;
        private uint? _attributesPointer;
        private byte? _tail;
        private byte? _head;

        /// <summary>
        /// Creates a new graph tile enumerator.
        /// </summary>
        internal NetworkTileEnumerator()
        {
            
        }
        
        internal NetworkTile? Tile { get; private set; }

        /// <summary>
        /// Gets the tile id.
        /// </summary>
        public uint TileId
        {
            get
            {
                if (Tile == null) return uint.MaxValue;
                
                return Tile.TileId;
            }
        }

        /// <summary>
        /// Moves to the given tile.
        /// </summary>
        /// <param name="graphTile">The graph tile to move to.</param>
        /// <returns>True if the move succeeds.</returns>
        public void MoveTo(NetworkTile graphTile)
        {
            Tile = graphTile;

            this.Reset();
        }

        /// <summary>
        /// Move to the vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the move succeeds and the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            if (Tile == null)
                throw new InvalidOperationException("Move to graph tile first.");
            
            if (vertex.LocalId >= Tile.VertexCount)
            {
                return false;
            }

            _localId = vertex.LocalId;
            _nextEdgePointer = uint.MaxValue;

            this.Vertex1 = vertex;
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
                throw new InvalidOperationException("Move to graph tile first.");
            if (this.TileId != edge.TileId) throw new ArgumentOutOfRangeException(nameof(edge), 
                "Cannot move to edge not in current tile, move to the tile first.");
        
            _nextEdgePointer = edge.LocalId;
            if (edge.LocalId >= EdgeId.MinCrossId)
            {
                _nextEdgePointer = Tile.GetEdgeCrossPointer(edge.LocalId - EdgeId.MinCrossId);
            }
            
            // decode edge data.
            this.EdgeId = edge;
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
                
                this.EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
            }
            
            // get edge profile id.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            this.EdgeTypeId = edgeProfileId;
            
            // get length.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            this.Length = length;
            
            // get tail and head order.
            Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tail, ref _head);
            _nextEdgePointer++;
            
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);
        
            if (forward)
            {
                this.Vertex1 = vertex1;
                this.Vertex2 = vertex2;
                this.Forward = true;
                
                _nextEdgePointer = vp1;
            }
            else
            {
                this.Vertex1 = vertex2;
                this.Vertex2 = vertex1;
                this.Forward = false;
                
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
        /// - returns false if there is no data in the current tile or if there is no tile selected.
        /// </remarks>
        public void Reset()
        {
            if (Tile == null) throw new InvalidOperationException("Cannot reset an empty enumerator.");
            
            _nextEdgePointer = uint.MaxValue;
        }

        public bool IsEmpty => Tile == null;

        /// <summary>
        /// Moves to the next edge for the current vertex.
        /// </summary>
        /// <returns>True when there is a new edge.</returns>
        public bool MoveNext()
        {
            if (Tile == null)
                throw new InvalidOperationException("Move to graph tile first.");
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
            this.EdgeId = new EdgeId(Tile.TileId, _nextEdgePointer.Value);
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
                
                this.EdgeId = new EdgeId(vertex1.TileId, edgeCrossId);
            }
            
            // get edge profile id.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            this.EdgeTypeId = edgeProfileId;
            
            // get length.
            size = Tile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            this.Length = length;
            
            // get tail and head order.
            Tile.GetTailHeadOrder(_nextEdgePointer.Value, ref _tail, ref _head);
            _nextEdgePointer++;
            
            // 
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = Tile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

            if (vertex1.TileId == Tile.TileId &&
                vertex1.LocalId == _localId)
            {
                _nextEdgePointer = vp1;

                this.Vertex2 = vertex2;
                this.Forward = true;
            }
            else
            {
                _nextEdgePointer = vp2;

                this.Vertex2 = vertex1;
                this.Forward = false;
            }

            return true;
        }

        /// <summary>
        /// Gets the shape of the given edge (not including vertex locations).
        /// </summary>
        public IEnumerable<(double longitude, double latitude)> Shape
        {
            get
            {
                if (Tile == null)
                    throw new InvalidOperationException("Move to graph tile first.");
                if (!this.Forward)
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
                    throw new InvalidOperationException("Move to graph tile first.");
                return Tile.GetAttributes(_attributesPointer);
            }
        }

        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        public VertexId Vertex1 { get; private set; }

        /// <summary>
        /// Gets the second vertex.
        /// </summary>
        public VertexId Vertex2 { get; private set; }

        /// <summary>
        /// Gets the local edge id.
        /// </summary>
        public EdgeId EdgeId { get; private set; }

        /// <summary>
        /// Gets the forward/backward flag.
        /// </summary>
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
        /// Gets the head index of this edge.
        /// </summary>
        public byte? Head => _head;

        /// <summary>
        /// Gets the tail index of this edge.
        /// </summary>
        public byte? Tail => _tail;

        /// <summary>
        /// Gets the turn cost to the current edge given the from order.
        /// </summary>
        /// <param name="fromOrder">The order of the source edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder)
        {
            if (Tile == null) return Enumerable.Empty<(uint turnCostType, uint cost)>();

            var order = this.Forward ? _tail : _head;
            if (order == null) return Enumerable.Empty<(uint turnCostType, uint cost)>();

            return Tile.GetTurnCosts(this.Vertex1, fromOrder, order.Value);
        }

        /// <summary>
        /// Gets the turn cost from the current edge given the to order.
        /// </summary>
        /// <param name="toOrder">The order of the target edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder)
        {
            if (Tile == null) return Enumerable.Empty<(uint turnCostType, uint cost)>();

            var order = this.Forward ? _head : _tail;
            if (order == null) return Enumerable.Empty<(uint turnCostType, uint cost)>();

            return Tile.GetTurnCosts(this.Vertex1, order.Value, toOrder);
        }
    }
}