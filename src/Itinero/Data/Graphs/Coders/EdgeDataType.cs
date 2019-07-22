using System;

namespace Itinero.Data.Graphs.Coders
{
    /// <summary>
    /// The edge data type.
    /// </summary>
    public enum EdgeDataType
    {
        /// <summary>
        /// Stored as a byte.
        /// </summary>
        Byte = 0,
        /// <summary>
        /// Stored as an unsigned short.
        /// </summary>
        UInt16 = 1,
        /// <summary>
        /// Stored as a signed short.
        /// </summary>
        Int16 = 2,
        /// <summary>
        /// Stored as an unsigned integer.
        /// </summary>
        UInt32 = 3,
        /// <summary>
        /// Stored as a signed integer.
        /// </summary>
        Int32 = 4
    }
    
    /// <summary>
    /// Contains edge data type extensions.
    /// </summary>
    public static class EdgeDataTypeExtensions
    {
        /// <summary>
        /// Gets the number of bytes needed to store the given edge data type.
        /// </summary>
        /// <param name="type">The edge data type.</param>
        /// <returns>The number of bytes.</returns>
        /// <exception cref="Exception"></exception>
        public static int ByteCount(this EdgeDataType type)
        {
            switch (type)
            {
                case EdgeDataType.Byte:
                    return 1;
                case EdgeDataType.Int16:
                case EdgeDataType.UInt16:
                    return 2;
                case EdgeDataType.UInt32:
                case EdgeDataType.Int32:
                    return 4;
                default:
                    throw new Exception($"Unknown data type: {type}");
            }
        }
        
        internal static byte ToByte(this EdgeDataType edgeDataType)
        {
            return (byte) edgeDataType;
        }

        internal static EdgeDataType FromByte(byte data)
        {
            switch (data)
            {
                case 0:
                    return EdgeDataType.Byte;
                case 1:
                    return EdgeDataType.UInt16;
                case 2:
                    return EdgeDataType.Int16;
                case 3:
                    return EdgeDataType.UInt32;
                case 4:
                    return EdgeDataType.Int32;
            }
            
            throw new ArgumentOutOfRangeException(nameof(data), $"Cannot convert given byte to a {nameof(EdgeDataType)}.");
        }
    }
}