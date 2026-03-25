using System;
using System.Text;
using Itinero.Network.Storage;

namespace Itinero.IO;

internal static class BitCoderBuffer
{
    public static uint GetVarUInt32(byte[] data, ref int offset)
    {
        var size = data.GetDynamicUInt32(offset, out var value);
        offset += size;
        return value;
    }

    public static uint? GetVarUInt32Nullable(byte[] data, ref int offset)
    {
        var size = data.GetDynamicUInt32Nullable(offset, out var value);
        offset += size;
        return value;
    }

    public static int GetVarInt32(byte[] data, ref int offset)
    {
        return FromUnsigned(GetVarUInt32(data, ref offset));
    }

    public static int? GetVarInt32Nullable(byte[] data, ref int offset)
    {
        return FromUnsigned(GetVarUInt32Nullable(data, ref offset));
    }

    public static Guid GetGuid(byte[] data, ref int offset)
    {
        data.GetGuid(offset, out var value);
        offset += 16;
        return value;
    }

    public static long GetInt64(byte[] data, ref int offset)
    {
        long value = 0;
        for (var b = 0; b < 8; b++)
        {
            value += (long)data[offset + b] << (b * 8);
        }

        offset += 8;
        return value;
    }

    public static string GetWithSizeString(byte[] data, ref int offset)
    {
        var size = GetInt64(data, ref offset);
        var str = Encoding.Unicode.GetString(data, offset, (int)size);
        offset += (int)size;
        return str;
    }

    public static void SetVarUInt32(byte[] data, ref int offset, uint value)
    {
        offset += data.SetDynamicUInt32(offset, value);
    }

    public static void SetVarUInt32Nullable(byte[] data, ref int offset, uint? value)
    {
        offset += data.SetDynamicUInt32Nullable(offset, value);
    }

    public static void SetVarInt32(byte[] data, ref int offset, int value)
    {
        SetVarUInt32(data, ref offset, ToUnsigned(value));
    }

    public static void SetVarInt32Nullable(byte[] data, ref int offset, int? value)
    {
        SetVarUInt32Nullable(data, ref offset, ToUnsigned(value));
    }

    public static void SetGuid(byte[] data, ref int offset, Guid value)
    {
        data.SetGuid(offset, value);
        offset += 16;
    }

    public static void SetInt64(byte[] data, ref int offset, long value)
    {
        for (var b = 0; b < 8; b++)
        {
            data[offset + b] = (byte)(value & byte.MaxValue);
            value >>= 8;
        }

        offset += 8;
    }

    public static void SetWithSizeString(byte[] data, ref int offset, string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);
        SetInt64(data, ref offset, bytes.Length);
        Buffer.BlockCopy(bytes, 0, data, offset, bytes.Length);
        offset += bytes.Length;
    }

    private static uint ToUnsigned(int value)
    {
        var unsigned = (uint)value;
        if (value < 0)
        {
            unsigned = (uint)-value;
        }

        unsigned <<= 1;
        if (value < 0)
        {
            unsigned += 1;
        }

        return unsigned;
    }

    private static uint? ToUnsigned(int? value)
    {
        if (value == null) return null;
        return ToUnsigned(value.Value);
    }

    private static int FromUnsigned(uint unsigned)
    {
        var sign = unsigned & 1;
        var value = (int)(unsigned >> 1);
        if (sign == 1)
        {
            value = -value;
        }

        return value;
    }

    private static int? FromUnsigned(uint? unsigned)
    {
        if (unsigned == null) return null;
        return FromUnsigned(unsigned.Value);
    }
}
