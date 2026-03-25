using Itinero.Profiles.Lua.Osm;
using Xunit;

namespace Itinero.Tests.Profiles.Lua.Osm;

public class CarProfileTests
{
    [Fact]
    public void TurnCostFactor_BarrierBollard_ShouldBeBinary()
    {
        var profile = OsmProfiles.Car;
        var factor = profile.TurnCostFactor(new[] { ("barrier", "bollard") });
        Assert.True(factor.IsBinary);
    }

    [Fact]
    public void TurnCostFactor_BarrierBollard_WithMotorcarYes_ShouldNotBlock()
    {
        var profile = OsmProfiles.Car;
        var factor = profile.TurnCostFactor(new[] { ("barrier", "bollard"), ("motorcar", "yes") });
        Assert.False(factor.IsBinary, "barrier with motorcar=yes should not block cars");
        Assert.Equal(0u, factor.CostFactor);
    }
}
