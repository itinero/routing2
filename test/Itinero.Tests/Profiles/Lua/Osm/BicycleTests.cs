using Itinero.Data.Attributes;
using Xunit;

namespace Itinero.Tests.Profiles.Lua.Osm
{
    public class BicycleTests
    {
        [Fact]
        public void Bicycle_ResidentialShouldGiveNonZeroFactor()
        {
            var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var factor = profile.Factor(new AttributeCollection(
                new Attribute("highway", "residential")));
            
            Assert.True(factor.BackwardFactor != 0);
            Assert.True(factor.ForwardFactor != 0);
        }
        
        [Fact]
        public void Bicycle_PedestrianShouldGiveZeroFactor()
        {
            var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var factor = profile.Factor(new AttributeCollection(
                new Attribute("highway", "pedestrian")));
            
            Assert.True(factor.BackwardFactor == 0);
            Assert.True(factor.ForwardFactor == 0);
        }
        
        [Fact]
        public void Bicycle_OneWayShouldGiveZeroBackwardFactor()
        {
            var profile = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var factor = profile.Factor(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("oneway", "yes")));
            
            Assert.True(factor.BackwardFactor == 0);
            Assert.True(factor.ForwardFactor != 0);
        }
    }
}