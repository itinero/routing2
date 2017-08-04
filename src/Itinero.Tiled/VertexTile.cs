using Reminiscence.Arrays;
using System.IO;
using System;
using Itinero;
using Itinero.Graphs.Geometric.Shapes;
using Itinero.Attributes;
using System.Collections.Generic;
using Itinero.LocalGeo;

namespace Itinero.Tiled
{
    /// <summary>
    /// Represents one individual tile.
    /// </summary>
    public class VertexTile
    {
        private const int BLOCK_SIZE = 1024;
        private const uint NO_DATA = uint.MaxValue;
        
        private const int MINIMUM_EDGE_SIZE = 4;
        private const int NODEA = 0;
        private const int NODEB = 1;
        private const int NEXTNODEA = 2;
        private const int NEXTNODEB = 3;
        //private const int DEFAULT_SIZE_ESTIMATE = 1024;
        private const int EDGE_SIZE = 6;
        private const uint MAX_VERTEX_ID = uint.MaxValue - 1;

        /// <summary>
        /// Holds the maximum profile count.
        /// </summary>
        public const ushort MAX_PROFILE_COUNT = (ushort)(1 << 14);

        /// <summary>
        /// Holds the maxium distance that can be stored on an edge.
        /// </summary>
        public const float MAX_DISTANCE = (((uint.MaxValue - 1) >> 14) / 10.0f);

        private readonly ArrayBase<uint> _vertices; // the base data: [ref-to-edges]
        private readonly ArrayBase<ulong> _externalVertices; // hold the external vertices.
        private readonly ArrayBase<float> _coordinates;
        private readonly ArrayBase<uint> _edges; // the edges data.
        private readonly ShapesArray _shapes;
        private readonly int _edgeDataSize = 2;

        /// <summary>
        /// Creates a new vertex tile.
        /// </summary>
        public VertexTile(uint zoom, uint id)
        {
            this.Id = id;
            this.Zoom = zoom;

            _vertices = new MemoryArray<uint>(0);
            _externalVertices = new MemoryArray<ulong>(0);
            _coordinates = new MemoryArray<float>(0);
            _edges = new MemoryArray<uint>(0);
            _shapes = new ShapesArray(0);
        }

        private uint _nextId = 0;
        private uint _nextExternalId = MAX_VERTEX_ID;
        private uint _nextEdgeId = 0;
        private long _edgeCount = 0;

        /// <summary>
        /// Gets or sets tile id.
        /// </summary>
        /// <returns></returns>
        public uint Id { get; private set; }

        /// <summary>
        /// Gets or sets tile zoom.
        /// </summary>
        /// <returns></returns>
        public uint Zoom { get; private set; }

        public AttributesIndex EdgeMeta { get; private set; } = new AttributesIndex(AttributesIndexMode.ReverseStringIndexKeysOnly);

        /// <summary>
        /// Adds a new vertex, returns a local id.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public uint AddVertex(float latitude, float longitude)
        {
            var id = _nextId;
            _nextId++;

            _vertices.EnsureMinimumSize(id + 1, Constants.NO_VERTEX);
            _coordinates.EnsureMinimumSize((id + 1) * 2);

            _vertices[id] = Constants.NO_EDGE;
            _coordinates[id * 2 + 0] = latitude;
            _coordinates[id * 2 + 1] = longitude;

            return id;
        }

        /// <summary>
        /// Gets the number of vertices.
        /// </summary>
        public uint VertexCount
        {
            get
            {
                return _nextId;
            }
        }

        /// <summary>
        /// Gets the vertex with the given local id.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public Coordinate GetVertex(uint vertex)
        {
            if (vertex * 2 + 1 < _coordinates.Length)
            {
                return new Coordinate()
                {
                    Latitude = _coordinates[vertex * 2],
                    Longitude = _coordinates[vertex * 2 + 1]
                };
            }
            throw new ArgumentOutOfRangeException("Vertex is not a valid id.");
        }

        /// <summary>
        /// Serializes edge data.
        /// </summary>
        /// <returns></returns>
        private static uint Serialize(float distance, ushort profile)
        {
            if (distance > MAX_DISTANCE)
            {
                throw new ArgumentOutOfRangeException("Cannot store distance on edge, too big.");
            }
            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException("Cannot store distance on edge, too small.");
            }
            if (profile >= MAX_PROFILE_COUNT)
            {
                throw new ArgumentOutOfRangeException("Cannot store profile id on edge, too big.");
            }

            var serDistance = (uint)(distance * 10) << 14;
            uint value = profile + serDistance;
            return value;
        }
        
