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
}
