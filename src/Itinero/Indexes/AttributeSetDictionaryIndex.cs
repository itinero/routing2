using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.IO;

namespace Itinero.Indexes;

/// <summary>
/// A default attribute set index using a dictionary internally.
/// </summary>
public sealed class AttributeSetDictionaryIndex : AttributeSetIndex
{
    private readonly List<IReadOnlyList<(string key, string value)>> _edgeProfiles;
    private readonly Dictionary<IReadOnlyList<(string key, string value)>, uint> _edgeProfilesIndex;

    /// <summary>
    /// Creates a new attribute set index.
    /// </summary>
    /// <param name="edgeProfiles"></param>
    public AttributeSetDictionaryIndex(List<IReadOnlyList<(string key, string value)>>? edgeProfiles = null)
    {
        _edgeProfiles = edgeProfiles ?? new List<IReadOnlyList<(string key, string value)>> { Array.Empty<(string key, string value)>() }; ;
        _edgeProfilesIndex =
            new Dictionary<IReadOnlyList<(string key, string value)>, uint>(AttributeSetEqualityComparer.Default);
        this.UpdateIndex();
    }

    internal void UpdateIndex()
    {
        _edgeProfilesIndex.Clear();
        for (var p = 0; p < _edgeProfiles.Count; p++)
        {
            _edgeProfilesIndex[_edgeProfiles[p]] = (uint)p;
        }
    }

    /// <summary>
    /// Gets the number of distinct sets.
    /// </summary>
    public override uint Count
    {
        get
        {
            return (uint)_edgeProfiles.Count;
        }
    }

    /// <summary>
    /// Gets the attributes for the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The attributes in the type.</returns>
    public override IEnumerable<(string key, string value)> GetById(uint id)
    {
        if (id > _edgeProfiles.Count) throw new ArgumentOutOfRangeException(nameof(id));

        return _edgeProfiles[(int)id];
    }

    /// <summary>
    /// Gets the type id for the given attributes set.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>The id, if any.</returns>
    public override uint Get(IEnumerable<(string key, string value)> attributes)
    {
        var attributeSet = attributes.ToArray();

        // sort array.
        Array.Sort(attributeSet, (x, y) => x.CompareTo(y));

        // check if profile already there.
        if (_edgeProfilesIndex.TryGetValue(attributeSet, out var edgeProfileId))
        {
            return edgeProfileId;
        }

        // add new profile.
        edgeProfileId = (uint)_edgeProfiles.Count;
        _edgeProfiles.Add(attributeSet);
        _edgeProfilesIndex.Add(attributeSet, edgeProfileId);

        return edgeProfileId;
    }
    
    /// <inheritdoc/>
    public override Task WriteTo(Stream stream)
    {
        // write version #.
        stream.WriteVarInt32(2);
        
        // write type.
        stream.WriteWithSize("dictionary-index");

        // write pairs.
        stream.WriteVarInt32(_edgeProfiles.Count);
        foreach (var attributes in _edgeProfiles)
        {
            stream.WriteVarInt32(attributes.Count);
            foreach (var (key, value) in attributes)
            {
                stream.WriteWithSize(key);
                stream.WriteWithSize(value);
            }
        }

        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public override Task ReadFrom(Stream stream)
    {
        // get version #.
        var version = stream.ReadVarInt32();
        if (version != 2) throw new InvalidDataException("Unexpected version #.");
        
        // read type.
        var type = stream.ReadWithSizeString();
        if (type != "dictionary-index") throw new InvalidDataException("Unexpected index type.");

        // read pairs.
        var count = stream.ReadVarInt32();
        _edgeProfiles.Clear();
        for (var i = 0; i < count; i++)
        {
            var c = stream.ReadVarInt32();
            var attribute = new (string key, string value)[c];
            for (var a = 0; a < c; a++)
            {
                var key = stream.ReadWithSizeString();
                var value = stream.ReadWithSizeString();
                attribute[a] = (key, value);
            }

            _edgeProfiles.Add(attribute);
        }
        this.UpdateIndex();
        return Task.CompletedTask;
    }
}
