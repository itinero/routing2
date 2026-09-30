using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data;
using Itinero.IO;
using Itinero.Network.Storage;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    /// <summary>
    /// Stores the attributes, starting with the number of attributes and then alternating key-value pairs.
    /// </summary>
    private byte[] _attributes;

    private uint _nextAttributePointer = 0;

    /// <summary>
    /// Stores each string once.
    /// </summary>
    private string[] _strings;

    private uint _nextStringId = 0;

    /// Shrinks the attribute buffers to what they hold. See NetworkTile.Trim.
    private void TrimAttributes()
    {
        if (_attributes.Length > _nextAttributePointer)
        {
            Array.Resize(ref _attributes, (int)_nextAttributePointer);
        }

        if (_strings.Length > _nextStringId) Array.Resize(ref _strings, (int)_nextStringId);
    }

    private uint SetAttributes(IEnumerable<(string key, string value)> attributes, GlobalEdgeId? globalEdgeId)
    {
        // ensure enough space for the globalEdgeId header (up to 20 bytes).
        EnsureCapacity(ref _attributes, _nextAttributePointer + 21L, 256);

        // save position before globalEdgeId — GetAttributes/GetGlobalEdgeId read from here.
        var start = _nextAttributePointer;

        if (globalEdgeId == null)
        {
            _nextAttributePointer += _attributes.SetDynamicInt64Nullable(_nextAttributePointer, null);
        }
        else
        {
            _nextAttributePointer += _attributes.SetDynamicInt64Nullable(_nextAttributePointer, globalEdgeId.Value.EdgeId);
            _nextAttributePointer += _attributes.SetDynamicUInt32(_nextAttributePointer, globalEdgeId.Value.Tail);
            _nextAttributePointer += _attributes.SetDynamicUInt32(_nextAttributePointer, globalEdgeId.Value.Head);
        }

        long cPos = _nextAttributePointer;
        long p = _nextAttributePointer + 1;
        var c = 0;
        foreach (var (key, value) in attributes)
        {
            EnsureCapacity(ref _attributes, p + 17, 256);

            var id = this.AddOrGetString(key);
            p += _attributes.SetDynamicUInt32(p, id);
            id = this.AddOrGetString(value);
            p += _attributes.SetDynamicUInt32(p, id);

            c++;
            if (c == 255)
            {
                _attributes[(int)cPos] = 255;
                c = 0;
                cPos = p;
                p++;
            }
        }

        EnsureCapacity(ref _attributes, cPos + 1, 256);

        _attributes[(int)cPos] = (byte)c;

        _nextAttributePointer = (uint)p;

        return start;
    }

    internal GlobalEdgeId? GetGlobalEdgeId(uint? pointer)
    {
        if (pointer == null) return null;

        var p = pointer.Value;
        p += _attributes.GetDynamicInt64Nullable(p, out var edgeId);
        if (edgeId == null) return null;
        p += _attributes.GetDynamicUInt32(p, out var tail);
        p += _attributes.GetDynamicUInt32(p, out var head);

        return GlobalEdgeId.Create(edgeId.Value, tail, head);
    }

    internal IEnumerable<(string key, string value)> GetAttributes(uint? pointer)
    {
        if (pointer == null) yield break;

        var p = pointer.Value;
        p += _attributes.GetDynamicInt64Nullable(p, out var edgeId);
        if (edgeId != null)
        {
            p += _attributes.GetDynamicUInt32(p, out _);
            p += _attributes.GetDynamicUInt32(p, out _);
        }

        int count;
        do
        {
            count = _attributes[(int)p];
            p++;

            for (var i = 0; i < count; i++)
            {
                p += _attributes.GetDynamicUInt32(p, out var keyId);
                p += _attributes.GetDynamicUInt32(p, out var valId);

                yield return (_strings[(int)keyId], _strings[(int)valId]);
            }
        } while (count == 255);
    }

    /// <summary>
    /// Calls into <see cref="AddOrGetString"/> and how many entries they compared.
    /// </summary>
    /// <remarks>
    /// AddOrGetString is a linear scan over the tile's strings, run per key and value written, and a
    /// cached tile starts with a full table. Counting comparisons says whether that matters.
    /// </remarks>
    internal static long StringLookups;

    internal static long StringComparisons;

    private uint AddOrGetString(string s)
    {
        System.Threading.Interlocked.Increment(ref StringLookups);
        System.Threading.Interlocked.Add(ref StringComparisons, _nextStringId);

        for (uint i = 0; i < _nextStringId; i++)
        {
            var existing = _strings[(int)i];
            if (existing == s)
            {
                return i;
            }
        }

        EnsureCapacity(ref _strings, _nextStringId + 1L, 256);

        var id = _nextStringId;
        _nextStringId++;

        _strings[(int)id] = s;
        return id;
    }

    private void WriteAttributesTo(Stream stream)
    {
        stream.WriteVarUInt32(_nextAttributePointer);
        for (var i = 0; i < _nextAttributePointer; i++)
        {
            stream.WriteByte(_attributes[i]);
        }

        stream.WriteVarUInt32(_nextStringId);
        for (var i = 0; i < _nextStringId; i++)
        {
            stream.WriteWithSize(_strings[i]);
        }
    }

    private void ReadAttributesFrom(Stream stream)
    {
        _nextAttributePointer = stream.ReadVarUInt32();
        Array.Resize(ref _attributes, (int)_nextAttributePointer);
        for (var i = 0; i < _nextAttributePointer; i++)
        {
            _attributes[i] = (byte)stream.ReadByte();
        }

        _nextStringId = stream.ReadVarUInt32();
        Array.Resize(ref _strings, (int)_nextStringId);
        for (var i = 0; i < _nextStringId; i++)
        {
            _strings[i] = stream.ReadWithSizeString();
        }
    }

    private void ReadAttributesFrom(byte[] data, ref int offset)
    {
        _nextAttributePointer = BitCoderBuffer.GetVarUInt32(data, ref offset);
        Array.Resize(ref _attributes, (int)_nextAttributePointer);
        Buffer.BlockCopy(data, offset, _attributes, 0, (int)_nextAttributePointer);
        offset += (int)_nextAttributePointer;

        _nextStringId = BitCoderBuffer.GetVarUInt32(data, ref offset);
        Array.Resize(ref _strings, (int)_nextStringId);
        for (var i = 0; i < _nextStringId; i++)
        {
            _strings[i] = BitCoderBuffer.GetWithSizeString(data, ref offset);
        }
    }

    private void WriteAttributesTo(byte[] data, ref int offset)
    {
        BitCoderBuffer.SetVarUInt32(data, ref offset, _nextAttributePointer);
        Buffer.BlockCopy(_attributes, 0, data, offset, (int)_nextAttributePointer);
        offset += (int)_nextAttributePointer;

        BitCoderBuffer.SetVarUInt32(data, ref offset, _nextStringId);
        for (var i = 0; i < _nextStringId; i++)
        {
            BitCoderBuffer.SetWithSizeString(data, ref offset, _strings[i]);
        }
    }
}
