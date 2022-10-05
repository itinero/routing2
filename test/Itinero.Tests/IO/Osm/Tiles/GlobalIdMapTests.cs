using System.IO;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.IO.Osm.Tiles;

public class GlobalIdMapTests
{
    [Fact]
    public void GlobalIdMap_ReadWriteShouldCopy()
    {
        var globalId = new GlobalIdMap();
        globalId.Set(808034, new VertexId(80912, 184));
        globalId.Set(808035, new VertexId(80915, 1823));

        using (var stream = new MemoryStream())
        {
            globalId.WriteTo(stream);

            stream.Seek(0, SeekOrigin.Begin);

            var copy = GlobalIdMap.ReadFrom(stream);

            Assert.True(globalId.TryGet(808034, out var vertex));
            Assert.Equal((uint)80912, vertex.TileId);
            Assert.Equal((uint)184, vertex.LocalId);
            Assert.True(globalId.TryGet(808035, out vertex));
            Assert.Equal((uint)80915, vertex.TileId);
            Assert.Equal((uint)1823, vertex.LocalId);
        }
    }
}
