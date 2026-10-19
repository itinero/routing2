using System.Linq;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Data.Providers;

/// <summary>
/// An edge written one at a time, with no way behind it, still reports its own attributes and
/// shape — they come from the provider rather than the tile, which is what the writer records.
/// </summary>
public class DefaultDataProviderWriteTests
{
    [Fact]
    public void AnEdgeWrittenWithAShape_ShouldReportIt()
    {
        var provider = TestProvider.New();
        EdgeId edge;
        using (var writer = provider.BeginWrite())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, new[] {
                (4.795167446136475, 51.26580191532799, (float?)null)
            });
        }

        var enumerator = provider.RouterDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(edge);

        var shape = enumerator.Shape.ToList();
        Assert.Single(shape);
        Assert.Equal(4.795167446136475, shape[0].longitude, 4);
        Assert.Equal(51.26580191532799, shape[0].latitude, 4);
    }

    [Fact]
    public void AnEdgeWrittenWithAttributes_ShouldReportThem()
    {
        var provider = TestProvider.New();
        EdgeId edge;
        using (var writer = provider.BeginWrite())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2,
                attributes: new[] { ("highway", "residential") });
        }

        var attributes = provider.RouterDb.Latest.GetAttributes(edge).ToList();
        Assert.Single(attributes);
        Assert.Equal("highway", attributes[0].key);
        Assert.Equal("residential", attributes[0].value);
    }

    [Fact]
    public void AnEdgeWrittenBackwards_ShouldReportItsShapeReversed()
    {
        var provider = TestProvider.New();
        EdgeId edge;
        using (var writer = provider.BeginWrite())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, new[] {
                (4.794, 51.2655, (float?)null),
                (4.796, 51.2661, (float?)null)
            });
        }

        var enumerator = provider.RouterDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(edge, false);

        // the provider answers in the written direction; reading the edge the other way round
        // has to reverse it, or a route materialises its geometry backwards.
        var shape = enumerator.Shape.ToList();
        Assert.Equal(2, shape.Count);
        Assert.Equal(4.796, shape[0].longitude, 4);
        Assert.Equal(4.794, shape[1].longitude, 4);
    }
}
