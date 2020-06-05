using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Graphs.Tiles
{
    internal class GraphTileEnumerator
    {
        private GraphTile? _graphTile;
        private uint _localId;
        private uint? _nextEdgePointer;
        private uint? _shapePointer;
        private uint? _attributesPointer;

        /// <summary>
        /// Creates a new graph tile enumerator.
        /// </summary>
        internal GraphTileEnumerator()
        {
            
        }

        /// <summary>
        /// Gets the tile id.
        /// </summary>
        public uint TileId
        {
            get
            {
                if (_graphTile == null) return uint.MaxValue;
                
                return _graphTile.TileId;
            }
        }

        /// <summary>
        /// Moves to the given tile.
        /// </summary>
        /// <param name="graphTile">The graph tile to move to.</param>
        /// <returns>True if the move succeeds.</returns>
        public void MoveTo(GraphTile graphTile)
        {
            _graphTile = graphTile;

            this.Reset();
        }

        /// <summary>
        /// Move to the vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the move succeeds and the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            if (_graphTile == null)
                throw new InvalidOperationException("Move to graph tile first.");
            
            if (vertex.LocalId >= _graphTile.VertexCount)
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
            if (_graphTile == null)
                throw new InvalidOperationException("Move to graph tile first.");
            if (this.TileId != edge.TileId) throw new ArgumentOutOfRangeException(nameof(edge), 
                "Cannot move to edge not in current tile, move to the tile first.");

            _nextEdgePointer = edge.LocalId;
            
            // decode edge data.
            this.EdgeId = edge;
            var size = _graphTile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
            var vertex1 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = _graphTile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
            var vertex2 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out var vp1);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out var vp2);
            _nextEdgePointer += size;

            var flipEdge = false;
            if (vertex1.TileId != vertex2.TileId)
            {
                size = _graphTile.DecodeEdgeId(_nextEdgePointer.Value, out var edgeId);
                _nextEdgePointer += size;

                if (edgeId != null)
                {
                    this.EdgeId = edgeId.Value;
                    flipEdge = true;
                }
            }
            
            // get edge profile id.
            size = _graphTile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            this.EdgeTypeId = edgeProfileId;
            
            // get length.
            size = _graphTile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            this.Length = length;
            
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

            if (forward)
            {
                this.Vertex1 = vertex1;
                this.Vertex2 = vertex2;
                this.Forward = !flipEdge;
                
                _nextEdgePointer = vp1;
            }
            else
            {
                this.Vertex1 = vertex2;
                this.Vertex2 = vertex1;
                this.Forward = flipEdge;
                
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
        public bool Reset()
        {
            if (_graphTile == null) return false;
            
            _nextEdgePointer = uint.MaxValue;

            return true;
        }

        /// <summary>
        /// Moves to the next edge for the current vertex.
        /// </summary>
        /// <returns>True when there is a new edge.</returns>
        public bool MoveNext()
        {
            if (_graphTile == null)
                throw new InvalidOperationException("Move to graph tile first.");
            if (_nextEdgePointer == uint.MaxValue)
            {
                // move to the first edge.
                _nextEdgePointer = _graphTile.VertexEdgePointer(_localId).DecodeNullableData();
            }

            if (_nextEdgePointer == null)
            {
                // no more data available.
                return false;
            }

            // decode edge data.
            this.EdgeId = new EdgeId(_graphTile.TileId, _nextEdgePointer.Value);
            var size = _graphTile.DecodeVertex(_nextEdgePointer.Value, out var localId, out var tileId);
            var vertex1 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = _graphTile.DecodeVertex(_nextEdgePointer.Value, out localId, out tileId);
            var vertex2 = new VertexId(tileId, localId);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out var vp1);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out var vp2);
            _nextEdgePointer += size;

            var flipEdge = false;
            if (vertex1.TileId != vertex2.TileId)
            {
                size = _graphTile.DecodeEdgeId(_nextEdgePointer.Value, out var edgeId);
                _nextEdgePointer += size;

                if (edgeId != null)
                {
                    this.EdgeId = edgeId.Value;
                    flipEdge = true;
                }
            }
            
            // get edge profile id.
            size = _graphTile.DecodeEdgePointerId(_nextEdgePointer.Value, out var edgeProfileId);
            _nextEdgePointer += size;
            this.EdgeTypeId = edgeProfileId;
            
            // get length.
            size = _graphTile.DecodeEdgePointerId(_nextEdgePointer.Value, out var length);
            _nextEdgePointer += size;
            this.Length = length;
            
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out _shapePointer);
            _nextEdgePointer += size;
            size = _graphTile.DecodePointer(_nextEdgePointer.Value, out _attributesPointer);

            if (vertex1.TileId == _graphTile.TileId &&
                vertex1.LocalId == _localId)
            {
                _nextEdgePointer = vp1;

                this.Vertex2 = vertex2;
                this.Forward = !flipEdge;
            }
            else
            {
                _nextEdgePointer = vp2;

                this.Vertex2 = vertex1;
                this.Forward = flipEdge;
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
                if (_graphTile == null)
                    throw new InvalidOperationException("Move to graph tile first.");
                if (!this.Forward)
                {
                    return _graphTile.GetShape(_shapePointer).Reverse();
                }
                return _graphTile.GetShape(_shapePointer);
            }
        }

        /// <summary>
        /// Gets the attributes of the given edge.
        /// </summary>
        public IEnumerable<(string key, string value)> Attributes
        {
            get
            {
                if (_graphTile == null)
                    throw new InvalidOperationException("Move to graph tile first.");
                return _graphTile.GetAttributes(_attributesPointer);
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
    }
}