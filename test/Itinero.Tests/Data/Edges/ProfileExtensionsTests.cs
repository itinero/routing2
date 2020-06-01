// using System.Collections.Generic;
// using System.Linq;
// using Itinero.Data.Edges;
// using Itinero.Profiles;
// using Xunit;
//
// namespace Itinero.Tests.Data.Edges
// {
//     public class ProfileExtensionsTests
//     {
//         public class MockProfile : Profile
//         {
//             private readonly HashSet<string> _keys;
//
//             public MockProfile(params string[] keys)
//             {
//                 _keys = new HashSet<string>(keys);
//             }
//
//             public override string Name { get; } = "MockProfile";
//             
//             public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
//             {
//                 ushort factor = 0;
//
//                 foreach (var (key, _) in attributes)
//                 {
//                     if (_keys.Contains(key)) factor++;
//                 }
//
//                 return new EdgeFactor(factor, factor, factor, factor, true);
//             }
//         }
//         
//         [Fact]
//         public void Test_EdgeProfile_AllKeysUsed_ShouldReturnAll()
//         {
//             var profile = new MockProfile("highway", "maxspeed");
//
//             var edgeProfile = profile.GetEdgeProfileFor(new (string key, string value)[]
//             {
//                 ("highway", "residential"),
//                 ("maxspeed", "50")
//             }).ToArray();
//             
//             Assert.NotNull(edgeProfile);
//             Assert.Equal(2, edgeProfile.Length);
//             Assert.Equal(("highway", "residential"), edgeProfile[0]);
//             Assert.Equal(("maxspeed", "50"), edgeProfile[1]);
//         }
//         
//         [Fact]
//         public void Test_EdgeProfile_OneKeysUsed_ShouldReturnOneKey()
//         {
//             var profile = new MockProfile("highway");
//
//             var edgeProfile = profile.GetEdgeProfileFor(new (string key, string value)[]
//             {
//                 ("highway", "residential"),
//                 ("maxspeed", "50")
//             }).ToArray();
//             
//             Assert.NotNull(edgeProfile);
//             Assert.Single(edgeProfile);
//             Assert.Equal(("highway", "residential"), edgeProfile[0]);
//         }
//     }
// }