using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;
using Itinero.Network.Storage;
using Itinero.Network.TurnCosts;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles {
    internal partial class NetworkTile {
        private const int DefaultSizeIncrease = 16;

        private readonly uint _tileId;
        private readonly int _zoom; // the zoom level.
        private readonly int _edgeTypeMapId; // the edge type index id.

        private uint _nextVertexId = 0; // the next vertex id.

        // the pointers, per vertex, to their first edge.
        // TODO: investigate if it's worth storing these with less precision, one tile will never contain this much data.
        // TODO: investigate if we can not use one-increasing vertex ids but also use their pointers like with the edges.
        private readonly ArrayBase<uint> _pointers;
        private uint _nextCrossTileId; // the next id for an edge that crosses tile boundaries.
        private readonly ArrayBase<uint> _crossEdgePointers; // points to the cross tile boundary edges.

        // the next edge id.
        private uint _nextEdgeId = 0;

        // the edges.
        private readonly ArrayBase<byte> _edges;

        /// <summary>
        /// Creates a new tile.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        /// <param name="tileId">The tile id.</param>
        /// <param name="edgeTypeMapId">The edge type index id.</param>
        public NetworkTile(int zoom, uint tileId, int edgeTypeMapId = 0) {
            _zoom = zoom;
            _tileId = tileId;
            _edgeTypeMapId = 0;
            _nextCrossTileId = 0;

            _pointers = new MemoryArray<uint>(0);
            _edges = new MemoryArray<byte>(0);
            _crossEdgePointers = new MemoryArray<uint>(0);

            _coordinates = new MemoryArray<byte>(0);
            _shapes = new MemoryArray<byte>(0);
            _attributes = new MemoryArray<byte>(0);
            _strings = new MemoryArray<string>(0);
        }

        private NetworkTile(int zoom, uint tileId, int edgeTypeMapId, uint nextCrossTileId, ArrayBase<uint> pointers,
            ArrayBase<byte> edges,
            ArrayBase<uint> crossEdgePointers, ArrayBase<byte> coordinates, ArrayBase<byte> shapes,
            ArrayBase<byte> attributes,
            ArrayBase<string> strings, ArrayBase<byte> turnCosts, uint nextVertexId, uint nextEdgeId,
            uint nextAttributePointer,
            uint nextShapePointer, uint nextStringId) {
            _zoom = zoom;
            _tileId = tileId;
            _edgeTypeMapId = edgeTypeMapId;
            _nextCrossTileId = nextCrossTileId;
            _pointers = pointers;
            _edges = edges;
            _crossEdgePointers = crossEdgePointers;

            _coordinates = coordinates;
            _shapes = shapes;
            _attributes = attributes;
            _strings = strings;
            _turnCosts = turnCosts;

            _nextVertexId = nextVertexId;
            _nextEdgeId = nextEdgeId;
            _nextAttributePointer = nextAttributePointer;
            _nextShapePointer = nextShapePointer;
            _nextStringId = nextStringId;
        }

        /// <summary>
        /// Clones this graph tile.
        /// </summary>
        /// <returns>The copy of this tile.</returns>
        public NetworkTile Clone() {
            return new(_zoom, _tileId, _edgeTypeMapId, _nextCrossTileId, _pointers.Clone(), _edges.Clone(),
                _crossEdgePointers.Clone(),
                _coordinates.Clone(), _shapes.Clone(), _attributes.Clone(), _strings.Clone(), _turnCosts.Clone(),
                _nextVertexId,
                _nextEdgeId, _nextAttributePointer, _nextShapePointer, _nextStringId);
        }

        /// <summary>
        /// Gets the tile id.
        /// </summary>
        public uint TileId => _tileId;

        /// <summary>
        /// Gets the number of vertices.
        /// </summary>
        public uint VertexCount => _nextVertexId;

        /// <summary>
        /// Gets the edge type map id.
        /// </summary>
        public int EdgeTypeMapId => _edgeTypeMapId;

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="e">The elevation in meters.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude, float? e = null) {
            // set coordinate.
            SetCoordinate(_nextVertexId, longitude, latitude, e);

            // create id.
            var vertexId = new VertexId(_tileId, _nextVertexId);
            _nextVertexId++;

            // make room for edges.
            _pointers.EnsureMinimumSize(vertexId.LocalId, DefaultSizeIncrease);

            return vertexId;
        }

        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="elevation">The elevation.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude, out float? elevation) {
            longitude = default;
            latitude = default;
            elevation = null;
            if (vertex.LocalId >= _nextVertexId) {
                return false;
            }

            GetCoordinate(vertex.LocalId, out longitude, out latitude, out elevation);
            return true;
        }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="shape">The shape."</param>
        /// <param name="attributes">The attributes."</param>
        /// <param name="edgeId">The edge id if this edge is a part of another tile.</param>
        /// <param name="edgeTypeId">The edge type id, if any.</param>
        /// <param name="length">The length in centimeters.</param>
        /// <returns>The new edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null, EdgeId? edgeId = null, uint? edgeTypeId = null,
            uint? length = null) {
            if (vertex2.TileId != _tileId) {
                // this edge crosses tiles boundaries, it need special treatment and a stable id.
                // because the edge originates in this tile, this tile is responsible for generating the id.
                if (edgeId != null) {
                    throw new ArgumentException(
                        "The edge id shouldn't be a given, it should be generated by the originating tile.",
                        nameof(edgeId));
                }

                if (vertex1.TileId != _tileId) {
                    throw new ArgumentException("None of the two vertices in this edge are in this tile.",
                        nameof(vertex1));
                }

                // generate a new cross tile id and store pointer to edge.
                edgeId = new EdgeId(_tileId, EdgeId.MinCrossId + _nextCrossTileId);
                _crossEdgePointers.EnsureMinimumSize(_nextCrossTileId + 1);
                _crossEdgePointers[_nextCrossTileId] = _nextEdgeId;
                _nextCrossTileId++;
            }
            else if (vertex1.TileId != _tileId) {
                // this edge crosses tiles boundaries, it need special treatment and a stable id.
                // because the edge originates in another tile it should already have an id.
                if (edgeId == null) {
                    throw new ArgumentException(
                        "Cannot add an edge that doesn't start in this tile without a proper edge id.",
                        nameof(edgeId));
                }

                if (edgeId.Value.TileId != vertex1.TileId) {
                    throw new ArgumentException("The edge id doesn't match the tile id in the second vertex.",
                        nameof(edgeId));
                }
            }
            else {
                // this edge starts in this tile, it get an id from this tile.
                edgeId = new EdgeId(_tileId, _nextEdgeId);
            }

            // write the edge data.
            var newEdgePointer = _nextEdgeId;
            var size = EncodeVertex(_edges, _tileId, _nextEdgeId, vertex1);
            _nextEdgeId += size;
            size = EncodeVertex(_edges, _tileId, _nextEdgeId, vertex2);
            _nextEdgeId += size;

            // get previous pointers if vertex already has edges
            // set the new pointers.
            uint? v1p = null;
            if (vertex1.TileId == _tileId) {
                v1p = _pointers[vertex1.LocalId].DecodeNullableData();
                _pointers[vertex1.LocalId] = newEdgePointer.EncodeToNullableData();
            }

            uint? v2p = null;
            if (vertex2.TileId == _tileId) {
                v2p = _pointers[vertex2.LocalId].DecodeNullableData();
                _pointers[vertex2.LocalId] = newEdgePointer.EncodeToNullableData();
            }

            // set next pointers.
            size = EncodePointer(_edges, _nextEdgeId, v1p);
            _nextEdgeId += size;
            size = EncodePointer(_edges, _nextEdgeId, v2p);
            _nextEdgeId += size;

            // write edge id explicitly if not in this edge.
            if (vertex1.TileId != vertex2.TileId) { // this data will only be there for edges crossing tile boundaries.
                _nextEdgeId += (uint) _edges.SetDynamicUInt32(_nextEdgeId,
                    edgeId.Value.LocalId - EdgeId.MinCrossId);
            }

            // write edge profile id.
            _nextEdgeId += SetDynamicUIn32Nullable(_edges, _nextEdgeId, edgeTypeId);

            // write length.
            _nextEdgeId += SetDynamicUIn32Nullable(_edges, _nextEdgeId, length);

            // set tail and head order.
            _edges.SetTailHeadOrder(_nextEdgeId, null, null);
            _nextEdgeId++;

            // take care of shape if any.
            uint? shapePointer = null;
            if (shape != null) {
                shapePointer = SetShape(shape);
            }

            size = EncodePointer(_edges, _nextEdgeId, shapePointer);
            _nextEdgeId += size;

            // take care of attributes if any.
            uint? attributesPointer = null;
            if (attributes != null) {
                attributesPointer = SetAttributes(attributes);
            }

            size = EncodePointer(_edges, _nextEdgeId, attributesPointer);
            _nextEdgeId += size;

            return edgeId.Value;
        }

        internal NetworkTile CloneForEdgeTypeMap(
            (int id, Func<IEnumerable<(string key, string value)>, uint> func) edgeTypeMap) {
            var edges = new MemoryArray<byte>(_edges.Length);
            var pointers = new MemoryArray<uint>(_pointers.Length);
            var crossEdgePointers = new MemoryArray<uint>(_crossEdgePointers.Length);
            var nextEdgeId = _nextEdgeId;
            var p = 0U;
            var newP = 0U;
            while (p < nextEdgeId) {
                // read edge data.
                p += DecodeVertex(p, out var local1Id, out var tile1Id);
                var vertex1 = new VertexId(tile1Id, local1Id);
                p += DecodeVertex(p, out var local2Id, out var tile2Id);
                var vertex2 = new VertexId(tile2Id, local2Id);
                p += DecodePointer(p, out _);
                p += DecodePointer(p, out _);
                uint? crossEdgeId = null;
                if (tile1Id != tile2Id) {
                    p += (uint) _edges.GetDynamicUInt32(p, out var c);
                    crossEdgeId = c;
                }

                p += (uint) _edges.GetDynamicUInt32Nullable(p, out var _);
                p += (uint) _edges.GetDynamicUInt32Nullable(p, out var length);
                var tailHeadOrder = _edges[p];
                p++;
                p += DecodePointer(p, out var shapePointer);
                p += DecodePointer(p, out var attributePointer);

                // generate new edge type id.
                var newEdgeTypeId = edgeTypeMap.func(GetAttributes(attributePointer));

                // write edge data again.
                var newEdgePointer = newP;
                newP += EncodeVertex(edges, _tileId, newP, vertex1);
                newP += EncodeVertex(edges, _tileId, newP, vertex2);
                uint? v1p = null;
                if (vertex1.TileId == _tileId) {
                    v1p = pointers[vertex1.LocalId].DecodeNullableData();
                    pointers[vertex1.LocalId] = newEdgePointer.EncodeToNullableData();
                }

                uint? v2p = null;
                if (vertex2.TileId == _tileId) {
                    v2p = pointers[vertex2.LocalId].DecodeNullableData();
                    pointers[vertex2.LocalId] = newEdgePointer.EncodeToNullableData();
                }

                newP += EncodePointer(edges, newP, v1p);
                newP += EncodePointer(edges, newP, v2p);
                if (crossEdgeId != null) {
                    newP += (uint) edges.SetDynamicUInt32(newP, crossEdgeId.Value);
                    if (vertex1.TileId == _tileId) {
                        crossEdgePointers[crossEdgeId.Value] = newEdgePointer;
                    }
                }

                newP += (uint) edges.SetDynamicUInt32Nullable(newP, newEdgeTypeId);
                newP += (uint) edges.SetDynamicUInt32Nullable(newP, length);
                edges[newP] = tailHeadOrder;
                newP++;
                newP += EncodePointer(edges, newP, shapePointer);
                newP += EncodePointer(edges, newP, attributePointer);
            }

            return new NetworkTile(_zoom, _tileId, edgeTypeMap.id, _nextCrossTileId, pointers, edges, crossEdgePointers,
                _coordinates,
                _shapes, _attributes, _strings, _turnCosts, _nextVertexId, _nextEdgeId,
                _nextAttributePointer, _nextShapePointer, _nextStringId);
        }

        internal uint VertexEdgePointer(uint vertex) {
            return _pointers[vertex];
        }

        internal static uint EncodeVertex(ArrayBase<byte> edges, uint localTileId, uint location, VertexId vertexId) {
            if (vertexId.TileId == localTileId) { // same tile, only store local id.
                if (edges.Length <= location + 5) {
                    edges.Resize(edges.Length + DefaultSizeIncrease);
                }

                return (uint) edges.SetDynamicUInt32(location, vertexId.LocalId);
            }

            // other tile, store full id.
            if (edges.Length <= location + 10) {
                edges.Resize(edges.Length + DefaultSizeIncrease);
            }

            var encodedId = vertexId.Encode();
            return (uint) edges.SetDynamicUInt64(location, encodedId);
        }

        internal uint DecodeVertex(uint location, out uint localId, out uint tileId) {
            var size = (uint) _edges.GetDynamicUInt64(location, out var encodedId);
            if (encodedId < uint.MaxValue) {
                localId = (uint) encodedId;
                tileId = _tileId;
                return size;
            }

            VertexId.Decode(encodedId, out tileId, out localId);
            return size;
        }

        internal uint DecodeEdgeCrossId(uint location, out uint edgeCrossId) {
            var s = _edges.GetDynamicUInt32(location, out var c);
            edgeCrossId = EdgeId.MinCrossId + c;
            return (uint) s;
        }

        internal uint GetEdgeCrossPointer(uint edgeCrossId) {
            return _crossEdgePointers[edgeCrossId];
        }

        internal static uint EncodePointer(ArrayBase<byte> edges, uint location, uint? pointer) {
            // TODO: save the diff instead of the full pointer.
            if (edges.Length <= location + 5) {
                edges.Resize(edges.Length + DefaultSizeIncrease);
            }

            return (uint) edges.SetDynamicUInt32(location,
                pointer.EncodeAsNullableData());
        }

        internal uint DecodePointer(uint location, out uint? pointer) {
            var size = _edges.GetDynamicUInt32(location, out var data);
            pointer = data.DecodeNullableData();
            return (uint) size;
        }

        internal static uint SetDynamicUIn32Nullable(ArrayBase<byte> edges, uint pointer, uint? data) {
            while (edges.Length <= pointer + 5) {
                edges.Resize(edges.Length + DefaultSizeIncrease);
            }

            return (uint) edges.SetDynamicUInt32Nullable(pointer, data);
        }

        internal void GetTailHeadOrder(uint location, ref byte? tail, ref byte? head) {
            _edges.GetTailHeadOrder(location, ref tail, ref head);
        }

        internal uint DecodeEdgePointerId(uint location, out uint? edgeProfileId) {
            return (uint) _edges.GetDynamicUInt32Nullable(location, out edgeProfileId);
        }

        private void WriteEdgesAndVerticesTo(Stream stream) {
            // write vertex pointers.
            stream.WriteVarUInt32(_nextVertexId);
            for (var i = 0; i < _nextVertexId; i++) {
                stream.WriteVarUInt32(_pointers[i]);
            }

            // write edges.
            stream.WriteVarUInt32(_nextEdgeId);
            for (var i = 0; i < _nextEdgeId; i++) {
                stream.WriteByte(_edges[i]);
            }

            // write cross edge pointers.
            stream.WriteVarUInt32(_nextCrossTileId);
            for (var i = 0; i < _nextCrossTileId; i++) {
                stream.WriteVarUInt32(_crossEdgePointers[i]);
            }
        }

        private void ReadEdgesAndVerticesFrom(Stream stream) {
            // read vertex pointers.
            _nextVertexId = stream.ReadVarUInt32();
            _pointers.Resize(_nextVertexId);
            for (var i = 0; i < _nextVertexId; i++) {
                _pointers[i] = stream.ReadVarUInt32();
            }

            // read edges.
            _nextEdgeId = stream.ReadVarUInt32();
            _edges.Resize(_nextEdgeId);
            for (var i = 0; i < _nextEdgeId; i++) {
                _edges[i] = (byte) stream.ReadByte();
            }

            // read cross tile edge pointers.
            _nextCrossTileId = stream.ReadVarUInt32();
            _crossEdgePointers.Resize(_nextCrossTileId);
            for (var i = 0; i < _nextCrossTileId; i++) {
                _crossEdgePointers[i] = stream.ReadVarUInt32();
            }
        }
    }
}