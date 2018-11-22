using System.IO;

namespace Itinero.Data.Shapes
{
    static class StreamExtensions
    {
        /// <summary>
        /// Sets the position within the given stream. 
        /// </summary>
        internal static long SeekBegin(this BinaryWriter stream, long offset)
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
    }
}