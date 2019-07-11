using System;
using System.IO;
using Reminiscence.Arrays;
using Reminiscence.IO;

namespace Itinero.Algorithms.DataStructures
{
    /// <summary>
    /// An in-memory sparse array.
    /// </summary>
    public class SparseArray<T>
        where T : struct
    {
        private T[][] _blocks;
        private readonly int _blockSize; // Holds the maximum array size, always needs to be a power of 2.
        private readonly int _arrayPow;
        private long _size; // the total size of this array.
        private readonly T _default = default;
        
        /// <summary>
        /// Creates a new array.
        /// </summary>
        public SparseArray(long size, int blockSize = 1 << 16,
            T emptyDefault = default)
        {
            if (size < 0) { throw new ArgumentOutOfRangeException(nameof(size), "Size needs to be bigger than or equal to zero."); }
            if (blockSize <= 0) { throw new ArgumentOutOfRangeException(nameof(blockSize),"Block size needs to be bigger than or equal to zero."); }
            if ((blockSize & (blockSize - 1)) != 0) { throw new ArgumentOutOfRangeException(nameof(blockSize),"Block size needs to be a power of 2."); }

            _default = emptyDefault;
            _blockSize = blockSize;
            _size = size;
            _arrayPow = ExpOf2(blockSize);

            var blockCount = (long)System.Math.Ceiling((double)size / _blockSize);
            _blocks = new T[blockCount][];
        }

        private static int ExpOf2(int powerOf2)
        { // this can probably be faster but it needs to run once in the constructor,
            // feel free to improve but not crucial.
            if (powerOf2 == 1)
            {
                return 0;
            }
            return ExpOf2(powerOf2 / 2) + 1;
        }

        /// <summary>
        /// Gets or sets the element at the given idx.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public T this[long idx]
        {
            get
            {
                var blockId = idx >> _arrayPow;
                var block = _blocks[blockId];
                if (block == null) return default(T);
                
                var localIdx = idx - (blockId << _arrayPow);
                return block[localIdx];
            }
            set
            {
                var blockId = idx >> _arrayPow;
                var block = _blocks[blockId];
                if (block == null)
                {
                    block = new T[_blockSize];
                    for (var i = 0; i < _blockSize; i++)
                    {
                        block[i] = _default;
                    }
                    _blocks[blockId] = block;
                }
                var localIdx = idx % _blockSize;
                _blocks[blockId][localIdx] = value;
            }
        }

        /// <summary>
        /// Returns true if this array can be resized.
        /// </summary>
        public bool CanResize => true;

        /// <summary>
        /// Resizes this array.
        /// </summary>
        /// <param name="size"></param>
        public void Resize(long size)
        {
            if (size < 0) { throw new ArgumentOutOfRangeException(nameof(size), "Cannot resize a huge array to a size of zero or smaller."); }

            _size = size;

            var blockCount = (long)System.Math.Ceiling((double)size / _blockSize);
            if (blockCount != _blocks.Length)
            {
                Array.Resize(ref _blocks, (int)blockCount);
            }
        }

        /// <summary>
        /// Returns the length of this array.
        /// </summary>
        public long Length => _size;
    }
}