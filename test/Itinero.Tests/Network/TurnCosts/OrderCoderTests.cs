using Itinero.Network.TurnCosts;
using Reminiscence.Arrays;
using Xunit;

namespace Itinero.Tests.Network.TurnCosts
{
    public class OrderCoderTests
    {
        [Fact]
        public void OrderCoder_SetTailHeadOrder_Null_Null_ShouldSet0()
        {
            var data = new MemoryArray<byte>(1);

            data.SetTailHeadOrder(0, null, null);

            Assert.Equal(0, data[0]);
        }

        [Fact]
        public void OrderCoder_SetTailHeadOrder_0_Null_ShouldSet1()
        {
            var data = new MemoryArray<byte>(1);

            data.SetTailHeadOrder(0, 0, null);

            Assert.Equal(1, data[0]);
        }

        [Fact]
        public void OrderCoder_SetTailHeadOrder_Null_0_ShouldSet16()
        {
            var data = new MemoryArray<byte>(1);

            data.SetTailHeadOrder(0, null, 0);

            Assert.Equal(16, data[0]);
        }

        [Fact]
        public void OrderCoder_SetTailHeadOrder_14_14_ShouldSet255()
        {
            var data = new MemoryArray<byte>(1);

            data.SetTailHeadOrder(0, 14, 14);

            Assert.Equal(255, data[0]);
        }

        [Fact]
        public void OrderCoder_GetTailHeadOrder_0_ShouldGet_Null_Null()
        {
            var data = new MemoryArray<byte>(1);

            data[0] = 0;

            byte? tail = null;
            byte? head = null;
            data.GetTailHeadOrder(0, ref tail, ref head);

            Assert.Null(tail);
            Assert.Null(head);
        }

        [Fact]
        public void OrderCoder_GetTailHeadOrder_1_ShouldGet_0_Null()
        {
            var data = new MemoryArray<byte>(1);

            data[0] = 1;

            byte? tail = null;
            byte? head = null;
            data.GetTailHeadOrder(0, ref tail, ref head);

            Assert.Equal((byte?)0, tail);
            Assert.Null(head);
        }

        [Fact]
        public void OrderCoder_GetTailHeadOrder_16_ShouldGet_Null_0()
        {
            var data = new MemoryArray<byte>(1);

            data[0] = 16;

            byte? tail = null;
            byte? head = null;
            data.GetTailHeadOrder(0, ref tail, ref head);

            Assert.Null(tail);
            Assert.Equal((byte?)0, head);
        }

        [Fact]
        public void OrderCoder_GetTailHeadOrder_17_ShouldGet_0_0()
        {
            var data = new MemoryArray<byte>(1);

            data[0] = 17;

            byte? tail = null;
            byte? head = null;
            data.GetTailHeadOrder(0, ref tail, ref head);

            Assert.Equal((byte?)0, tail);
            Assert.Equal((byte?)0, head);
        }

        [Fact]
        public void OrderCoder_GetTailHeadOrder_255_ShouldGet_14_14()
        {
            var data = new MemoryArray<byte>(1);

            data[0] = 255;

            byte? tail = null;
            byte? head = null;
            data.GetTailHeadOrder(0, ref tail, ref head);

            Assert.Equal((byte?)14, tail);
            Assert.Equal((byte?)14, head);
        }
    }
}
