using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.IO;

namespace Itinero.Indexes;

/// <summary>
/// A default attribute set index using a dictionary internally.
/// </summary>
/// <remarks>
/// Safe for concurrent use. <see cref="Get"/> is called while tiles are inserted from several
/// threads — it is reached indirectly, through the <c>Func</c> handed out by
/// <c>RouterDb.GetEdgeTypeMap()</c>, which makes it easy to miss that it is shared mutable state
/// at all: the call site reads as a pure lookup but interning an unseen set appends here.
///
/// In a production run nothing is ever appended, because the tiles were preprocessed against
/// this same index and every type already exists. So the design optimises the hit: a lock-free
/// dictionary read, no allocation beyond the caller's own sorted array. Only a genuine miss
/// takes a lock.
///
/// Ids have to stay contiguous from zero — <see cref="WriteTo"/> emits the sets in id order and
/// <see cref="ReadFrom"/> restores them by position, so a gap would corrupt the format. That is
/// why the miss path takes a lock and assigns the id itself rather than using
/// <c>GetOrAdd</c> with an <c>Interlocked</c> counter: a concurrent dictionary may run its value
/// factory more than once, and each spare run would burn an id and leave a hole.
/// </remarks>
public sealed class AttributeSetDictionaryIndex : AttributeSetIndex
{
    private readonly ConcurrentDictionary<IReadOnlyList<(string key, string value)>, uint> _index;
    private readonly ConcurrentDictionary<uint, IReadOnlyList<(string key, string value)>> _byId;

    /// Guards the miss path only: assigning the next id and publishing both mappings.
    private readonly object _writeLock = new();

    private int _count;

    /// <summary>
    /// Creates a new attribute set index.
    /// </summary>
    /// <param name="edgeProfiles"></param>
    public AttributeSetDictionaryIndex(List<IReadOnlyList<(string key, string value)>>? edgeProfiles = null)
    {
        _index = new ConcurrentDictionary<IReadOnlyList<(string key, string value)>, uint>(
            AttributeSetEqualityComparer.Default);
        _byId = new ConcurrentDictionary<uint, IReadOnlyList<(string key, string value)>>();

        var initial = edgeProfiles ??
                      new List<IReadOnlyList<(string key, string value)>>
                          { Array.Empty<(string key, string value)>() };
        this.Reset(initial);
    }

    /// Replaces the contents wholesale. Used on construction and by <see cref="ReadFrom"/>.
    private void Reset(IReadOnlyList<IReadOnlyList<(string key, string value)>> sets)
    {
        lock (_writeLock)
        {
            _index.Clear();
            _byId.Clear();
            for (var p = 0; p < sets.Count; p++)
            {
                _byId[(uint)p] = sets[p];
                _index[sets[p]] = (uint)p;
            }

            Volatile.Write(ref _count, sets.Count);
        }
    }

    /// <summary>
    /// Gets the number of distinct sets.
    /// </summary>
    public override uint Count => (uint)Volatile.Read(ref _count);

    /// <summary>
    /// Gets the attributes for the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The attributes in the type.</returns>
    public override IEnumerable<(string key, string value)> GetById(uint id)
    {
        if (!_byId.TryGetValue(id, out var set)) throw new ArgumentOutOfRangeException(nameof(id));

        return set;
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

        // check if profile already there - the only path a production run takes.
        if (_index.TryGetValue(attributeSet, out var edgeProfileId))
        {
            return edgeProfileId;
        }

        lock (_writeLock)
        {
            // Re-check: another thread may have interned the same set while this one waited.
            if (_index.TryGetValue(attributeSet, out edgeProfileId))
            {
                return edgeProfileId;
            }

            // Publish by id first. A reader that resolves an id from the index must always find
            // the set behind it; the other order would expose an id whose attributes are not
            // there yet.
            edgeProfileId = (uint)Volatile.Read(ref _count);
            _byId[edgeProfileId] = attributeSet;
            _index[attributeSet] = edgeProfileId;
            Volatile.Write(ref _count, (int)edgeProfileId + 1);

            return edgeProfileId;
        }
    }

    /// <inheritdoc/>
    public override Task WriteTo(Stream stream)
    {
        // write version #.
        stream.WriteVarInt32(2);

        // write type.
        stream.WriteWithSize("dictionary-index");

        // write pairs, in id order: ReadFrom restores them by position.
        var count = Volatile.Read(ref _count);
        stream.WriteVarInt32(count);
        for (var id = 0; id < count; id++)
        {
            var attributes = _byId[(uint)id];
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
        var sets = new List<IReadOnlyList<(string key, string value)>>(count);
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

            sets.Add(attribute);
        }

        this.Reset(sets);
        return Task.CompletedTask;
    }
}
