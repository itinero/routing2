using Itinero.Profiles.EdgeTypesMap;
using Itinero.Profiles.Lua.Osm;
using Xunit;

namespace Itinero.Tests.Profiles.EdgeTypesMap
{
    public class ProfilesEdgeTypeMapTests
    {
        [Fact]
        public void ProfilesEdgeTypeMap_Bicycle_Mapping_Residential_ShouldReturnResidential()
        {
            var bicycle = OsmProfiles.Bicycle;

            var profilesEdgeMap = new ProfilesEdgeTypeMap(new[] {bicycle});

            var tags = new[] {("highway", "residential")};
            var t = profilesEdgeMap.Mapping(tags);
            
            Assert.Equal(tags, t);
        }
    }
}