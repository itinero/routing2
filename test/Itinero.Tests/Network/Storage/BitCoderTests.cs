using Itinero.Network.Storage;
using Reminiscence.Arrays;
using Xunit;

namespace Itinero.Tests.Network.Storage
{
    public class BitCoderTests
    {
        [Fact]
        public void BitCoder_ShouldCodeFixed_Int32_3_0()
        {
            var data = new MemoryArray<byte>(5);

            data.SetFixed(0, 3, 0);
            data.GetFixed(0, 3, out var result);
            Assert.Equal(0, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeFixed_Int32_3_4242()
        {
            var data = new MemoryArray<byte>(5);

            data.SetFixed(0, 3, 4242);
            data.GetFixed(0, 3, out var result);
            Assert.Equal(4242, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeFixed_Int32_3_424242()
        {
            var data = new MemoryArray<byte>(5);

            data.SetFixed(0, 3, 424242);
            data.GetFixed(0, 3, out var result);
            Assert.Equal(424242, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_0()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(1, data.SetDynamicUInt32(0, 0));
            Assert.Equal(1, data.GetDynamicUInt32(0, out var result));
            Assert.Equal((uint)0, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_46()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(1, data.SetDynamicUInt32(0, 46));
            Assert.Equal(1, data.GetDynamicUInt32(0, out var result));
            Assert.Equal((uint)46, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_4646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(2, data.SetDynamicUInt32(0, 4646));
            Assert.Equal(2, data.GetDynamicUInt32(0, out var result));
            Assert.Equal((uint)4646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_464646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(3, data.SetDynamicUInt32(0, 464646));
            Assert.Equal(3, data.GetDynamicUInt32(0, out var result));
            Assert.Equal((uint)464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_46464646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(4, data.SetDynamicUInt32(0, 46464646));
            Assert.Equal(4, data.GetDynamicUInt32(0, out var result));
            Assert.Equal((uint)46464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32_MaxValue()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(5, data.SetDynamicUInt32(0, uint.MaxValue));
            Assert.Equal(5, data.GetDynamicUInt32(0, out var result));
            Assert.Equal(uint.MaxValue, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_0()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(1, data.SetDynamicUInt64(0, (ulong)0));
            Assert.Equal(1, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)0, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_46()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(1, data.SetDynamicUInt64(0, (ulong)46));
            Assert.Equal(1, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)46, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_4646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(2, data.SetDynamicUInt64(0, (ulong)4646));
            Assert.Equal(2, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)4646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_464646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(3, data.SetDynamicUInt64(0, (ulong)464646));
            Assert.Equal(3, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_46464646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(4, data.SetDynamicUInt64(0, (ulong)46464646));
            Assert.Equal(4, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)46464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_4646464646()
        {
            var data = new MemoryArray<byte>(5);

            Assert.Equal(5, data.SetDynamicUInt64(0, (ulong)4646464646));
            Assert.Equal(5, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)4646464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_464646464646()
        {
            var data = new MemoryArray<byte>(6);

            Assert.Equal(6, data.SetDynamicUInt64(0, (ulong)464646464646));
            Assert.Equal(6, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)464646464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_46464646464646()
        {
            var data = new MemoryArray<byte>(7);

            Assert.Equal(7, data.SetDynamicUInt64(0, (ulong)46464646464646));
            Assert.Equal(7, data.GetDynamicUInt64(0, out var result));
            Assert.Equal((ulong)46464646464646, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt64_MaxValue()
        {
            var data = new MemoryArray<byte>(10);

            Assert.Equal(10, data.SetDynamicUInt64(0, ulong.MaxValue));
            Assert.Equal(10, data.GetDynamicUInt64(0, out var result));
            Assert.Equal(ulong.MaxValue, result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32Nullable_Null()
        {
            var data = new MemoryArray<byte>(10);

            Assert.Equal(1, data.SetDynamicUInt32Nullable(0, null));
            Assert.Equal(1, data.GetDynamicUInt32Nullable(0, out var result));
            Assert.Null(result);
        }
        
        [Fact]
        public void BitCoder_ShouldCodeDynamic_UInt32Nullable_0()
        {
            var data = new MemoryArray<byte>(10);

            Assert.Equal(1, data.SetDynamicUInt32Nullable(0, 0));
            Assert.Equal(1, data.GetDynamicUInt32Nullable(0, out var result));
            Assert.Equal((uint?)0, result);
        }
    }
}