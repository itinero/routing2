// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using Itinero.IO.Osm.Tiles;
// using Itinero.Profiles;
// using Itinero.Serialization;
//
// namespace Itinero.Tests.Functional.Tests
// {
//     public sealed class RouterDbSerializeDeserializeTest : FunctionalTest<RouterDb, RouterDb>
//     {
//         protected override RouterDb Execute(
//             RouterDb input)
//         {
//             var profiles = input.PreparedProfiles().ToList();
//             var fileName = $"{Guid.NewGuid().ToString()}.routerdb";
//
//             try
//             {
//                 input.Serialize(settings => { settings.Path = fileName; });
//
//                 return RouterDb.Deserialize(c =>
//                 {
//                     c.Path = fileName;
//                     c.UseDefaultProfileSerializer(pc =>
//                     {
//                         foreach (var p in profiles)
//                         {
//                             pc.Use(p);
//                         }
//                     });
//                     c.RegisterDataProvider();
//                 });
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 throw;
//             }
//             finally
//             {
//                 if (File.Exists(fileName)) File.Delete(fileName);
//             }
//         }
//         
//         public static readonly RouterDbSerializeDeserializeTest Default = new RouterDbSerializeDeserializeTest();
//     }
// }