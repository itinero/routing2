using Itinero.Data.Graphs.Coders;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Coders
{
    public class EdgeDataTypeTests
    {
        [Fact]
        public void EdgeDataType_ShouldConvertToByte()
        {
            Assert.Equal(0, EdgeDataType.Byte.ToByte());
            Assert.Equal(1, EdgeDataType.UInt16.ToByte());
            Assert.Equal(2, EdgeDataType.Int16.ToByte());
            Assert.Equal(3, EdgeDataType.UInt32.ToByte());
            Assert.Equal(4, EdgeDataType.Int32.ToByte());
        }

        [Fact]
        public void EdgeDataType_ShouldConvertFromByte()
        {
            Assert.Equal(EdgeDataType.Byte, EdgeDataTypeExtensions.FromByte(0));
            Assert.Equal(EdgeDataType.UInt16, EdgeDataTypeExtensions.FromByte(1));
            Assert.Equal(EdgeDataType.Int16, EdgeDataTypeExtensions.FromByte(2));
            Assert.Equal(EdgeDataType.UInt32, EdgeDataTypeExtensions.FromByte(3));
            Assert.Equal(EdgeDataType.Int32, EdgeDataTypeExtensions.FromByte(4));
        }
    }
}