        /// <summary>
        /// Adds a new edge.
        /// </summary>
        public void AddEdge(uint tileId1, uint vertex1, uint tileId2, uint vertex2, float distance, ushort profile, IAttributeCollection meta,
            IEnumerable<Coordinate> shape)
        {
            if (tileId1 != this.Id)
            {
                if (tileId2 != this.Id)
                {
                    throw new ArgumentException("At least one vertex has to be local.");
                }

                var globalId1 = BuildGlobalId(tileId1, vertex1);
                vertex1 = _nextExternalId;
                _nextExternalId--;
                var offset = MAX_VERTEX_ID - vertex1;
                _externalVertices.EnsureMinimumSize(offset + 1);
                _externalVertices[offset] = globalId1;
            }
            if (tileId2 != this.Id)
            {
                if (tileId1 != this.Id)
                {
                    throw new ArgumentException("At least one vertex has to be local.");
                }

                var globalId2 = BuildGlobalId(tileId2, vertex2);
                vertex2 = _nextExternalId;
                _nextExternalId--;
                var offset = MAX_VERTEX_ID - vertex2;
                _externalVertices.EnsureMinimumSize(offset + 1);
                _externalVertices[offset] = globalId2;
            }

            if (vertex1 == vertex2) { throw new ArgumentException("Given vertices must be different."); }

            var data0 = Serialize(distance, profile);
            var data1 = this.EdgeMeta.Add(meta);

            var reversed = false;
            if (vertex1 >= MAX_VERTEX_ID / 2)
            {
                var t = vertex1;
                vertex1 = vertex2;
                vertex2 = t;
                reversed = true;
            }
            
            var edgeId = _vertices[vertex1];
            if (_vertices[vertex1] != Constants.NO_EDGE)
            { // check for an existing edge first.
                // check if the arc exists already.
                edgeId = _vertices[vertex1];
                uint nextEdgeSlot = 0;
                while (edgeId != Constants.NO_EDGE)
                { // keep looping.
                    uint otherVertexId = 0;
                    uint previousEdgeId = edgeId;
                    //bool forward = true;
                    if (_edges[edgeId + NODEA] == vertex1)
                    {
                        otherVertexId = _edges[edgeId + NODEB];
                        nextEdgeSlot = edgeId + NEXTNODEA;
                        edgeId = _edges[edgeId + NEXTNODEA];
                    }
                    else
                    {
                        otherVertexId = _edges[edgeId + NODEA];
                        nextEdgeSlot = edgeId + NEXTNODEB;
                        edgeId = _edges[edgeId + NEXTNODEB];
                    }
                }

                // create a new edge.
                edgeId = _nextEdgeId;

                // there may be a need to increase edges array.
                _edges.EnsureMinimumSize(_nextEdgeId + MINIMUM_EDGE_SIZE + 2 + 1, Constants.NO_EDGE);
                if (!reversed)
                {
                    _edges[_nextEdgeId + NODEA] = vertex1;
                    _edges[_nextEdgeId + NODEB] = vertex2;
                }
                else
                {
                    _edges[_nextEdgeId + NODEA] = vertex2;
                    _edges[_nextEdgeId + NODEB] = vertex1;
                }
                _edges[_nextEdgeId + NEXTNODEA] = Constants.NO_EDGE;
                _edges[_nextEdgeId + NEXTNODEB] = Constants.NO_EDGE;
                _nextEdgeId = (uint)(_nextEdgeId + EDGE_SIZE);

                // append the new edge to the from list.
                _edges[nextEdgeSlot] = edgeId;

                // set data.
                _edges[edgeId + MINIMUM_EDGE_SIZE + 0] = data0;
                _edges[edgeId + MINIMUM_EDGE_SIZE + 1] = data1;
                _edgeCount++;
            }
            else
            { // create a new edge and set.
                edgeId = _nextEdgeId;
                _vertices[vertex1] = _nextEdgeId;

                // there may be a need to increase edges array.
                _edges.EnsureMinimumSize(_nextEdgeId + MINIMUM_EDGE_SIZE + 2 + 1, Constants.NO_EDGE);
                if (!reversed)
                {
                    _edges[_nextEdgeId + NODEA] = vertex1;
                    _edges[_nextEdgeId + NODEB] = vertex2;
                }
                else
                {
                    _edges[_nextEdgeId + NODEA] = vertex2;
                    _edges[_nextEdgeId + NODEB] = vertex1;
                }
                _edges[_nextEdgeId + NEXTNODEA] = Constants.NO_EDGE;
                _edges[_nextEdgeId + NEXTNODEB] = Constants.NO_EDGE;
                _nextEdgeId = (uint)(_nextEdgeId + EDGE_SIZE);

                // set data.
                _edges[edgeId + MINIMUM_EDGE_SIZE + 0] = data0;
                _edges[edgeId + MINIMUM_EDGE_SIZE + 1] = data1;
                _edgeCount++;
            }

            if (vertex2 < MAX_VERTEX_ID / 2)
            {
                var toEdgeId = _vertices[vertex2];
                if (toEdgeId != Constants.NO_EDGE)
                { // there are existing edges.
                    uint nextEdgeSlot = 0;
                    while (toEdgeId != Constants.NO_EDGE)
                    { // keep looping.
                        uint otherVertexId = 0;
                        if (_edges[toEdgeId + NODEA] == vertex2)
                        {
                            otherVertexId = _edges[toEdgeId + NODEB];
                            nextEdgeSlot = toEdgeId + NEXTNODEA;
                            toEdgeId = _edges[toEdgeId + NEXTNODEA];
                        }
                        else
                        {
                            otherVertexId = _edges[toEdgeId + NODEA];
                            nextEdgeSlot = toEdgeId + NEXTNODEB;
                            toEdgeId = _edges[toEdgeId + NEXTNODEB];
                        }
                    }
                    _edges[nextEdgeSlot] = edgeId;
                }
                else
                { // there are no existing edges point the vertex straight to it's first edge.
                    _vertices[vertex2] = edgeId;
                }
            }

            var stableEdgeId = (uint)(edgeId / EDGE_SIZE);

            if (shape != null)
            {
                _shapes.EnsureMinimumSize(stableEdgeId + 1);
                _shapes[stableEdgeId] = new ShapeEnumerable(shape);
            }
        }


