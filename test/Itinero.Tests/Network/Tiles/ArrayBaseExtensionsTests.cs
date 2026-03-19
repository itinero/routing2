using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class ArrayBaseExtensionsTests
{
    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_1_ShouldIncreaseOneStep()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 1, 15);

        Assert.Equal(15, array.Length);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_OneLessThanStep_ShouldIncreaseOneStep()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 9, 10);

        Assert.Equal(10, array.Length);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_IndexAsStep_ShouldIncreaseTwoSteps()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 10, 10);

        Assert.Equal(20, array.Length);
    }
}
