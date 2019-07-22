using System;
using System.IO;

namespace Itinero.Data
{
    internal static class StreamExtensions
    {
        internal static long WriteWithSize(this Stream stream, string value)
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(value);
            return stream.WriteWithSize(bytes);
        }
        
        internal static long WriteWithSize(this Stream stream, byte[] value)
        {
            stream.Write(System.BitConverter.GetBytes((long)value.Length), 0, 8);
            stream.Write(value, 0, value.Length);
            return value.Length + 8;
        }
        
        internal static string ReadWithSizeString(this Stream stream)
        {
            var size = stream.ReadInt64();
            var data = new byte[size];
            stream.Read(data, 0, (int)size);

            return System.Text.Encoding.Unicode.GetString(data, 0, data.Length);
        }

        internal static long ReadInt64(this Stream stream)
        {
            var longBytes = new byte[8];
            stream.Read(longBytes, 0, 8);
            return BitConverter.ToInt64(longBytes, 0);
        }

        internal static int ReadInt32(this Stream stream)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}