        public EdgeEnumerator GetEdgeEnumerator(uint vertex)
        {
            var enumerator =  new EdgeEnumerator(this);
            enumerator.MoveTo(vertex);
            return enumerator;
        }

        /// <summary>
        /// Serializes this tile to the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public long Serialize(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes a tile from the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static VertexTile Deserialize(Stream stream, VertexTileProfile profile = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Builds a local id.
        /// </summary>
        /// <param name="tileId"></param>
        /// <param name="localId"></param>
        /// <returns></returns>
        public static ulong BuildGlobalId(uint tileId, uint localId)
        {
            return (ulong)localId + ((ulong)tileId << 32);
        }

        /// <summary>
        /// Extracts a local id.
        /// </summary>
        /// <param name="globalId"></param>
        /// <param name="tileId"></param>
        /// <param name="localId"></param>
        public static void ExtractLocalId(ulong globalId, out uint tileId, out uint localId)
        {
            localId = (uint)(globalId & 0xFFFFFFFF);
            tileId = (uint)((globalId >> 32) & 0xFFFFFFFF);
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
            public uint Id
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the vertex at the beginning of this edge.
            /// </summary>
            public uint From
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the vertex at the end of this edge.
            /// </summary>
            public uint To
            {
                get;
                private set;
            }
        }
        
        /// <summary>
        /// An edge enumerator.
        /// </summary>
        public class EdgeEnumerator : IEnumerable<Edge>, IEnumerator<Edge>
        {
            private readonly VertexTile _graph;
            private uint _nextEdgePointer;
            private uint _vertex;
            private uint _currentEdgePointer;
            private bool _currentEdgeInverted = false;
            private uint _startVertex;
            private uint _startEdgePointer;
            private uint _neighbour;
            private uint _neighbourTileId;

            /// <summary>
            /// Creates a new edge enumerator.
            /// </summary>
            internal EdgeEnumerator(VertexTile graph)
            {
                _graph = graph;
                _currentEdgePointer = Constants.NO_EDGE;
                _vertex = Constants.NO_EDGE;

                _startVertex = Constants.NO_EDGE;
                _startEdgePointer = Constants.NO_EDGE;
                _currentEdgeInverted = false;
                _neighbourTileId = _graph.Id;
            }

            /// <summary>
            /// Returns true if there is at least one edge.
            /// </summary>
            public bool HasData
            {
                get
                {
                    return _startVertex != Constants.NO_EDGE &&
                    _graph._vertices[_startVertex] != Constants.NO_EDGE;
                }
            }

