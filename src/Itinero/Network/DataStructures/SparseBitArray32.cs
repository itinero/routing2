// using System;
// using System.Collections;
// using System.Collections.Generic;
// namespace Itinero.Network.DataStructures
// {
//     /// <summary>
//     /// Represents a sparse bitarray.
//     /// </summary>
//     internal class SparseBitArray32 : IEnumerable<long>
//     {
//         private readonly int _blockSize; // Holds the blocksize, or the size of the 'sub arrays'.
//         private readonly long _length; // Holds the length of this array.
//         private readonly Dictionary<long, BitArray> _data; // holds the actual data blocks.
//         //private readonly BitArray32[] _data; // Holds the actual data blocks.
//
//         /// <summary>
//         /// Creates a new sparse bitarray.
//         /// </summary>
//         public SparseBitArray32(long size, int blockSize)
//         {
//             if (size % 32 != 0) { throw new ArgumentOutOfRangeException(nameof(size), "Size has to be divisible by 32."); }
//             if (size % blockSize != 0) { throw new ArgumentOutOfRangeException(nameof(size), "Size has to be divisible by block size."); }
//
//             _length = size;
//             _blockSize = blockSize;
//             _data = new Dictionary<long, BitArray>();
//         }
//
//         /// <summary>
//         /// Gets or sets the value at the given index.
//         /// </summary>
//         public bool this[long idx]
//         {
//             get
//             {
//                 int blockId = (int)(idx / _blockSize);
//                 if (_data.TryGetValue(blockId, out _))
//                 { // the block actually exists.
//                     int blockIdx = (int)(idx % _blockSize);
//                     return _data[blockId][blockIdx];
//                 }
//                 return false;
//             }
//             set
//             {
//                 int blockId = (int)(idx / _blockSize);
//                 if (!_data.TryGetValue(blockId, out var block))
//                 {
//                     if (value)
//                     { // only add new block if true.
//                         block = new BitArray(_blockSize);
//                         int blockIdx = (int)(idx % _blockSize);
//                         block[blockIdx] = true;
//                         _data[blockId] = block;
//                     }
//                 }
//                 else
//                 { // set value at block.
//                     int blockIdx = (int)(idx % _blockSize);
//                     block[blockIdx] = value;
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Returns the length of this array.
//         /// </summary>
//         public long Length
//         {
//             get
//             {
//                 return _length;
//             }
//         }
//
//         /// <summary>
//         /// Gets the enumerator.
//         /// </summary>
//         /// <returns></returns>
//         public IEnumerator<long> GetEnumerator()
//         {
//             return new Enumerator(this);
//         }
//
//         IEnumerator IEnumerable.GetEnumerator()
//         {
//             return new Enumerator(this);
//         }
//
//         private struct Enumerator : IEnumerator<long>
//         {
//             private readonly SparseBitArray32 _array;
//             
//             public Enumerator(SparseBitArray32 array)
//             {
//                 _array = array;
//                 _current = -1;
//             }
//
//             private long _current;
//
//             public long Current
//             {
//                 get
//                 {
//                     return _current;
//                 }
//             }
//
//             object IEnumerator.Current
//             {
//                 get
//                 {
//                     if (_current < 0)
//                     {
//                         throw new InvalidOperationException();
//                     }
//                     if (_current >= _array.Length)
//                     {
//                         throw new InvalidOperationException();
//                     }
//                     return _current;
//                 }
//             }
//
//             public bool MoveNext()
//             {
//                 if (_current >= _array.Length)
//                 {
//                     return false;
//                 }
//
//                 while (true)
//                 {
//                     _current++;
//                     if (_current >= _array.Length)
//                     {
//                         return false;
//                     }
//                     if (_array[_current])
//                     {
//                         break;
//                     }
//                 }
//                 return true;
//             }
//
//             public void Reset()
//             {
//                 _current = -1;
//             }
//
//             public void Dispose()
//             {
//
//             }
//         }
//     }
// }

