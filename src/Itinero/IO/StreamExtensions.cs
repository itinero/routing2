using System;
using System.IO;

namespace Itinero.IO;

internal static class StreamExtensions
{
    internal static long WriteWithSize(this Stream stream, string value)
    {
        var bytes = System.Text.Encoding.Unicode.GetBytes(value);
        return stream.WriteWithSize(bytes);
    }

    internal static long WriteWithSize(this Stream stream, byte[] value)
    {
        stream.Write(BitConverter.GetBytes((long)value.Length), 0, 8);
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
}
