using Itinero.Network.Tiles;
using Reminiscence.Arrays;
using Xunit;

namespace Itinero.Tests.Network.Tiles {
    public class ArrayBaseExtensionsTests {
        [Fact]
        public void ArrayBaseExtensions_EnsureMinimumSize_1_ShouldIncreaseOneStep() {
            var array = new MemoryArray<int>(0);

            array.EnsureMinimumSize(1, 15);

            Assert.Equal(15, array.Length);
        }

        [Fact]
        public void ArrayBaseExtensions_EnsureMinimumSize_OneLessThanStep_ShouldIncreaseOneStep() {
            var array = new MemoryArray<int>(0);

            array.EnsureMinimumSize(9, 10);

            Assert.Equal(10, array.Length);
        }

        [Fact]
        public void ArrayBaseExtensions_EnsureMinimumSize_IndexAsStep_ShouldIncreaseTwoSteps() {
            var array = new MemoryArray<int>(0);

            array.EnsureMinimumSize(10, 10);

            Assert.Equal(20, array.Length);
        }
    }
}