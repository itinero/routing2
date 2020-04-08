using System;
using System.IO;
using Itinero.Data.Graphs;

namespace Itinero.IO.Osm.Tiles
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

        internal static uint ReadUInt32(this Stream stream)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt32(bytes, 0);
        }

        internal static int ReadInt32(this Stream stream)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes, 0);
        }

        internal static void WriteInt64(this Stream stream, long data)
        {
            var bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, 8); 
        }
        
        internal static void WriteUInt32(this Stream stream, uint data)
        {
            var bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, 4); 
        }

        internal static void WriteVertexId(this Stream stream, VertexId vertexId)
        {
            stream.WriteUInt32(vertexId.TileId);
            stream.WriteUInt32(vertexId.LocalId);
        }

        internal static VertexId ReadVertexId(this Stream stream)
        {
            return new VertexId(stream.ReadUInt32(), stream.ReadUInt32());
        }
    }
}