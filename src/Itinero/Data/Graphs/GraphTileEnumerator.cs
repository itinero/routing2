namespace Itinero.Data.Graphs
{
    internal class GraphTileEnumerator
    {
        private GraphTile _graphTile;
        private uint _localId;
        private uint? _edgep;

        /// <summary>
        /// Gets the tile id.
        /// </summary>
        public uint TileId => _graphTile.TileId;

        /// <summary>
        /// Gets the local edge id.
        /// </summary>
        public uint EdgeId
        {
            get
            {
                if (_edgep == null) return uint.MaxValue;

                return _edgep.Value;
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
            if (vertex.LocalId >= _graphTile.VertexCount)
            {
                return false;
            }

            _localId = vertex.LocalId;
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
            // TODO: implement this!
            return false;
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
            
            _edgep = uint.MaxValue;

            return true;
        }

        /// <summary>
        /// Moves to the next edge for the current vertex.
        /// </summary>
        /// <returns>True when there is a new edge.</returns>
        public bool MoveNext()
        {
            if (_edgep == uint.MaxValue)
            {
                // move to the first edge.
                _edgep = _graphTile.VertexEdgePointer(_localId).ToNullable();
            }

            if (_edgep == null)
            {
                // no more data available.
                return false;
            }

            // decode edge data.
            var size = _graphTile.DecodeVertex(_edgep.FromNullable(), out var localId, out var tileId);
            this.Vertex1 = new VertexId(tileId, localId);
            _edgep += size;
            size = _graphTile.DecodeVertex(_edgep.FromNullable(), out localId, out tileId);
            this.Vertex2 = new VertexId(tileId, localId);
            _edgep += size;
            size = _graphTile.DecodePointer(_edgep.FromNullable(), out var vp1);
            _edgep += size;
            size = _graphTile.DecodePointer(_edgep.FromNullable(), out var vp2);

            if (this.Vertex1.TileId == _graphTile.TileId &&
                this.Vertex1.LocalId == _localId)
            {
                _edgep = vp1;
            }

            if (this.Vertex2.TileId == _graphTile.TileId &&
                this.Vertex2.LocalId == _localId)
            {
                _edgep = vp1;
            }

            return true;
        }

        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        public VertexId Vertex1 { get; set; }

        /// <summary>
        /// Gets the second vertex.
        /// </summary>
        public VertexId Vertex2 { get; set; }

        /// <summary>
        /// Gets the forward/backward flag.
        /// </summary>
        public bool Forward { get; set; }
    }
}