// using System.Collections.Generic;
// using Itinero.Profiles;
//
// namespace Itinero.Tests.Functional.Tests
// {
//     public sealed class RouterDbSerializeTest : FunctionalTest<object?, (RouterDb routerDb, string path)>
//     {
//         protected override object? Execute(
//             (RouterDb routerDb, string path) input)
//         {
//             input.routerDb.Serialize(settings =>
//             {
//                 settings.Path = input.path;
//             });
//
//             return null;
//         }
//         
//         public static readonly RouterDbSerializeTest Default = new RouterDbSerializeTest();
//     }
// }

