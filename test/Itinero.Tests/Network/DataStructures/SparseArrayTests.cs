using System.Collections.Generic;
using System.Linq;
using Itinero.Network.DataStructures;
using Xunit;

namespace Itinero.Tests.Network.DataStructures;

/// Pins the two tile-loading changes: a new block is not re-filled when _default is
/// already default(T), and the block array grows geometrically and never shrinks.
/// Both run under the network write lock while tiles load, which is where the
/// contention showed up.
public class SparseArrayTests
{
    [Fact]
    public void SparseArray_DefaultEmpty_ReadsBackDefault()
    {
        var array = new SparseArray<string?>(100);

        Assert.Null(array[0]);
        Assert.Null(array[99]);
    }

    [Fact]
    public void SparseArray_NonDefaultEmpty_StillFillsNewBlocks()
    {
        // The skip is only valid when _default equals default(T); a custom empty value
        // must still be written across the block.
        var array = new SparseArray<string?>(100, 16, "empty");

        Assert.Equal("empty", array[0]);
        Assert.Equal("empty", array[15]);

        array[3] = "set";

        Assert.Equal("set", array[3]);
        Assert.Equal("empty", array[2]);
        Assert.Equal("empty", array[4]);
    }

    [Fact]
    public void SparseArray_SetAndGet_RoundTrips()
    {
        var array = new SparseArray<string?>(0, 16);
        array.Resize(64);

        array[0] = "a";
        array[17] = "b";
        array[63] = "c";

        Assert.Equal("a", array[0]);
        Assert.Equal("b", array[17]);
        Assert.Equal("c", array[63]);
        Assert.Null(array[1]);
    }

    [Fact]
    public void SparseArray_RepeatedGrowth_KeepsValues()
    {
        // The EnsureMinimumSize pattern: grow one past the end, over and over.
        var array = new SparseArray<string?>(0, 16);
        for (var i = 0; i < 200; i++)
        {
            array.EnsureMinimumSize(i);
            array[i] = $"v{i}";
        }

        Assert.Equal(200, array.Length);
        for (var i = 0; i < 200; i++)
        {
            Assert.Equal($"v{i}", array[i]);
        }
    }

    [Fact]
    public void SparseArray_Grows_DoesNotShrinkBack()
    {
        // Growth is geometric, so Length can sit below the allocated blocks. A later
        // smaller Resize must not throw away headroom or corrupt what is in range.
        var array = new SparseArray<string?>(0, 16);
        array.Resize(1000);
        array[999] = "high";

        array.Resize(500);
        Assert.Equal(500, array.Length);

        array.Resize(1000);
        Assert.Equal("high", array[999]);
    }

    [Fact]
    public void SparseArray_Enumerate_MayYieldPastLength()
    {
        // Documenting existing behaviour rather than endorsing it: enumeration walks
        // whole blocks and ignores Length, so entries past it are yielded. Bounding it
        // at Length breaks RoutingNetworkVertexEnumerator, which never clears its
        // current tile when the tile enumerator runs out and then re-yields that
        // tile's vertex 0 forever.
        var array = new SparseArray<string?>(0, 16);
        array.Resize(64);
        array[60] = "b";

        array.Resize(32);

        Assert.Contains(array, x => x.i == 60 && x.value == "b");
    }

    [Fact]
    public void SparseArray_Enumerate_YieldsSetValues()
    {
        var array = new SparseArray<string?>(0, 16);
        array.Resize(64);
        array[5] = "a";
        array[40] = "b";

        var set = array.Where(x => x.value != null).ToDictionary(x => x.i, x => x.value);

        Assert.Equal("a", set[5]);
        Assert.Equal("b", set[40]);
    }

    [Fact]
    public void SparseArray_Clone_IsIndependent()
    {
        var array = new SparseArray<string?>(0, 16);
        array.Resize(64);
        array[5] = "a";

        var clone = array.Clone();
        clone[5] = "b";

        Assert.Equal("a", array[5]);
        Assert.Equal("b", clone[5]);
    }

    [Fact]
    public void SparseArray_OutOfRange_Throws()
    {
        var array = new SparseArray<string?>(0, 16);
        array.Resize(20);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => array[20]);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => array[20] = "x");
    }
}
