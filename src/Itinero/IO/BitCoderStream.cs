using System.IO;

namespace Itinero.IO
{
    internal static class BitCoderStream
    {
        private const byte Mask = 128 - 1;

        public static void WriteVarUInt32Nullable(this Stream stream, uint? value)
        {
            if (value == null)
            {
                stream.WriteVarUInt32(0);
            }
            else
            {
                stream.WriteVarUInt32(value.Value + 1);
            }
        }

        public static void WriteVarUInt32(this Stream stream, uint value)
        {
            var d0 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                return;
            }

            d0 += 128;
            var d1 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                return;
            }

            d1 += 128;
            var d2 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                return;
            }

            d2 += 128;
            var d3 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                return;
            }

            d3 += 128;
            var d4 = (byte) (value & Mask);
            stream.WriteByte(d0);
            stream.WriteByte(d1);
            stream.WriteByte(d2);
            stream.WriteByte(d3);
            stream.WriteByte(d4);
            return;
        }


        public static void WriteVarUInt64Nullable(this Stream stream, ulong? value)
        {
            if (value == null)
            {
                stream.WriteVarUInt64(0);
            }
            else
            {
                stream.WriteVarUInt64(value.Value + 1);
            }
        }

        public static void WriteVarUInt64(this Stream stream, ulong value)
        {
            var d0 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                return;
            }

            d0 += 128;
            var d1 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                return;
            }

            d1 += 128;
            var d2 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                return;
            }

            d2 += 128;
            var d3 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                return;
            }

            d3 += 128;
            var d4 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                stream.WriteByte(d4);
                return;
            }

            d4 += 128;
            var d5 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                stream.WriteByte(d4);
                stream.WriteByte(d5);
                return;
            }

            d5 += 128;
            var d6 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                stream.WriteByte(d4);
                stream.WriteByte(d5);
                stream.WriteByte(d6);
                return;
            }

            d6 += 128;
            var d7 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                stream.WriteByte(d4);
                stream.WriteByte(d5);
                stream.WriteByte(d6);
                stream.WriteByte(d7);
                return;
            }

            d7 += 128;
            var d8 = (byte) (value & Mask);
            value >>= 7;
            if (value == 0)
            {
                stream.WriteByte(d0);
                stream.WriteByte(d1);
                stream.WriteByte(d2);
                stream.WriteByte(d3);
                stream.WriteByte(d4);
                stream.WriteByte(d5);
                stream.WriteByte(d6);
                stream.WriteByte(d7);
                stream.WriteByte(d8);
                return;
            }

            d8 += 128;
            var d9 = (byte) (value & Mask);
            stream.WriteByte(d0);
            stream.WriteByte(d1);
            stream.WriteByte(d2);
            stream.WriteByte(d3);
            stream.WriteByte(d4);
            stream.WriteByte(d5);
            stream.WriteByte(d6);
            stream.WriteByte(d7);
            stream.WriteByte(d8);
            stream.WriteByte(d9);
            return;
        }

        public static uint? ReadVarUInt32Nullable(this Stream stream)
        {
            var value = stream.ReadVarUInt32();
            if (value == 0) return null;

            return value - 1;
        }

        public static uint ReadVarUInt32(this Stream stream)
        {
            var value = 0U;
            var d = stream.ReadByte();
            if (d < 128)
            {
                value = (uint)d;
                return value;
            }
            value = (uint)d - 128;
            d = stream.ReadByte();
            if (d < 128)
            {
                value += ((uint)d << 7);
                return value;
            }
            d -= 128;
            value += ((uint)d << 7);
            d = stream.ReadByte();
            if (d < 128)
            {
                value += ((uint)d << 14);
                return value;
            }
            d -= 128;
            value += ((uint)d << 14);
            d = stream.ReadByte();
            if (d < 128)
            {
                value += ((uint)d << 21);
                return value;
            }
            d -= 128;
            value += ((uint)d << 21); 
            d =stream.ReadByte();
            value += ((uint) d << 28);
            return value;
        }

        public static ulong? ReadVarUInt64Nullable(this Stream stream)
        {
            var value = stream.ReadVarUInt64();
            if (value == 0) return null;

            return value - 1;
        }

        public static ulong ReadVarUInt64(this Stream stream)
        {
            var value = 0UL;
            var d = stream.ReadByte();
            if (d < 128)
            {
                value = (ulong)d;
                return value;
            }

            value = (ulong) d - 128;
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((uint) d << 7);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 7);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((uint) d << 14);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 14);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 21);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 21);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 28);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 28);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 35);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 35);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 42);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 42);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 49);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 49);
            d = stream.ReadByte();;
            if (d < 128)
            {
                value += ((ulong) d << 56);
                return value;
            }

            d -= 128;
            value += ((ulong) d << 56);
            d = stream.ReadByte();;
            value += ((ulong) d << 63);
            return value;
        }

        private static ulong ToUnsigned(long value)
        {
            var unsigned = (ulong) value;
            if (value < 0) unsigned = (ulong) -value;

            unsigned <<= 1;
            if (value < 0)
            {
                unsigned += 1;
            }

            return unsigned;
        }

        private static ulong? ToUnsigned(long? valueNullable)
        {
            if (valueNullable == null) return null;

            var value = valueNullable.Value;
            var unsigned = (ulong) value;
            if (value < 0) unsigned = (ulong) -value;

            unsigned <<= 1;
            if (value < 0)
            {
                unsigned += 1;
            }

            return unsigned;
        }

        private static uint ToUnsigned(int value)
        {
            var unsigned = (uint) value;
            if (value < 0) unsigned = (uint) -value;

            unsigned <<= 1;
            if (value < 0)
            {
                unsigned += 1;
            }

            return unsigned;
        }

        private static uint? ToUnsigned(int? valueNullable)
        {
            if (valueNullable == null) return null;

            var value = valueNullable.Value;
            var unsigned = (uint) value;
            if (value < 0) unsigned = (uint) -value;

            unsigned <<= 1;
            if (value < 0)
            {
                unsigned += 1;
            }

            return unsigned;
        }

        private static long FromUnsigned(ulong unsigned)
        {
            var sign = unsigned & (uint)1;

            var value = (long)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }

        private static long? FromUnsigned(ulong? unsignedNullable)
        {
            if (unsignedNullable == null) return null;

            var unsigned = unsignedNullable.Value;
            var sign = unsigned & (uint)1;

            var value = (long)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }

        private static int FromUnsigned(uint unsigned)
        {
            var sign = unsigned & (uint)1;

            var value = (int)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }

        private static int? FromUnsigned(uint? unsignedNullable)
        {
            if (unsignedNullable == null) return null;

            var unsigned = unsignedNullable.Value;
            var sign = unsigned & (uint)1;

            var value = (int)(unsigned >> 1);
            if (sign == 1)
            {
                value = -value;
            }

            return value;
        }
        
        public static void WriteVarInt32Nullable(this Stream data, int? value)
        {
            data.WriteVarUInt32Nullable(ToUnsigned(value));
        }
        
        public static void WriteVarInt32(this Stream data, int value)
        {
            data.WriteVarUInt32(ToUnsigned(value));
        }

        public static int? ReadVarInt32Nullable(this Stream data)
        {
            return FromUnsigned(data.ReadVarUInt32Nullable());
        }

        public static int ReadVarInt32(this Stream data)
        {
            return FromUnsigned(data.ReadVarUInt32());
        }
        
        public static void WriteVarInt64Nullable(this Stream data, long? value)
        {
            data.WriteVarUInt64Nullable(ToUnsigned(value));
        }
        
        public static void WriteVarInt64(this Stream data, long value)
        {
            data.WriteVarUInt64(ToUnsigned(value));
        }

        public static long? ReadVarInt64Nullable(this Stream data)
        {
            return FromUnsigned(data.ReadVarUInt64Nullable());
        }

        public static long ReadVarInt64(this Stream data)
        {
            return FromUnsigned(data.ReadVarUInt64());
        }

        public static void WriteInt64(this Stream stream, long value)
        {
            for (var b = 0; b < 8; b++)
            {
                stream.WriteByte((byte)(value & byte.MaxValue));
                value >>= 8;
            }
        }

        public static long ReadInt64(this Stream stream)
        {
            var value = 0L;
            for (var b = 0; b < 8; b++)
            {
                value += ((long)stream.ReadByte() << (b * 8));
            }

            return value;
        }

        public static void WriteUInt32(this Stream stream, uint value)
        {
            for (var b = 0; b < 4; b++)
            {
                stream.WriteByte((byte)(value & byte.MaxValue));
                value >>= 8;
            }
        }

        public static uint ReadUInt32(this Stream stream)
        {
            var value = 0U;
            for (var b = 0; b < 4; b++)
            {
                value += ((uint)stream.ReadByte() << (b * 8));
            }

            return value;
        }

        public static void WriteUInt64(this Stream stream, ulong value)
        {
            for (var b = 0; b < 8; b++)
            {
                stream.WriteByte((byte)(value & byte.MaxValue));
                value >>= 8;
            }
        }

        public static ulong ReadUInt64(this Stream stream)
        {
            var value = 0UL;
            for (var b = 0; b < 8; b++)
            {
                value += ((ulong)stream.ReadByte() << (b * 8));
            }

            return value;
        }

        public static void WriteInt32(this Stream stream, int value)
        {
            for (var b = 0; b < 4; b++)
            {
                stream.WriteByte((byte)(value & byte.MaxValue));
                value >>= 8;
            }
        }

        public static int ReadInt32(this Stream stream)
        {
            var value = 0;
            for (var b = 0; b < 4; b++)
            {
                value += (stream.ReadByte() << (b * 8));
            }

            return value;
        }
    }
}