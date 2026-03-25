using System;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Network.Storage;

internal static class BitCoder
{
    private const byte Mask = 128 - 1;

    public static byte SetDynamicUInt32(this byte[] data, long i, uint value)
    {
        var d0 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            return 1;
        }

        d0 += 128;
        var d1 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            return 2;
        }

        d1 += 128;
        var d2 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            return 3;
        }

        d2 += 128;
        var d3 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            return 4;
        }

        d3 += 128;
        var d4 = (byte)(value & Mask);
        data[(int)i] = d0;
        data[(int)i + 1] = d1;
        data[(int)i + 2] = d2;
        data[(int)i + 3] = d3;
        data[(int)i + 4] = d4;
        return 5;
    }

    public static byte SetDynamicUInt64(this byte[] data, long i, ulong value)
    {
        var d0 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            return 1;
        }

        d0 += 128;
        var d1 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            return 2;
        }

        d1 += 128;
        var d2 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            return 3;
        }

        d2 += 128;
        var d3 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            return 4;
        }

        d3 += 128;
        var d4 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            data[(int)i + 4] = d4;
            return 5;
        }

        d4 += 128;
        var d5 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            data[(int)i + 4] = d4;
            data[(int)i + 5] = d5;
            return 6;
        }

        d5 += 128;
        var d6 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            data[(int)i + 4] = d4;
            data[(int)i + 5] = d5;
            data[(int)i + 6] = d6;
            return 7;
        }

        d6 += 128;
        var d7 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            data[(int)i + 4] = d4;
            data[(int)i + 5] = d5;
            data[(int)i + 6] = d6;
            data[(int)i + 7] = d7;
            return 8;
        }

        d7 += 128;
        var d8 = (byte)(value & Mask);
        value >>= 7;
        if (value == 0)
        {
            data[(int)i] = d0;
            data[(int)i + 1] = d1;
            data[(int)i + 2] = d2;
            data[(int)i + 3] = d3;
            data[(int)i + 4] = d4;
            data[(int)i + 5] = d5;
            data[(int)i + 6] = d6;
            data[(int)i + 7] = d7;
            data[(int)i + 8] = d8;
            return 9;
        }

        d8 += 128;
        var d9 = (byte)(value & Mask);
        data[(int)i] = d0;
        data[(int)i + 1] = d1;
        data[(int)i + 2] = d2;
        data[(int)i + 3] = d3;
        data[(int)i + 4] = d4;
        data[(int)i + 5] = d5;
        data[(int)i + 6] = d6;
        data[(int)i + 7] = d7;
        data[(int)i + 8] = d8;
        data[(int)i + 9] = d9;
        return 10;
    }

    public static byte GetDynamicUInt32(this byte[] data, long i, out uint value)
    {
        var d = data[(int)i];
        if (d < 128)
        {
            value = d;
            return 1;
        }

        value = (uint)d - 128;
        d = data[(int)i + 1];
        if (d < 128)
        {
            value += (uint)d << 7;
            return 2;
        }

        d -= 128;
        value += (uint)d << 7;
        d = data[(int)i + 2];
        if (d < 128)
        {
            value += (uint)d << 14;
            return 3;
        }

        d -= 128;
        value += (uint)d << 14;
        d = data[(int)i + 3];
        if (d < 128)
        {
            value += (uint)d << 21;
            return 4;
        }

        d -= 128;
        value += (uint)d << 21;
        d = data[(int)i + 4];
        value += (uint)d << 28;
        return 5;
    }

    public static byte GetDynamicUInt64(this byte[] data, long i, out ulong value)
    {
        var d = data[(int)i];
        if (d < 128)
        {
            value = d;
            return 1;
        }

        value = (ulong)d - 128;
        d = data[(int)i + 1];
        if (d < 128)
        {
            value += (uint)d << 7;
            return 2;
        }

        d -= 128;
        value += (ulong)d << 7;
        d = data[(int)i + 2];
        if (d < 128)
        {
            value += (uint)d << 14;
            return 3;
        }

        d -= 128;
        value += (ulong)d << 14;
        d = data[(int)i + 3];
        if (d < 128)
        {
            value += (ulong)d << 21;
            return 4;
        }

        d -= 128;
        value += (ulong)d << 21;
        d = data[(int)i + 4];
        if (d < 128)
        {
            value += (ulong)d << 28;
            return 5;
        }

        d -= 128;
        value += (ulong)d << 28;
        d = data[(int)i + 5];
        if (d < 128)
        {
            value += (ulong)d << 35;
            return 6;
        }

        d -= 128;
        value += (ulong)d << 35;
        d = data[(int)i + 6];
        if (d < 128)
        {
            value += (ulong)d << 42;
            return 7;
        }

        d -= 128;
        value += (ulong)d << 42;
        d = data[(int)i + 7];
        if (d < 128)
        {
            value += (ulong)d << 49;
            return 8;
        }

        d -= 128;
        value += (ulong)d << 49;
        d = data[(int)i + 8];
        if (d < 128)
        {
            value += (ulong)d << 56;
            return 9;
        }

        d -= 128;
        value += (ulong)d << 56;
        d = data[(int)i + 9];
        value += (ulong)d << 63;
        return 10;
    }

    public static long SetGuid(this byte[] data, long i, Guid value)
    {
        var bytes = value.ToByteArray();
        for (var b = 0; b < 16; b++)
        {
            data[(int)i + b] = bytes[b];
        }

        return 16;
    }

    public static byte GetGuid(this byte[] data, long i, out Guid value)
    {
        var bytes = new byte[16];
        for (var b = 0; b < 16; b++)
        {
            bytes[b] = data[(int)i + b];
        }

        value = new Guid(bytes);
        return 16;
    }

    public static uint ZigZagEncode32(int value)
    {
        return (uint)((value << 1) ^ (value >> 31));
    }

    public static int ZigZagDecode32(uint value)
    {
        return (int)((value >> 1) ^ (~(value & 1) + 1));
    }

    public static ulong ZigZagEncode64(long value)
    {
        return (ulong)((value << 1) ^ (value >> 63));
    }

    public static long ZigZagDecode64(ulong value)
    {
        return (long)((value >> 1) ^ (~(value & 1) + 1));
    }

    public static byte SetDynamicInt32(this byte[] data, long i, int value)
    {
        return data.SetDynamicUInt32(i, ZigZagEncode32(value));
    }

    public static byte GetDynamicInt32(this byte[] data, long i, out int value)
    {
        var c = data.GetDynamicUInt32(i, out var unsigned);
        value = ZigZagDecode32(unsigned);
        return c;
    }

    public static byte SetDynamicInt64(this byte[] data, long i, long value)
    {
        return data.SetDynamicUInt64(i, ZigZagEncode64(value));
    }

    public static byte GetDynamicInt64(this byte[] data, long i, out long value)
    {
        var c = data.GetDynamicUInt64(i, out var unsigned);
        value = ZigZagDecode64(unsigned);
        return c;
    }

    public static byte SetDynamicUInt32Nullable(this byte[] data, long i, uint? value)
    {
        value = value == null ? 0 : value + 1;
        return data.SetDynamicUInt32(i, value.Value);
    }

    public static byte GetDynamicUInt32Nullable(this byte[] data, long i, out uint? value)
    {
        var c = data.GetDynamicUInt32(i, out var unsigned);
        value = unsigned == 0 ? null : (uint?)unsigned - 1;
        return c;
    }

    public static byte SetDynamicUInt64Nullable(this byte[] data, long i, ulong? value)
    {
        value = value == null ? 0 : value + 1;
        return data.SetDynamicUInt64(i, value.Value);
    }

    public static byte GetDynamicUInt64Nullable(this byte[] data, long i, out ulong? value)
    {
        var c = data.GetDynamicUInt64(i, out var unsigned);
        value = unsigned == 0 ? null : (uint?)unsigned - 1;
        return c;
    }

    public static byte SetDynamicInt64Nullable(this byte[] data, long i, long? value)
    {
        if (value == null) return data.SetDynamicUInt64(i, 0);

        var unsigned = ZigZagEncode64(value.Value) + 1;
        return data.SetDynamicUInt64(i, unsigned);
    }

    public static byte GetDynamicInt64Nullable(this byte[] data, long i, out long? value)
    {
        var c = data.GetDynamicUInt64(i, out var unsigned);
        if (unsigned == 0)
        {
            value = null;
        }
        else
        {
            value = ZigZagDecode64(unsigned - 1);
        }
        return c;
    }

    public static void SetFixed(this byte[] data, long i, int bytes, int value)
    {
        for (var b = 0; b < bytes; b++)
        {
            data[(int)i + b] = (byte)(value & byte.MaxValue);
            value >>= 8;
        }
    }

    public static void GetFixed(this byte[] data, long i, int bytes, out int value)
    {
        value = 0;
        for (var b = 0; b < bytes; b++)
        {
            value += data[(int)i + b] << (b * 8);
        }
    }

    public static byte SetGlobalEdgeId(this byte[] data, long p, GlobalEdgeId globalEdgeId)
    {
        var c = data.SetDynamicInt64(p, globalEdgeId.EdgeId);
        c += data.SetDynamicUInt32(p + c, globalEdgeId.Tail);
        c += data.SetDynamicUInt32(p + c, globalEdgeId.Head);
        return c;
    }

    public static byte GetGlobalEdgeId(this byte[] data, long p, out GlobalEdgeId globalEdgeId)
    {
        var c = data.GetDynamicInt64(p, out var edgeId);
        c += data.GetDynamicUInt32(p + c, out var tail);
        c += data.GetDynamicUInt32(p + c, out var head);

        globalEdgeId = GlobalEdgeId.Create(edgeId, tail, head);

        return c;
    }

    public static byte SetGlobalEdgeIdNullable(this byte[] data, long p, GlobalEdgeId? globalEdgeId)
    {
        if (globalEdgeId == null)
        {
            return data.SetDynamicInt64Nullable(p, null);
        }
        else
        {
            var c = data.SetDynamicInt64Nullable(p, globalEdgeId.Value.EdgeId);
            c += data.SetDynamicUInt32(p + c, globalEdgeId.Value.Tail);
            c += data.SetDynamicUInt32(p + c, globalEdgeId.Value.Head);
            return c;
        }
    }
}
