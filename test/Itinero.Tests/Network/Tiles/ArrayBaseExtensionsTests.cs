using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class ArrayBaseExtensionsTests
{
    // These used to pin the exact length of a fixed-increment policy. Callers only require that the
    // position fits, plus proportional growth — without it the appends stay quadratic.

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_1_ShouldFitTheIndex()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 1, 15);

        Assert.True(array.Length > 1);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_OneLessThanStep_ShouldFitTheIndex()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 9, 10);

        Assert.True(array.Length > 9);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_IndexAsStep_ShouldFitTheIndex()
    {
        var array = new int[0];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 10, 10);

        Assert.True(array.Length > 10);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_AlreadyBigEnough_ShouldNotResize()
    {
        var array = new int[20];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 10, 10);

        Assert.Equal(20, array.Length);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_LargeArray_ShouldGrowProportionally()
    {
        // The point of the change: a big array must not grow by the small increment, or appending
        // to it costs a full copy every `step` elements.
        var array = new int[80_000];

        ArrayBaseExtensions.EnsureMinimumSize(ref array, 80_000, 16);

        Assert.True(array.Length >= 80_000 + 80_000 / 8);
    }

    [Fact]
    public void ArrayBaseExtensions_EnsureMinimumSize_AppendingOneByOne_ShouldResizeRarely()
    {
        // Appending 100k elements one at a time must not cost 100k/16 resizes. Counted by watching
        // for a change in Length, which only happens on a resize.
        var array = new int[0];
        var resizes = 0;
        var length = 0;
        for (var i = 0; i < 100_000; i++)
        {
            ArrayBaseExtensions.EnsureMinimumSize(ref array, i, 16);
            if (array.Length == length) continue;

            resizes++;
            length = array.Length;
        }

        Assert.True(array.Length > 100_000);
        Assert.True(resizes < 200, $"expected amortised growth, got {resizes} resizes");
    }
}
