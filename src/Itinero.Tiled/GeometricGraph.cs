using System.Collections.Generic;
using Reminiscence;
using Itinero.Attributes;
using Itinero.LocalGeo;
using System;
using System.Collections;

namespace Itinero.Tiled
{
    /// <summary>
    /// A geometric graph.
    /// </summary>
    public class GeometricGraph
    {
        private readonly Dictionary<uint, VertexTile> _tiles;
        private readonly uint _zoom;

        /// <summary>
        /// Creates a new geometric graph.
        /// </summary>
        public GeometricGraph(uint zoom = 15)
        {
            _zoom = zoom;
            _tiles = new Dictionary<uint, VertexTile>();
        }

        public IEnumerable<uint> TileIds
        {
            get
            {
                return _tiles.Keys;
            }
        }

        public uint Zoom
        {
            get
            {
                return _zoom;
            }
        }

        public VertexTile GetTile(uint tileId)
        {
            return _tiles[tileId];
        }
            

        /// <summary>
        /// Adds a new vertex.
        /// </summary>
        public ulong AddVertex(float latitude, float longitude)
        {
            var tileId = (uint)Tiles.Tile.WorldToTileIndex(latitude, longitude, _zoom).LocalId;

            VertexTile tile;
            if (!_tiles.TryGetValue(tileId, out tile))
            {
                tile = new VertexTile(_zoom, tileId);
                _tiles[tileId] = tile;
            }

            var localId = tile.AddVertex(latitude, longitude);
            
            return VertexTile.BuildGlobalId(tileId, localId);            
        }

        public Coordinate GetVertex(ulong vertex)
        {
            uint tileId, localId;
            VertexTile.ExtractLocalId(vertex, out tileId, out localId);

            var tile = _tiles[tileId];
            return tile.GetVertex(localId);
        }

        public EdgeEnumerator GetEdgeEnumerator(ulong vertex)
        {
            uint tileId, localId;
            VertexTile.ExtractLocalId(vertex, out tileId, out localId);

            var tile = _tiles[tileId];
            return new EdgeEnumerator(tileId, tile.GetEdgeEnumerator(localId));
        }

        /// <summary>
        /// Adds a new edge.
        /// </summary>
        public void AddEdge(ulong vertex1, ulong vertex2, float distance, ushort profile, IAttributeCollection meta,
            IEnumerable<Coordinate> shape)
        {
            uint tileId1, tileId2, localId1, localId2;
            VertexTile.ExtractLocalId(vertex1, out tileId1, out localId1);
            VertexTile.ExtractLocalId(vertex2, out tileId2, out localId2);

            var tile = _tiles[tileId1];
            tile.AddEdge(tileId1, localId1, tileId2, localId2, distance, profile, meta, shape);

            if (tileId1 != tileId2)
            {
                tile = _tiles[tileId2];
                tile.AddEdge(tileId1, localId1, tileId2, localId2, distance, profile, meta, shape);
            }
        }

        public VertexEnumerator GetVertexEnumerator()
        {
            return new VertexEnumerator(this);
        }

        public class VertexEnumerator : IEnumerator<ulong>
        {
            private readonly GeometricGraph _graph;

            public VertexEnumerator(GeometricGraph graph)
            {
                _graph = graph;
            }

            private IEnumerator<uint> _tiles = null;
            private VertexTile _tile = null;
            private uint _vertex = Constants.NO_VERTEX;

            public ulong Current => VertexTile.BuildGlobalId(_tile.Id, _vertex);

            object IEnumerator.Current => this._vertex;

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                if (_tiles == null)
                {
                    _tiles = _graph.TileIds.GetEnumerator();
                }
                if (_vertex == Constants.NO_VERTEX)
                {
                    while (_tiles.MoveNext())
                    {
                        _tile = _graph.GetTile(_tiles.Current);
                        if (_tile.VertexCount > 0)
                        {
                            _vertex = 0;
                            return true;
                        }
                    }
                    if (_vertex == Constants.NO_VERTEX)
                    {
                        return false;
                    }
                }
                _vertex++;
                if (_vertex >= _tile.VertexCount)
                {
                    _vertex = Constants.NO_VERTEX;
                    return this.MoveNext();
                }
                return true;
            }

            public void Reset()
            {
                _tiles = null;
            }
        }

        public class Edge
        {
            /// <summary>
            /// Creates a new edge keeping the current state of the given enumerator.
            /// </summary>
            internal Edge(EdgeEnumerator enumerator)
            {
                this.Id = enumerator.Id;
                this.To = enumerator.To;
                this.From = enumerator.From;
            }

            /// <summary>
            /// Gets the edge id.
            /// </summary>
            public ulong Id
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the vertex at the beginning of this edge.
            /// </summary>
            public ulong From
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the vertex at the end of this edge.
            /// </summary>
            public ulong To
            {
                get;
                private set;
            }
        }

        public class EdgeEnumerator : IEnumerable<Edge>, IEnumerator<Edge>
        {
            private readonly uint _tileId;
            private readonly VertexTile.EdgeEnumerator _enumerator;

            public EdgeEnumerator(uint tileId, VertexTile.EdgeEnumerator enumerator)
            {
                _enumerator = enumerator;
                _tileId = tileId;
            }

            /// <summary>
            /// Returns the id.
            /// </summary>
            public ulong Id
            {
                get
                {
                    return VertexTile.BuildGlobalId(_tileId, _enumerator.Id);
                }
            }

            public long IdDirected
            {
                get
                {
                    var directedId = (long)(this.Id + 1);
                    if (this.DataInverted)
                    {
                        return -directedId;
                    }
                    return directedId;
                }
            }

            /// <summary>
            /// Returns the vertex at the beginning.
            /// </summary>
            public ulong From
            {
                get
                {
                    return VertexTile.BuildGlobalId(_tileId, _enumerator.From);
                }
            }

            /// <summary>
            /// Returns the vertex at the end.
            /// </summary>
            public ulong To
            {
                get
                {
                    return VertexTile.BuildGlobalId(_tileId, _enumerator.To);
                }
            }

            /// <summary>
            /// Returns the edge data.
            /// </summary>
            public uint[] Data
            {
                get
                {
                    return _enumerator.Data;
                }
            }

            /// <summary>
            /// Returns the first data element.
            /// </summary>
            public uint Data0
            {
                get
                {
                    return _enumerator.Data0;
                }
            }

            /// <summary>
            /// Returns the second data element.
            /// </summary>
            public uint Data1
            {
                get
                {
                    return _enumerator.Data1;
                }
            }

            /// <summary>
            /// Returns true if the edge data is inverted by default.
            /// </summary>
            public bool DataInverted
            {
                get { return _enumerator.DataInverted; }
            }

            /// <summary>
            /// Move to the next edge.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            public void Reset()
            {
                _enumerator.Reset();
            }

            /// <summary>
            /// Gets the enumerator.
            /// </summary>
            /// <returns></returns>
            public IEnumerator<Edge> GetEnumerator()
            {
                this.Reset();
                return this;
            }

            /// <summary>
            /// Gets the current edge.
            /// </summary>
            public Edge Current
            {
                get { return new Edge(this); }
            }

            /// <summary>
            /// Disposes this enumerator.
            /// </summary>
            public void Dispose()
            {

            }

            /// <summary>
            /// Gets the current edge.
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Gets the enumerator.
            /// </summary>
            /// <returns></returns>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}