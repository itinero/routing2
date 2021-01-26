using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Tests.Indexes;
using Xunit;

namespace Itinero.Tests.Network {
    public class NetworkTests {
        [Fact]
        public void RoutingNetwork_SetEdgeTypeMap_AddEdge_NewType_ShouldAdd() {
            var routerDb = new RouterDb();
            var network = new RoutingNetwork(routerDb);

            var attributeSetMap = new AttributeSetMapMock(102948, a => { return a.Where(x => x.key == "highway"); });
            routerDb.SetEdgeTypeMap(attributeSetMap);

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0],
                        new (string key, string value)[] {("highway", "residential")})
                });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges[0]);

            Assert.Equal(1U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_ExistingType_ShouldGet() {
            var routerDb = new RouterDb();
            var network = new RoutingNetwork(routerDb);

            var attributeSetMap = new AttributeSetMapMock(102948, a => { return a.Where(x => x.key == "highway"); });
            routerDb.SetEdgeTypeMap(attributeSetMap);

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0],
                        new (string key, string value)[] {("highway", "residential")}),
                    (0, 2, new (double longitude, double latitude, float? e)[0],
                        new (string key, string value)[] {("highway", "residential")})
                });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges[1]);

            Assert.Equal(1U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_SecondType_ShouldAdd() {
            var routerDb = new RouterDb();
            var network = new RoutingNetwork(routerDb);

            var attributeSetMap = new AttributeSetMapMock(102948, a => { return a.Where(x => x.key == "highway"); });
            routerDb.SetEdgeTypeMap(attributeSetMap);

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0],
                        new (string key, string value)[] {("highway", "residential")}),
                    (0, 2, new (double longitude, double latitude, float? e)[0],
                        new (string key, string value)[] {("highway", "primary")})
                });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges[1]);

            Assert.Equal(2U, enumerator.EdgeTypeId);
        }

        [Fact]
        public void RoutingNetwork_EdgeType_AddEdge_NewEdgeTypeFunc_ShouldUpdateEdgeTypeId() {
            var routerDb = new RouterDb();
            var network = new RoutingNetwork(routerDb);

            // first don't include maxspeed.
            var attributeSetMap = new AttributeSetMapMock(102948, a => { return a.Where(x => x.key == "highway"); });
            routerDb.SetEdgeTypeMap(attributeSetMap);
            network = routerDb.Latest;

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.801111221313477, 51.26676859478893, (float?) null)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                IEnumerable<(string key, string value)>? attributes)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0], new (string key, string value)[] {
                        ("highway", "residential"),
                        ("maxspeed", "50")
                    })
                });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges[0]);
            Assert.Equal(1U, enumerator.EdgeTypeId);

            // update edge type func to include maxspeed.
            attributeSetMap = new AttributeSetMapMock(1029445,
                a => { return a.Where(x => x.key == "highway" || x.key == "maxspeed"); });
            routerDb.SetEdgeTypeMap(attributeSetMap);
            network = routerDb.Latest;

            enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges[0]);

            Assert.Equal(2U, enumerator.EdgeTypeId);
        }
    }
}