using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Tests.Indexes;
using Xunit;

namespace Itinero.Tests.Network;

public class NetworkTests
{
    [Fact]
    public void RoutingNetwork_SetEdgeTypeMap_AddEdge_NewType_ShouldAdd()
    {
        var attributeSetMap = new AttributeSetMapMock(Guid.NewGuid(), a => { return a.Where(x => x.key == "highway"); });
        var provider = TestProvider.New(attributeSetMap);

        var (vertices, edges) = provider.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
            IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, Array.Empty<(double longitude, double latitude, float? e)>(),
                        new (string key, string value)[] {("highway", "residential")})
            });

        var network = provider.RouterDb.Latest;
        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(edges[0]);

        Assert.Equal(1U, enumerator.EdgeTypeId);
    }

    [Fact]
    public void RoutingNetwork_EdgeType_AddEdge_ExistingType_ShouldGet()
    {
        var attributeSetMap = new AttributeSetMapMock(Guid.NewGuid(), a => { return a.Where(x => x.key == "highway"); });
        var provider = TestProvider.New(attributeSetMap);

        var (vertices, edges) = provider.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
            IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, Array.Empty<(double longitude, double latitude, float? e)>(),
                        new (string key, string value)[] {("highway", "residential")}),
                    (0, 2, Array.Empty<(double longitude, double latitude, float? e)>(),
                        new (string key, string value)[] {("highway", "residential")})
            });

        var network = provider.RouterDb.Latest;
        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(edges[1]);

        Assert.Equal(1U, enumerator.EdgeTypeId);
    }

    [Fact]
    public void RoutingNetwork_EdgeType_AddEdge_SecondType_ShouldAdd()
    {
        var attributeSetMap = new AttributeSetMapMock(Guid.NewGuid(), a => { return a.Where(x => x.key == "highway"); });
        var provider = TestProvider.New(attributeSetMap);

        var (vertices, edges) = provider.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
            IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, Array.Empty<(double longitude, double latitude, float? e)>(),
                        new (string key, string value)[] {("highway", "residential")}),
                    (0, 2, Array.Empty<(double longitude, double latitude, float? e)>(),
                        new (string key, string value)[] {("highway", "primary")})
            });

        var network = provider.RouterDb.Latest;
        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(edges[1]);

        Assert.Equal(2U, enumerator.EdgeTypeId);
    }

}
