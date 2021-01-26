//using System.IO;
//using Itinero.IO.Osm.Tiles;
//using Xunit;
//

// TODO: improve testability of the data provider.

//namespace Itinero.Tests.IO.Osm.Tiles
//{
//    public class DataProviderTests
//    {
//        [Fact]
//        public void DataProvider_ReadWriteShouldCopy()
//        {
//            var db = new RouterDb();
//            var globalId = new DataProvider(db);
//            globalId.TouchBox((4.34974, 50.86608, 4.34974, 50.86608));
//
//            using (var stream = new MemoryStream())
//            {
//                globalId.WriteTo(stream);
//
//                stream.Seek(0, SeekOrigin.Begin);
//
//                var copy = GlobalIdMap.ReadFrom(stream);
//                
//                Assert.True(globalId.TryGet(808034, out var vertex));
//                Assert.Equal((uint)80912, vertex.TileId);
//                Assert.Equal((uint)184, vertex.LocalId);
//                Assert.True(globalId.TryGet(808035, out vertex));
//                Assert.Equal((uint)80915, vertex.TileId);
//                Assert.Equal((uint)1823, vertex.LocalId);
//            }
//        }
//    }
//}

