using System;
using System.IO;
using Reminiscence.Arrays;

namespace routing2
{
    public static class Extensions
    {
        /// <summary>
        /// Sets the position within the given stream. 
        /// </summary>
        public static long SeekBegin(this BinaryWriter stream, long offset)
        {
            if(offset <= int.MaxValue)
            {
                return stream.Seek((int)offset, SeekOrigin.Begin);
            }
            stream.Seek(0, SeekOrigin.Begin);
            while(offset > int.MaxValue)
            {
                stream.Seek(int.MaxValue, SeekOrigin.Current);
                offset -= int.MaxValue;
            }
            return stream.Seek((int)offset, SeekOrigin.Current);
        }

        /// <summary>
        /// Ensures that this <see cref="ArrayBase{T}"/> has room for at least
        /// the given number of elements, resizing if not.
        /// </summary>
        /// <typeparam name="T">
        /// The type of element stored in this array.
        /// </typeparam>
        /// <param name="array">
        /// This array.
        /// </param>
        /// <param name="minimumSize">
        /// The minimum number of elements that this array must fit.
        /// </param>
        public static void EnsureMinimumSize<T>(this ArrayBase<T> array, long minimumSize)
        {
            if (array.Length < minimumSize)
            {
                IncreaseMinimumSize(array, minimumSize, fillEnd: false, fillValueIfNeeded: default(T));
            }
        }

        /// <summary>
        /// Ensures that this <see cref="ArrayBase{T}"/> has room for at least
        /// the given number of elements, resizing and filling the empty space
        /// with the given value if not.
        /// </summary>
        /// <typeparam name="T">
        /// The type of element stored in this array.
        /// </typeparam>
        /// <param name="array">
        /// This array.
        /// </param>
        /// <param name="minimumSize">
        /// The minimum number of elements that this array must fit.
        /// </param>
        /// <param name="fillValue">
        /// The value to use to fill in the empty spaces if we have to resize.
        /// </param>
        public static void EnsureMinimumSize<T>(this ArrayBase<T> array, long minimumSize, T fillValue)
        {
            if (array.Length < minimumSize)
            {
                IncreaseMinimumSize(array, minimumSize, fillEnd: true, fillValueIfNeeded: fillValue);
            }
        }

        private static void IncreaseMinimumSize<T>(ArrayBase<T> array, long minimumSize, bool fillEnd, T fillValueIfNeeded)
        {
            long oldSize = array.Length;

            // fast-forward, perhaps, through the first several resizes.
            // Math.Max also ensures that we can resize from 0.
            long size = Math.Max(1024, oldSize * 2);
            while (size < minimumSize)
            {
                size *= 2;
            }

            array.Resize(size);
            if (!fillEnd)
            {
                return;
            }

            for (long i = oldSize; i < size; i++)
            {
                array[i] = fillValueIfNeeded;
            }
        }
    }
}