using System.Collections.Generic;
using System.IO;
using Itinero.Data;
using Itinero.IO;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    /// <summary>
    /// Stores the attributes, starting with the number of attributes and then alternating key-value pairs.
    /// </summary>
    private readonly ArrayBase<byte> _attributes;

    private uint _nextAttributePointer = 0;

    /// <summary>
    /// Stores each string once.
    /// </summary>
    private readonly ArrayBase<string> _strings;

    private uint _nextStringId = 0;

    private uint SetAttributes(IEnumerable<(string key, string value)> attributes)
    {
        var start = _nextAttributePointer;

        long cPos = start;
        long p = start + 1;
        var c = 0;
        foreach (var (key, value) in attributes)
        {
            if (_attributes.Length <= p + 16)
            {
                _attributes.Resize(_attributes.Length + 256);
            }

            var id = this.AddOrGetString(key);
            p += _attributes.SetDynamicUInt32(p, id);
            id = this.AddOrGetString(value);
            p += _attributes.SetDynamicUInt32(p, id);

            c++;
            if (c == 255)
            {
                _attributes[cPos] = 255;
                c = 0;
                cPos = p;
                p++;
            }
        }

        if (_attributes.Length <= cPos)
        {
            _attributes.Resize(_attributes.Length + 256);
        }

        _attributes[cPos] = (byte)c;

        _nextAttributePointer = (uint)p;

        return start;
    }

    internal IEnumerable<(string key, string value)> GetAttributes(uint? pointer)
    {
        if (pointer == null)
        {
            yield break;
        }

        var p = pointer.Value;

        var count = -1;
        do
        {
            count = _attributes[p];
            p++;

            for (var i = 0; i < count; i++)
            {
                p += (uint)_attributes.GetDynamicUInt32(p, out var keyId);
                p += (uint)_attributes.GetDynamicUInt32(p, out var valId);

                yield return (_strings[keyId], _strings[valId]);
            }
        } while (count == 255);
    }

    private uint AddOrGetString(string s)
    {
        for (uint i = 0; i < _nextStringId; i++)
        {
            var existing = _strings[i];
            if (existing == s)
            {
                return i;
            }
        }

        if (_strings.Length <= _nextStringId)
        {
            _strings.Resize(_strings.Length + 256);
        }

        var id = _nextStringId;
        _nextStringId++;

        _strings[id] = s;
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
        _attributes.Resize(_nextAttributePointer);
        for (var i = 0; i < _nextAttributePointer; i++)
        {
            _attributes[i] = (byte)stream.ReadByte();
        }

        _nextStringId = stream.ReadVarUInt32();
        _strings.Resize(_nextStringId);
        for (var i = 0; i < _nextStringId; i++)
        {
            _strings[i] = stream.ReadWithSizeString();
        }
    }
}
