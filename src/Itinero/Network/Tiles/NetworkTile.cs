using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;
using Itinero.Network.Storage;
using Itinero.Network.TurnCosts;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    private const int DefaultSizeIncrease = 16;

    /// <summary>
    /// Makes sure a buffer can hold <paramref name="required"/> elements, growing it geometrically.
    /// </summary>
    /// <remarks>
    /// Fixed-increment growth made appending to a large tile quadratic, and a cached tile is sized to
    /// its content so the first boundary edge resizes it. Capacity is invisible to the file format.
    /// </remarks>
    private static void EnsureCapacity<T>(ref T[] array, long required, int minimumIncrease)
    {
        if (array.Length >= required) return;

        var grown = array.Length + Math.Max(minimumIncrease, array.Length >> 3);
        Array.Resize(ref array, (int)Math.Max(required, grown));
    }

    private readonly uint _tileId;
    private readonly int _zoom; // the zoom level.

    private uint _nextVertexId = 0; // the next vertex id.

    // the pointers, per vertex, to their first edge.
    // TODO: investigate if it's worth storing these with less precision, one tile will never contain this much data.
    // TODO: investigate if we can not use one-increasing vertex ids but also use their pointers like with the edges.
    private uint[] _pointers;
    private uint _nextCrossTileId; // the next id for an edge that crosses tile boundaries.
    private uint[] _crossEdgePointers; // points to the cross tile boundary edges.

    // the next edge id.
    private uint _nextEdgeId = 0;

    // the edges.
    private byte[] _edges;

    /// <summary>
    /// Creates a new tile.
    /// </summary>
    /// <param name="zoom">The zoom level.</param>
    /// <param name="tileId">The tile id.</param>
    public NetworkTile(int zoom, uint tileId)
    {
        _zoom = zoom;
        _tileId = tileId;
        (_boxLeft, _boxTop, _boxLonStep, _boxLatStep) = this.ComputeBox();
        _nextCrossTileId = 0;

        _pointers = new uint[0];
        _edges = new byte[0];
        _crossEdgePointers = new uint[0];

        _coordinates = new byte[0];
        _shapes = new byte[0];
        _attributes = new byte[0];
        _strings = new string[0];
    }

    private NetworkTile(int zoom, uint tileId, uint nextCrossTileId, uint[] pointers,
        byte[] edges,
        uint[] crossEdgePointers, byte[] coordinates, byte[] shapes,
        byte[] attributes,
        string[] strings, byte[] turnCosts, uint nextVertexId, uint nextEdgeId,
        uint nextAttributePointer,
        uint nextShapePointer, uint nextStringId)
    {
        _zoom = zoom;
        _tileId = tileId;
        (_boxLeft, _boxTop, _boxLonStep, _boxLatStep) = this.ComputeBox();
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
    public NetworkTile Clone()
    {
        return new(_zoom, _tileId, _nextCrossTileId, (uint[])_pointers.Clone(), (byte[])_edges.Clone(),
            (uint[])_crossEdgePointers.Clone(),
            (byte[])_coordinates.Clone(), (byte[])_shapes.Clone(), (byte[])_attributes.Clone(), (string[])_strings.Clone(), (byte[])_turnCosts.Clone(),
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
    /// Adds a new vertex and returns its id.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="e">The elevation in meters.</param>
    /// <returns>The ID of the new vertex.</returns>
    public VertexId AddVertex(double longitude, double latitude, float? e = null)
    {
        // set coordinate.
        this.SetCoordinate(_nextVertexId, longitude, latitude, e);

        // create id.
        var vertexId = new VertexId(_tileId, _nextVertexId);
        _nextVertexId++;

        // make room.
        ArrayBaseExtensions.EnsureMinimumSize(ref _pointers, vertexId.LocalId, DefaultSizeIncrease);

        return vertexId;
    }

    /// <summary>
    /// Returns true if the vertex exists.
    /// </summary>
    /// <param name="vertex"></param>
    /// <returns></returns>
    public bool HasVertex(VertexId vertex)
    {
        if (vertex.LocalId >= _nextVertexId)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the vertex with the given id.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>True if the vertex exists.</returns>
    public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude, out float? elevation)
    {
        longitude = default;
        latitude = default;
        elevation = null;
        if (vertex.LocalId >= _nextVertexId)
        {
            return false;
        }

        this.GetCoordinate(vertex.LocalId, out longitude, out latitude, out elevation);
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
    public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, uint edgeTypeId,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
        IEnumerable<(string key, string value)>? attributes = null, EdgeId? edgeId = null,
        uint? length = null)
    {
        // Edge set changed → cached MaxLonDiff/MaxLatDiff are now stale.
        this.InvalidateDiffs();

        if (vertex2.TileId != _tileId)
        {
            // this edge crosses tiles boundaries, it need special treatment and a stable id.
            // because the edge originates in this tile, this tile is responsible for generating the id.
            if (edgeId != null) throw new ArgumentException(
                    "The edge id shouldn't be a given, it should be generated by the originating tile.",
                    nameof(edgeId));
            if (vertex1.TileId != _tileId) throw new ArgumentException("None of the two vertices in this edge are in this tile.",
                    nameof(vertex1));

            // generate a new cross tile id and store pointer to edge.
            edgeId = EdgeId.CrossEdgeId(_tileId, _nextCrossTileId);
            ArrayBaseExtensions.EnsureMinimumSize(ref _crossEdgePointers, _nextCrossTileId + 1);
            _crossEdgePointers[_nextCrossTileId] = _nextEdgeId;
            _nextCrossTileId++;
        }
        else if (vertex1.TileId != _tileId)
        {
            // this edge crosses tiles boundaries, it need special treatment and a stable id.
            // because the edge originates in another tile it should already have an id.
            if (edgeId == null) throw new ArgumentException(
                    "Cannot add an edge that doesn't start in this tile without a proper edge id.",
                    nameof(edgeId));
            if (edgeId.Value.TileId != vertex1.TileId) throw new ArgumentException("The edge id doesn't match the tile id in the second vertex.",
                    nameof(edgeId));
        }
        else
        {
            // this edge starts in this tile, it get an id from this tile.
            edgeId = new EdgeId(_tileId, _nextEdgeId);
        }

        // write the edge data.
        var newEdgePointer = _nextEdgeId;
        var size = EncodeVertex(ref _edges, _tileId, _nextEdgeId, vertex1);
        _nextEdgeId += size;
        size = EncodeVertex(ref _edges, _tileId, _nextEdgeId, vertex2);
        _nextEdgeId += size;

        // get previous pointers if vertex already has edges
        // set the new pointers.
        uint? v1p = null;
        if (vertex1.TileId == _tileId)
        {
            v1p = _pointers[vertex1.LocalId].DecodeNullableData();
            _pointers[vertex1.LocalId] = newEdgePointer.EncodeToNullableData();
        }

        uint? v2p = null;
        if (vertex2.TileId == _tileId)
        {
            v2p = _pointers[vertex2.LocalId].DecodeNullableData();
            _pointers[vertex2.LocalId] = newEdgePointer.EncodeToNullableData();
        }

        // set next pointers.
        size = EncodePointer(ref _edges, _nextEdgeId, v1p);
        _nextEdgeId += size;
        size = EncodePointer(ref _edges, _nextEdgeId, v2p);
        _nextEdgeId += size;

        // write edge id explicitly if not in this edge.
        if (vertex1.TileId != vertex2.TileId)
        {
            // this data will only be there for edges crossing tile boundaries.
            _nextEdgeId += (uint)_edges.SetDynamicUInt32(_nextEdgeId,
                edgeId.Value.LocalId - EdgeId.MinCrossId);
        }

        // write edge profile id.
        _nextEdgeId += SetDynamicUIn32(ref _edges, _nextEdgeId, edgeTypeId);

        // write length.
        _nextEdgeId += SetDynamicUIn32Nullable(ref _edges, _nextEdgeId, length);

        // set tail and head order.
        _edges.SetTailHeadOrder(_nextEdgeId, null, null);
        _nextEdgeId++;

        // take care of shape if any.
        uint? shapePointer = null;
        if (shape != null)
        {
            shapePointer = this.SetShape(shape);
        }

        size = EncodePointer(ref _edges, _nextEdgeId, shapePointer);
        _nextEdgeId += size;

        // take care of attributes if any.
        uint? attributesPointer = null;
        if (attributes != null)
        {
            attributesPointer = this.SetAttributes(attributes);
        }

        size = EncodePointer(ref _edges, _nextEdgeId, attributesPointer);
        _nextEdgeId += size;

        return edgeId.Value;
    }

    /// <summary>
    /// Shrinks every appendable buffer to what it actually holds.
    /// </summary>
    /// <remarks>
    /// Growing by an eighth leaves slack in arrays resident for the process's life; hot measured −2.4%
    /// without this. Caller must hold this tile's write lock: it replaces the arrays.
    /// </remarks>
    internal void Trim()
    {
        if (_pointers.Length > _nextVertexId) Array.Resize(ref _pointers, (int)_nextVertexId);
        if (_edges.Length > _nextEdgeId) Array.Resize(ref _edges, (int)_nextEdgeId);
        if (_crossEdgePointers.Length > _nextCrossTileId)
        {
            Array.Resize(ref _crossEdgePointers, (int)_nextCrossTileId);
        }

        this.TrimAttributes();
        this.TrimShapes();
        this.TrimTurnCosts();
    }

    internal uint VertexEdgePointer(uint vertex)
    {
        return _pointers[vertex];
    }

    internal static byte EncodeVertex(ref byte[] edges, uint localTileId, uint location, VertexId vertexId)
    {
        if (vertexId.TileId == localTileId)
        {
            // same tile, only store local id.
            EnsureCapacity(ref edges, location + 6L, DefaultSizeIncrease);

            return edges.SetDynamicUInt32(location, vertexId.LocalId);
        }

        // other tile, store full id.
        EnsureCapacity(ref edges, location + 11L, DefaultSizeIncrease);

        var encodedId = vertexId.Encode();
        return edges.SetDynamicUInt64(location, encodedId);
    }

    internal byte DecodeVertex(uint location, out uint localId, out uint tileId)
    {
        var size = _edges.GetDynamicUInt64(location, out var encodedId);
        if (encodedId < uint.MaxValue)
        {
            localId = (uint)encodedId;
            tileId = _tileId;
            return size;
        }

        VertexId.Decode(encodedId, out tileId, out localId);
        return size;
    }

    internal byte DecodeEdgeCrossId(uint location, out uint edgeCrossId)
    {
        var s = _edges.GetDynamicUInt32(location, out var c);
        edgeCrossId = EdgeId.MinCrossId + c;
        return s;
    }

    internal uint GetEdgeCrossPointer(uint edgeCrossId)
    {
        return _crossEdgePointers[edgeCrossId];
    }

    internal static byte EncodePointer(ref byte[] edges, uint location, uint? pointer)
    {
        // TODO: save the diff instead of the full pointer.
        EnsureCapacity(ref edges, location + 6L, DefaultSizeIncrease);

        return edges.SetDynamicUInt32(location,
            pointer.EncodeAsNullableData());
    }

    internal byte DecodePointer(uint location, out uint? pointer)
    {
        var size = _edges.GetDynamicUInt32(location, out var data);
        pointer = data.DecodeNullableData();
        return size;
    }

    internal static byte SetDynamicUIn32(ref byte[] edges, uint pointer, uint data)
    {
        EnsureCapacity(ref edges, pointer + 6L, DefaultSizeIncrease);

        return edges.SetDynamicUInt32(pointer, data);
    }

    internal static byte SetDynamicUIn32Nullable(ref byte[] edges, uint pointer, uint? data)
    {
        EnsureCapacity(ref edges, pointer + 6L, DefaultSizeIncrease);

        return edges.SetDynamicUInt32Nullable(pointer, data);
    }

    internal void GetTailHeadOrder(uint location, ref byte? tail, ref byte? head)
    {
        _edges.GetTailHeadOrder(location, ref tail, ref head);
    }

    internal byte DecodeEdgeTypeId(uint location, out uint edgeTypeId)
    {
        return _edges.GetDynamicUInt32(location, out edgeTypeId);
    }

    internal byte DecodeEdgePointerId(uint location, out uint? edgeProfileId)
    {
        return _edges.GetDynamicUInt32Nullable(location, out edgeProfileId);
    }

    private void WriteEdgesAndVerticesTo(Stream stream)
    {
        // write vertex pointers.
        stream.WriteVarUInt32(_nextVertexId);
        for (var i = 0; i < _nextVertexId; i++)
        {
            stream.WriteVarUInt32(_pointers[i]);
        }

        // write edges.
        stream.WriteVarUInt32(_nextEdgeId);
        for (var i = 0; i < _nextEdgeId; i++)
        {
            stream.WriteByte(_edges[i]);
        }

        // write cross edge pointers.
        stream.WriteVarUInt32(_nextCrossTileId);
        for (var i = 0; i < _nextCrossTileId; i++)
        {
            stream.WriteVarUInt32(_crossEdgePointers[i]);
        }
    }

    private void ReadEdgesAndVerticesFrom(Stream stream)
    {
        // read vertex pointers.
        _nextVertexId = stream.ReadVarUInt32();
        Array.Resize(ref _pointers, (int)_nextVertexId);
        for (var i = 0; i < _nextVertexId; i++)
        {
            _pointers[i] = stream.ReadVarUInt32();
        }

        // read edges.
        _nextEdgeId = stream.ReadVarUInt32();
        Array.Resize(ref _edges, (int)_nextEdgeId);
        for (var i = 0; i < _nextEdgeId; i++)
        {
            _edges[i] = (byte)stream.ReadByte();
        }

        // read cross tile edge pointers.
        _nextCrossTileId = stream.ReadVarUInt32();
        Array.Resize(ref _crossEdgePointers, (int)_nextCrossTileId);
        for (var i = 0; i < _nextCrossTileId; i++)
        {
            _crossEdgePointers[i] = stream.ReadVarUInt32();
        }
    }

    private void ReadEdgesAndVerticesFrom(byte[] data, ref int offset)
    {
        _nextVertexId = BitCoderBuffer.GetVarUInt32(data, ref offset);
        Array.Resize(ref _pointers, (int)_nextVertexId);
        for (var i = 0; i < _nextVertexId; i++)
        {
            _pointers[i] = BitCoderBuffer.GetVarUInt32(data, ref offset);
        }

        _nextEdgeId = BitCoderBuffer.GetVarUInt32(data, ref offset);
        Array.Resize(ref _edges, (int)_nextEdgeId);
        Buffer.BlockCopy(data, offset, _edges, 0, (int)_nextEdgeId);
        offset += (int)_nextEdgeId;

        _nextCrossTileId = BitCoderBuffer.GetVarUInt32(data, ref offset);
        Array.Resize(ref _crossEdgePointers, (int)_nextCrossTileId);
        for (var i = 0; i < _nextCrossTileId; i++)
        {
            _crossEdgePointers[i] = BitCoderBuffer.GetVarUInt32(data, ref offset);
        }
    }

    private void WriteEdgesAndVerticesTo(byte[] data, ref int offset)
    {
        BitCoderBuffer.SetVarUInt32(data, ref offset, _nextVertexId);
        for (var i = 0; i < _nextVertexId; i++)
        {
            BitCoderBuffer.SetVarUInt32(data, ref offset, _pointers[i]);
        }

        BitCoderBuffer.SetVarUInt32(data, ref offset, _nextEdgeId);
        Buffer.BlockCopy(_edges, 0, data, offset, (int)_nextEdgeId);
        offset += (int)_nextEdgeId;

        BitCoderBuffer.SetVarUInt32(data, ref offset, _nextCrossTileId);
        for (var i = 0; i < _nextCrossTileId; i++)
        {
            BitCoderBuffer.SetVarUInt32(data, ref offset, _crossEdgePointers[i]);
        }
    }
}
