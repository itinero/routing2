using System.IO;
using System.Linq;
using Xunit;

namespace Itinero.Tests;

public class RouterDb_MetaTests
{
    [Fact]
    public void RouterDb_Meta_Add1_ShouldAdd1()
    {
        var provider = TestProvider.New();
        var routerDb = provider.RouterDb;

        routerDb.Meta.Add(("source", "OpenStreetMap"));

        Assert.Single(routerDb.Meta);
        Assert.Equal(("source", "OpenStreetMap"), routerDb.Meta.First());
    }

    [Fact]
    public void RouterDb_Meta_WriteRead_1Attribute_ShouldHaveMetaAfter()
    {
        var provider = TestProvider.New();
        var routerDb = provider.RouterDb;

        routerDb.Meta.Add(("source", "OpenStreetMap"));

        // write
        var stream = new MemoryStream();
        routerDb.WriteTo(stream);

        // read.
        stream.Seek(0, SeekOrigin.Begin);
        var readProvider = TestProvider.New();
        readProvider.LoadFrom(stream);
        var read = readProvider.RouterDb;

        Assert.Single(read.Meta);
        Assert.Equal(("source", "OpenStreetMap"), read.Meta.First());
    }
}
