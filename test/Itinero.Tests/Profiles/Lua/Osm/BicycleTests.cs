using System.Linq;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Xunit;

namespace Itinero.Tests.Profiles.Lua.Osm;

public class BicycleTests
{
    [Fact]
    public void Bicycle_ResidentialShouldGiveNonZeroFactor()
    {
        var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        var factor = profile.Factor(new[] {
                ("highway", "residential")
            });

        Assert.True(factor.BackwardFactor != 0);
        Assert.True(factor.ForwardFactor != 0);
    }

    [Fact]
    public void Bicycle_PedestrianShouldGiveZeroFactor()
    {
        var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        var factor = profile.Factor(new[] {
                ("highway", "pedestrian")
            });

        Assert.True(factor.BackwardFactor == 0);
        Assert.True(factor.ForwardFactor == 0);
    }

    [Fact]
    public void Bicycle_OneWayShouldGiveZeroBackwardFactor()
    {
        var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        var factor = profile.Factor(new[] {
                ("highway", "residential"),
                ("oneway", "yes")
            });

        Assert.True(factor.BackwardFactor == 0);
        Assert.True(factor.ForwardFactor != 0);
    }

    [Fact]
    public void Bicycle_TurnCostFactor_Empty_ShouldReturnEmpty()
    {
        var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        var factor = profile.TurnCostFactor(Enumerable.Empty<(string key, string value)>());

        Assert.Equal(TurnCostFactor.Empty, factor);
    }

    [Fact]
    public void Bicycle_TurnCostFactor_BicycleTurnRestriction_ShouldReturnBinary()
    {
        var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        var factor = profile.TurnCostFactor(new[] {
                ("restriction", "yes"),
                ("bicycle", "yes")
            });

        Assert.Equal(TurnCostFactor.Binary, factor);
    }
}
