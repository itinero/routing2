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
        Byte,
        /// <summary>
        /// Stored as an unsigned short.
        /// </summary>
        UInt16,
        /// <summary>
        /// Stored as a signed short.
        /// </summary>
        Int16,
        /// <summary>
        /// Stored as an unsigned integer.
        /// </summary>
        UInt32,
        /// <summary>
        /// Stored as a signed integer.
        /// </summary>
        Int32
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
    }
}