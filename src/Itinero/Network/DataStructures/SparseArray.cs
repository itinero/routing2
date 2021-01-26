using System;
using System.Collections;
using System.Collections.Generic;

namespace Itinero.Network.DataStructures
{
    internal sealed class SparseArray<T> : IEnumerable<(long i, T value)>
    {
        private T[][] _blocks;
        private readonly int _blockSize; // Holds the maximum array size, always needs to be a power of 2.
        private readonly int _arrayPow;
        private long _size; // the total size of this array.
        private readonly T _default;

        public SparseArray(long size, int blockSize = 1 << 16,
            T emptyDefault = default)
        {
            if (size < 0) {
                throw new ArgumentOutOfRangeException(nameof(size), "Size needs to be bigger than or equal to zero.");
            }

            if (blockSize <= 0) {
                throw new ArgumentOutOfRangeException(nameof(blockSize),
                    "Block size needs to be bigger than or equal to zero.");
            }

            if ((blockSize & (blockSize - 1)) != 0) {
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size needs to be a power of 2.");
            }

            _default = emptyDefault;
            _blockSize = blockSize;
            _size = size;
            _arrayPow = ExpOf2(blockSize);

            var blockCount = (long) Math.Ceiling((double) size / _blockSize);
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
            if (powerOf2 == 1) {
                return 0;
            }

            return ExpOf2(powerOf2 / 2) + 1;
        }

        /// <summary>
        /// Gets or sets the item at the given index.
        /// </summary>
        /// <param name="idx">The index.</param>
        public T this[long idx] {
            get {
                if (idx >= Length) {
                    throw new ArgumentOutOfRangeException(nameof(idx));
                }

                var blockId = idx >> _arrayPow;
                var block = _blocks[blockId];
                if (block == null) {
                    return _default;
                }

                var localIdx = idx - (blockId << _arrayPow);
                return block[localIdx];
            }
            set {
                if (idx >= Length) {
                    throw new ArgumentOutOfRangeException(nameof(idx));
                }

                var blockId = idx >> _arrayPow;
                var block = _blocks[blockId];
                if (block == null) {
                    // don't create a new block for a default value.
                    if (EqualityComparer<T>.Default.Equals(value, _default)) {
                        return;
                    }

                    block = new T[_blockSize];
                    for (var i = 0; i < _blockSize; i++) {
                        block[i] = _default;
                    }

                    _blocks[blockId] = block;
                }

                var localIdx = idx % _blockSize;
                _blocks[blockId][localIdx] = value;
            }
        }

        /// <summary>
        /// Resizes this array to the given size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Resize(long size)
        {
            if (size < 0) {
                throw new ArgumentOutOfRangeException(nameof(size),
                    "Cannot resize an array to a size of zero or smaller.");
            }

            _size = size;

            var blockCount = (long) Math.Ceiling((double) size / _blockSize);
            if (blockCount != _blocks.Length) {
                Array.Resize(ref _blocks, (int) blockCount);
            }
        }

        /// <summary>
        /// Gets the length of this array.
        /// </summary>
        public long Length => _size;

        /// <summary>
        /// Creates a clone.
        /// </summary>
        /// <returns>A copy of this array.</returns>
        public SparseArray<T> Clone()
        {
            var blocks = new T[_blocks.Length][];
            for (var b = 0; b < blocks.Length; b++) {
                var block = _blocks[b];
                if (block == null) {
                    continue;
                }

                blocks[b] = (block.Clone() as T[])!;
            }

            return new SparseArray<T>(blocks, _size, _blockSize, _arrayPow, _default);
        }

        public IEnumerator<(long i, T value)> GetEnumerator()
        {
            for (var b = 0; b < _blocks.Length; b++) {
                var block = _blocks[b];
                if (block == null) {
                    continue;
                }

                for (var i = 0; i < block.Length; i++) {
                    yield return (_blockSize * b + i, block[i]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal static class SparseArrayExtensions
    {
        internal static void EnsureMinimumSize<T>(this SparseArray<T> array, long i)
        {
            if (array.Length <= i) {
                array.Resize(i + 1);
            }
        }
    }
}