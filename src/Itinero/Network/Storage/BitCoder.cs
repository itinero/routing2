using System;
using Reminiscence.Arrays;

namespace Itinero.Network.Storage
{
    internal static class BitCoder
    {
        private const byte Mask = 128 - 1;

        public static long SetDynamicUInt32(this ArrayBase<byte> data, long i, uint value)
        {
            var d0 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                return 1;
            }

            d0 += 128;
            var d1 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                return 2;
            }

            d1 += 128;
            var d2 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                return 3;
            }

            d2 += 128;
            var d3 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                return 4;
            }

            d3 += 128;
            var d4 = (byte)(value & Mask);
            data[i] = d0;
            data[i + 1] = d1;
            data[i + 2] = d2;
            data[i + 3] = d3;
            data[i + 4] = d4;
            return 5;
        }

        public static long SetDynamicUInt64(this ArrayBase<byte> data, long i, ulong value)
        {
            var d0 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                return 1;
            }

            d0 += 128;
            var d1 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                return 2;
            }

            d1 += 128;
            var d2 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                return 3;
            }

            d2 += 128;
            var d3 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                return 4;
            }

            d3 += 128;
            var d4 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                data[i + 4] = d4;
                return 5;
            }

            d4 += 128;
            var d5 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                data[i + 4] = d4;
                data[i + 5] = d5;
                return 6;
            }

            d5 += 128;
            var d6 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                data[i + 4] = d4;
                data[i + 5] = d5;
                data[i + 6] = d6;
                return 7;
            }

            d6 += 128;
            var d7 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                data[i + 4] = d4;
                data[i + 5] = d5;
                data[i + 6] = d6;
                data[i + 7] = d7;
                return 8;
            }

            d7 += 128;
            var d8 = (byte)(value & Mask);
            value >>= 7;
            if (value == 0)
            {
                data[i] = d0;
                data[i + 1] = d1;
                data[i + 2] = d2;
                data[i + 3] = d3;
                data[i + 4] = d4;
                data[i + 5] = d5;
                data[i + 6] = d6;
                data[i + 7] = d7;
                data[i + 8] = d8;
                return 9;
            }

            d8 += 128;
            var d9 = (byte)(value & Mask);
            data[i] = d0;
            data[i + 1] = d1;
            data[i + 2] = d2;
            data[i + 3] = d3;
            data[i + 4] = d4;
            data[i + 5] = d5;
            data[i + 6] = d6;
            data[i + 7] = d7;
            data[i + 8] = d8;
            data[i + 9] = d9;
            return 10;
        }

        public static long GetDynamicUInt32(this ArrayBase<byte> data, long i, out uint value)
        {
            if (i >= data.Length) throw new ArgumentOutOfRangeException(nameof(i));

            var d = data[i];
            if (d < 128)
            {
                value = d;
                return 1;
            }

            value = (uint)d - 128;
            d = data[i + 1];
            if (d < 128)
            {
                value += (uint)d << 7;
                return 2;
            }

            d -= 128;
            value += (uint)d << 7;
            d = data[i + 2];
            if (d < 128)
            {
                value += (uint)d << 14;
                return 3;
            }

            d -= 128;
            value += (uint)d << 14;
            d = data[i + 3];
            if (d < 128)
            {
                value += (uint)d << 21;
                return 4;
            }

            d -= 128;
            value += (uint)d << 21;
            d = data[i + 4];
            value += (uint)d << 28;
            return 5;
        }

        public static long GetDynamicUInt64(this ArrayBase<byte> data, long i, out ulong value)
        {
            if (i >= data.Length) throw new ArgumentOutOfRangeException(nameof(i));

            var d = data[i];
            if (d < 128)
            {
                value = d;
                return 1;
            }

            value = (ulong)d - 128;
            d = data[i + 1];
            if (d < 128)
            {
                value += (uint)d << 7;
                return 2;
            }

            d -= 128;
            value += (ulong)d << 7;
            d = data[i + 2];
            if (d < 128)
            {
                value += (uint)d << 14;
                return 3;
            }

            d -= 128;
            value += (ulong)d << 14;
            d = data[i + 3];
            if (d < 128)
            {
                value += (ulong)d << 21;
                return 4;
            }

            d -= 128;
            value += (ulong)d << 21;
            d = data[i + 4];
            if (d < 128)
            {
                value += (ulong)d << 28;
                return 5;
            }

            d -= 128;
            value += (ulong)d << 28;
            d = data[i + 5];
            if (d < 128)
            {
                value += (ulong)d << 35;
                return 6;
            }

            d -= 128;
            value += (ulong)d << 35;
            d = data[i + 6];
            if (d < 128)
            {
                value += (ulong)d << 42;
                return 7;
            }

            d -= 128;
            value += (ulong)d << 42;
            d = data[i + 7];
            if (d < 128)
            {
                value += (ulong)d << 49;
                return 8;
            }

            d -= 128;
            value += (ulong)d << 49;
            d = data[i + 8];
            if (d < 128)
            {
                value += (ulong)d << 56;
                return 9;
            }

            d -= 128;
            value += (ulong)d << 56;
            d = data[i + 9];
            value += (ulong)d << 63;
            return 10;
        }

        public static uint ToUnsigned(int value)
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

        public static int FromUnsigned(uint unsigned)
        {
            var sign = unsigned & (uint)1;

            var value = (int)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }

        public static long SetGuid(this ArrayBase<byte> data, long i, Guid value)
        {
            var bytes = value.ToByteArray();
            for (var b = 0; b < 16; b++)
            {
                data[i + b] = bytes[b];
            }

            return 16;
        }

        public static long GetGuid(this ArrayBase<byte> data, long i, out Guid value)
        {
            var bytes = new byte[16];
            for (var b = 0; b < 16; b++)
            {
                bytes[b] = data[i + b];
            }

            value = new Guid(bytes);
            return 16;
        }

        public static long SetDynamicInt32(this ArrayBase<byte> data, long i, int value)
        {
            return data.SetDynamicUInt32(i, ToUnsigned(value));
        }

        public static long GetDynamicInt32(this ArrayBase<byte> data, long i, out int value)
        {
            if (i >= data.Length) throw new ArgumentOutOfRangeException(nameof(i));

            var c = data.GetDynamicUInt32(i, out var unsigned);
            value = FromUnsigned(unsigned);
            return c;
        }

        public static ulong ToUnsigned(long value)
        {
            var unsigned = (ulong)value;
            if (value < 0)
            {
                unsigned = (ulong)-value;
            }

            unsigned <<= 1;
            if (value < 0)
            {
                unsigned += 1;
            }

            return unsigned;
        }

        public static long FromUnsigned(ulong unsigned)
        {
            var sign = unsigned & (ulong)1;

            var value = (long)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }

        public static long SetDynamicInt64(this ArrayBase<byte> data, long i, long value)
        {
            return data.SetDynamicUInt64(i, ToUnsigned(value));
        }

        public static long GetDynamicInt64(this ArrayBase<byte> data, long i, out long value)
        {
            var c = data.GetDynamicUInt64(i, out var unsigned);
            value = FromUnsigned(unsigned);
            return c;
        }

        public static long SetDynamicUInt32Nullable(this ArrayBase<byte> data, long i, uint? value)
        {
            value = value == null ? 0 : value + 1;
            return data.SetDynamicUInt32(i, value.Value);
        }

        public static long GetDynamicUInt32Nullable(this ArrayBase<byte> data, long i, out uint? value)
        {
            var c = data.GetDynamicUInt32(i, out var unsigned);
            value = unsigned == 0 ? null : (uint?)unsigned - 1;
            return c;
        }

        public static void SetFixed(this ArrayBase<byte> data, long i, int bytes, int value)
        {
            for (var b = 0; b < bytes; b++)
            {
                data[i + b] = (byte)(value & byte.MaxValue);
                value >>= 8;
            }
        }

        public static void GetFixed(this ArrayBase<byte> data, long i, int bytes, out int value)
        {
            value = 0;
            for (var b = 0; b < bytes; b++)
            {
                value += data[i + b] << (b * 8);
            }
        }
    }
}
