using System.Linq;
using Itinero.IO.Osm.Restrictions.Barriers;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions.Barriers;

public class OsmBarrierParserTests
{
    [Fact]
    public void OsmBarrierParser_IsBarrier_WhenBarrier_ShouldReturnTrue()
    {
        var node = new Node()
        {
            Id = 1,
            Version = 1,
            Longitude = 4.80177104473114,
            Latitude = 51.26888649155825,
            Tags = new TagsCollection(new Tag("barrier", "bollard"))
        };

        var parser = new OsmBarrierParser();
        var result = parser.IsBarrier(node);

        Assert.True(result);
    }

    [Fact]
    public void OsmBarrierParser_IsBarrier_WhenNotBarrier_ShouldReturnFalse()
    {
        var node = new Node()
        {
            Id = 1,
            Version = 1,
            Longitude = 4.80177104473114,
            Latitude = 51.26888649155825,
            Tags = new TagsCollection(new Tag("not_a_barrier", "bollard"))
        };

        var parser = new OsmBarrierParser();
        var result = parser.IsBarrier(node);

        Assert.False(result);
    }
}
