// using System.Collections.Generic;
// using Itinero.IO.Osm.Tiles;
// using Itinero.Profiles;
// using Itinero.Serialization;
//
// namespace Itinero.Tests.Functional.Tests
// {
//     public sealed class
//         RouterDbDeserializeTest : FunctionalTest<RouterDb, (IReadOnlyList<Profile> profiles, string path)>
//     {
//         protected override RouterDb Execute(
//             (IReadOnlyList<Profile> profiles, string path) input)
//         {
//             return RouterDb.Deserialize(c =>
//             {
//                 c.Path = input.path;
//                 c.UseDefaultProfileSerializer(pc =>
//                 {
//                     foreach (var p in input.profiles)
//                     {
//                         pc.Use(p);
//                     }
//                 });
//                 c.RegisterDataProvider();
//             });
//         }
//
//         public static readonly RouterDbDeserializeTest Default = new RouterDbDeserializeTest();
//     }
// }

