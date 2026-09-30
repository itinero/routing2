using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Itinero.Network.DataStructures;

internal sealed class SparseArray<T> : IEnumerable<(long i, T value)>
{
    private T[][] _blocks;
    private readonly int _blockSize; // Holds the maximum array size, always needs to be a power of 2.
    private readonly int _arrayPow;
    private long _size; // the total size of this array.
    private readonly T _default;

    /// <summary>
    /// Guards growing: replacing <see cref="_blocks"/> and allocating a block into it.
    /// </summary>
    /// <remarks>
    /// Growing copies the array of block references, so a block installed into the old array mid-copy
    /// is lost. Allocating under the same lock removes that and the double-allocate race.
    /// </remarks>
    private readonly object _growLock = new();

    public SparseArray(long size, int blockSize = 1 << 16,
        T emptyDefault = default)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size needs to be bigger than or equal to zero.");
        }

        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize),
                "Block size needs to be bigger than or equal to zero.");
        }

        if ((blockSize & (blockSize - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size needs to be a power of 2.");
        }

        _default = emptyDefault;
        _blockSize = blockSize;
        _size = size;
        _arrayPow = ExpOf2(blockSize);

        var blockCount = (long)Math.Ceiling((double)size / _blockSize);
        _blocks = new T[blockCount][];
    }

    private SparseArray(T[][] blocks, long size, int blockSize, int arrayPow, T @default)
    {
        _blocks = blocks;
        _size = size;
        _blockSize = blockSize;
        _arrayPow = arrayPow;
        _default = @default;
    }

    private static int ExpOf2(int powerOf2)
    {
        // this can probably be faster but it needs to run once in the constructor,
        // feel free to improve but not crucial.
        if (powerOf2 == 1)
        {
            return 0;
        }

        return ExpOf2(powerOf2 / 2) + 1;
    }

    /// <summary>
    /// Gets or sets the item at the given index.
    /// </summary>
    /// <param name="idx">The index.</param>
    public T this[long idx]
    {
        get
        {
            if (idx >= this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(idx));
            }

            // One read of the field: reading twice could see two arrays either side of a concurrent grow,
            // and growth only copies block references forward.
            var blocks = Volatile.Read(ref _blocks);
            var blockId = idx >> _arrayPow;

            // Length can be visible before the wider blocks array is. Nothing has been written
            // that far out yet, so the default is the right answer.
            if (blockId >= blocks.Length)
            {
                return _default;
            }

            var block = blocks[blockId];
            if (block == null)
            {
                return _default;
            }

            var localIdx = idx - (blockId << _arrayPow);
            return block[localIdx];
        }
        set
        {
            if (idx >= this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(idx));
            }

            var blockId = idx >> _arrayPow;
            var blocks = Volatile.Read(ref _blocks);
            var block = blockId < blocks.Length ? blocks[blockId] : null;
            if (block == null)
            {
                // don't create a new block for a default value.
                if (EqualityComparer<T>.Default.Equals(value, _default))
                {
                    return;
                }

                block = this.EnsureBlock(blockId);
            }

            // Write through the block reference, not through _blocks[blockId] a second time: the
            // block object is shared across grows, so this stays correct if the array is replaced
            // underneath, whereas re-reading the array could land on a stale slot.
            var localIdx = idx % _blockSize;
            block[localIdx] = value;
        }
    }

    /// Allocates the block for <paramref name="blockId"/>, or returns the one another thread
    /// allocated first.
    private T[] EnsureBlock(long blockId)
    {
        lock (_growLock)
        {
            var blocks = _blocks;
            if (blockId < blocks.Length && blocks[blockId] != null) return blocks[blockId];

            // A new array arrives filled with default(T), so only a _default that
            // differs from it needs writing. Skipping that loop matters: _blockSize
            // is 65536.
            var block = new T[_blockSize];
            if (!EqualityComparer<T>.Default.Equals(_default, default!))
            {
                for (var i = 0; i < _blockSize; i++)
                {
                    block[i] = _default;
                }
            }

            if (blockId >= blocks.Length)
            {
                // Length said this index was in range, so the blocks array has not caught up
                // yet. Grow it here rather than failing.
                var grown = new T[Math.Max(blockId + 1, Math.Min(blocks.Length * 2L, int.MaxValue))][];
                Array.Copy(blocks, grown, blocks.Length);
                blocks = grown;
                Volatile.Write(ref _blocks, blocks);
            }

            blocks[blockId] = block;
            return block;
        }
    }

    /// <summary>
    /// Resizes this array to the given size.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Resize(long size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size),
                "Cannot resize an array to a size of zero or smaller.");
        }

        // Grow-only, and geometrically. EnsureMinimumSize resizes to exactly i + 1, so
        // tile loading walked this with an Array.Resize per new block while holding the
        // network write lock. Never shrinking keeps that amortised: shrinking back to
        // blockCount would undo the headroom on the very next call.
        //
        // Blocks past _size stay unreachable - the indexer bounds-checks against Length
        // and the enumerator stops at _size - so the extra entries are inert.
        lock (_growLock)
        {
            this.GrowBlocksTo(size);

            // Published last: Length is what the indexer bounds-checks against, so widening it
            // before the blocks array exists would admit an index with nowhere to go.
            Volatile.Write(ref _size, size);
        }
    }

    /// <summary>
    /// Grows to at least <paramref name="minimum"/>, never shrinking.
    /// </summary>
    /// <remarks>
    /// Separate from Resize, which must set the size it is given. A check-then-Resize pair outside a
    /// lock lets a smaller Resize land last and shrink the array.
    /// </remarks>
    public void EnsureSize(long minimum)
    {
        if (Volatile.Read(ref _size) >= minimum) return;

        lock (_growLock)
        {
            if (_size >= minimum) return;

            this.GrowBlocksTo(minimum);
            Volatile.Write(ref _size, minimum);
        }
    }

    /// Widens the block array to cover <paramref name="size"/>. Caller holds <see cref="_growLock"/>.
    private void GrowBlocksTo(long size)
    {
        var blockCount = (long)Math.Ceiling((double)size / _blockSize);
        if (blockCount <= _blocks.Length) return;

        // Copy into a fresh array and publish it, rather than Array.Resize's ref-swap, so
        // a reader always observes a fully populated array.
        var grown = new T[Math.Max(blockCount, Math.Min(_blocks.Length * 2L, int.MaxValue))][];
        Array.Copy(_blocks, grown, _blocks.Length);
        Volatile.Write(ref _blocks, grown);
    }

    /// <summary>
    /// Gets the length of this array.
    /// </summary>
    public long Length => Volatile.Read(ref _size);

    /// <summary>
    /// Creates a clone.
    /// </summary>
    /// <returns>A copy of this array.</returns>
    public SparseArray<T> Clone()
    {
        var blocks = new T[_blocks.Length][];
        for (var b = 0; b < blocks.Length; b++)
        {
            var block = _blocks[b];
            if (block == null)
            {
                continue;
            }

            blocks[b] = (block.Clone() as T[])!;
        }

        return new SparseArray<T>(blocks, _size, _blockSize, _arrayPow, _default);
    }

    public IEnumerator<(long i, T value)> GetEnumerator()
    {
        for (var b = 0; b < _blocks.Length; b++)
        {
            var block = _blocks[b];
            if (block == null)
            {
                continue;
            }

            for (var i = 0; i < block.Length; i++)
            {
                yield return ((_blockSize * b) + i, block[i]);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}

internal static class SparseArrayExtensions
{
    internal static void EnsureMinimumSize<T>(this SparseArray<T> array, long i)
    {
        // Not a Length check plus Resize: see SparseArray.EnsureSize for why that pair races.
        array.EnsureSize(i + 1);
    }
}
