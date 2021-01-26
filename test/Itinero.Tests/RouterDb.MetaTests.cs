using System.IO;
using System.Linq;
using Xunit;

namespace Itinero.Tests
{
    public class RouterDb_MetaTests
    {
        [Fact]
        public void RouterDb_Meta_Add1_ShouldAdd1()
        {
            var routerDb = new RouterDb();

            routerDb.Meta.Add(("source", "OpenStreetMap"));

            Assert.Single(routerDb.Meta);
            Assert.Equal(("source", "OpenStreetMap"), routerDb.Meta.First());
        }

        [Fact]
        public void RouterDb_Meta_WriteRead_1Attribute_ShouldHaveMetaAfter()
        {
            var routerDb = new RouterDb();

            routerDb.Meta.Add(("source", "OpenStreetMap"));

            // write
            var stream = new MemoryStream();
            routerDb.WriteTo(stream);

            // read.
            stream.Seek(0, SeekOrigin.Begin);
            var read = RouterDb.ReadFrom(stream);

            Assert.Single(read.Meta);
            Assert.Equal(("source", "OpenStreetMap"), read.Meta.First());
        }
    }
}