            /// <summary>
            /// Move to the next edge.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (_nextEdgePointer != Constants.NO_EDGE)
                { // there is a next edge.
                    _currentEdgePointer = _nextEdgePointer;
                    _neighbour = 0; // reset neighbour.
                    if (_graph._edges[_nextEdgePointer + NODEA] == _vertex)
                    {
                        _neighbour = _graph._edges[_nextEdgePointer + NODEB];
                        _nextEdgePointer = _graph._edges[_nextEdgePointer + NEXTNODEA];
                        _currentEdgeInverted = false;
                    }
                    else
                    {
                        _neighbour = _graph._edges[_nextEdgePointer + NODEA];
                        _nextEdgePointer = _graph._edges[_nextEdgePointer + NEXTNODEB];
                        _currentEdgeInverted = true;
                    }

                    if (_neighbour > MAX_VERTEX_ID / 2)
                    {
                        var offset = MAX_VERTEX_ID - _neighbour;
                        var globalId = _graph._externalVertices[offset];
                        VertexTile.ExtractLocalId(globalId, out _neighbourTileId, out _neighbour);
                    }
                    else
                    {
                        _neighbourTileId = _graph.Id;
                    }

                    return true;
                }
                return false;
            }

            /// <summary>
            /// Returns the vertex at the beginning.
            /// </summary>
            public uint From
            {
                get
                {
                    return _vertex;
                }
            }

            /// <summary>
            /// Returns the vertex at the beginning.
            /// </summary>
            public ulong GlobalFrom
            {
                get
                {
                    return VertexTile.BuildGlobalId(_graph.Id, _vertex);
                }
            }

            /// <summary>
            /// Returns the vertex at the end.
            /// </summary>
            public uint To
            {
                get { return _neighbour; }
            }

            /// <summary>
            /// Gets the neighbour tile id.
            /// </summary>
            public uint ToTiledId
            {
                get { return _neighbourTileId; }
            }

            public ulong GlobalTo
            {
                get
                {
                    return VertexTile.BuildGlobalId(_neighbourTileId, _neighbour);
                }
            }

            /// <summary>
            /// Returns the edge data.
            /// </summary>
            public uint[] Data
            {
                get
                {
                    var data = new uint[_graph._edgeDataSize];
                    for (var i = 0; i < _graph._edgeDataSize; i++)
                    {
                        data[i] = _graph._edges[_currentEdgePointer + MINIMUM_EDGE_SIZE + i];
                    }
                    return data;
                }
            }

            /// <summary>
            /// Returns the first data element.
            /// </summary>
            public uint Data0
            {
                get
                {
                    return _graph._edges[_currentEdgePointer + MINIMUM_EDGE_SIZE + 0];
                }
            }

            /// <summary>
            /// Returns the second data element.
            /// </summary>
            public uint Data1
            {
                get
                {
                    return _graph._edges[_currentEdgePointer + MINIMUM_EDGE_SIZE + 1];
                }
            }

            /// <summary>
            /// Returns true if the edge data is inverted by default.
            /// </summary>
            public bool DataInverted
            {
                get { return _currentEdgeInverted; }
            }

            public VertexTile VertexTile
            {
                get
                {
                    return _graph;
                }
            }

            /// <summary>
            /// Gets the current edge id.
            /// </summary>
            public uint Id
            {
                get
                {
                    return (uint)(_currentEdgePointer / EDGE_SIZE);
                }
            }

            public ShapeBase Shape
            {
                get
                {
                    return _graph._shapes[this.Id];
                }
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            public void Reset()
            {
                _nextEdgePointer = _startEdgePointer;
                _currentEdgePointer = 0;
                _vertex = _startVertex;
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
            /// Moves this enumerator to the given vertex.
            /// </summary>
            public bool MoveTo(uint vertex)
            {
                var edgePointer = _graph._vertices[vertex];
                _nextEdgePointer = edgePointer;
                _currentEdgePointer = 0;
                _vertex = vertex;

                _startVertex = vertex;
                _startEdgePointer = edgePointer;
                _currentEdgeInverted = false;

                return edgePointer != Constants.NO_EDGE;
            }

            ///// <summary>
            ///// Moves this enumerator to the given edge.
            ///// </summary>
            //public void MoveToEdge(uint edge)
            //{
            //    var edgePointer = edge * (uint)_graph._edgeSize;

            //    _nextEdgePointer = edgePointer;
            //    _currentEdgePointer = _nextEdgePointer;

            //    _vertex = _graph._edges[_nextEdgePointer + NODEA];
            //    _neighbour = _graph._edges[_nextEdgePointer + NODEB];

            //    _startVertex = _vertex;
            //    _startEdgePointer = edgePointer;
            //    _currentEdgeInverted = false;
            //}

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