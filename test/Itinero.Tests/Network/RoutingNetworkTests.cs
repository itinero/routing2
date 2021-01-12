using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;
using Itinero.Network;
using Itinero.Tests.Indexes;
using Xunit;

namespace Itinero.Tests.Network
{
    public class NetworkTests
    {
        [Fact]
         public void RoutingNetworkEnumerator_EdgeWithShape_ShouldEnumerateShapeForward()
         {
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
             {
                 (4.800467491149902,51.26896368721961),
                 (4.801111221313477,51.26676859478893)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
             {
                 (0, 1, new (double longitude, double latitude)[]
                 {
                     (4.800703525543213, 51.26832598004091),
                     (4.801368713378906, 51.26782252075405)
                 })
             });

             var enumerator = network.GetEdgeEnumerator();
             Assert.True(enumerator.MoveTo(vertices[0]));
             Assert.True(enumerator.MoveNext());

             var shape = enumerator.Shape.ToArray();
             Assert.Equal(2, shape.Length);
             Assert.Equal(4.800703525543213, shape[0].longitude, 4);
             Assert.Equal(51.26832598004091, shape[0].latitude, 4);
             Assert.Equal(4.801368713378906, shape[1].longitude, 4);
             Assert.Equal(51.26782252075405, shape[1].latitude, 4);
         }
         
         [Fact]
         public void RoutingNetwork_RoutingNetworkEdgeEnumerator_ShouldEnumerateEdgesInRoutingNetwork()
         {
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
             {
                 (4.792613983154297, 51.26535213392538),
                 (4.797506332397461, 51.26674845584085)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
             {
                 (0, 1,null)
             });

             var enumerator = network.GetEdgeEnumerator();
             enumerator.MoveTo(vertices[0]);
             Assert.True(enumerator.MoveNext());
             Assert.Equal(vertices[0], enumerator.From);
             Assert.Equal(vertices[1], enumerator.To);
             Assert.True(enumerator.Forward);
         }
         
         [Fact]
         public void RoutingNetworkEnumerator_EdgeWithShape_ShouldEnumerateShapeBackward()
         {
             var network = new RoutingNetwork(new RouterDb());
             var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
             {
                 (4.800467491149902,51.26896368721961),
                 (4.801111221313477,51.26676859478893)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
             {
                 (0, 1, new (double longitude, double latitude)[]
                 {
                     (4.800703525543213, 51.26832598004091),
                     (4.801368713378906, 51.26782252075405)
                 })
             });

             var enumerator = network.GetEdgeEnumerator();
             Assert.True(enumerator.MoveTo(vertices[1]));
             Assert.True(enumerator.MoveNext());

             var shape = enumerator.Shape.ToArray();
             Assert.Equal(2, shape.Length);
             Assert.Equal(4.800703525543213, shape[1].longitude, 4);
             Assert.Equal(51.26832598004091, shape[1].latitude, 4);
             Assert.Equal(4.801368713378906, shape[0].longitude, 4);
             Assert.Equal(51.26782252075405, shape[0].latitude, 4);
         }

         [Fact]
         public void RoutingNetwork_SetEdgeTypeMap_AddEdge_NewType_ShouldAdd()
         {
             var routerDb = new RouterDb();
             var network = new RoutingNetwork(routerDb);

             var attributeSetMap = new AttributeSetMapMock(102948, a =>
             {
                 return a.Where(x => x.key == "highway");
             });
             routerDb.SetEdgeTypeMap(attributeSetMap);
             
             var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
             {
                 (4.800467491149902,51.26896368721961),
                 (4.801111221313477,51.26676859478893)
             }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
             {
                 (0, 1, new (double longitude, double latitude)[]
                 {
                     (4.800703525543213, 51.26832598004091),
                     (4.801368713378906, 51.26782252075405)
                 })
             });
             
             VertexId vertex1, vertex2;
             EdgeId edge;
             using (var writer = network.GetWriter())
             {
                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
                 vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
                 edge = writer.AddEdge(vertex1, vertex2,
                     attributes: new (string key, string value)[] {("highway", "residential")});
             }
             
             var enumerator = network.GetEdgeEnumerator();
             enumerator.MoveToEdge(edge);
             
             Assert.Equal(0U, enumerator.EdgeTypeId);
         }

//         [Fact]
//         public void RoutingNetwork_EdgeType_AddEdge_ExistingType_ShouldGet()
//         {
//             var routerDb = new RouterDb();
//             var network = new RoutingNetwork(routerDb);
//
//             var attributeSetMap = new AttributeSetMap(102948, a =>
//             {
//                 return a.Where(x => x.key == "highway");
//             });
//             routerDb.SetEdgeTypeMap(attributeSetMap);
//             
//             VertexId vertex1, vertex2, vertex3;
//             EdgeId edge;
//             using (var writer = network.GetWriter())
//             {
//                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
//                 vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
//                 vertex3 = writer.AddVertex(4.801111221313477,51.26676859478893);
//                 writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[] { ("highway", "residential") });
//                 edge = writer.AddEdge(vertex1, vertex3,
//                     attributes: new (string key, string value)[] {("highway", "residential")});
//             }
//             
//             var enumerator = network.GetEdgeEnumerator();
//             enumerator.MoveToEdge(edge);
//             
//             Assert.Equal(0U, enumerator.EdgeTypeId);
//         }
//
//         [Fact]
//         public void RoutingNetwork_EdgeType_AddEdge_SecondType_ShouldAdd()
//         {
//             var routerDb = new RouterDb();
//             var network = new RoutingNetwork(routerDb);
//
//             var attributeSetMap = new AttributeSetMap(102948, a =>
//             {
//                 return a.Where(x => x.key == "highway");
//             });
//             routerDb.SetEdgeTypeMap(attributeSetMap);
//             
//             VertexId vertex1, vertex2, vertex3;
//             EdgeId edge;
//             using (var writer = network.GetWriter())
//             {
//                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
//                 vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
//                 vertex3 = writer.AddVertex(4.801111221313477,51.26676859478893);
//                 writer.AddEdge(vertex1, vertex2,
//                     attributes: new (string key, string value)[] {("highway", "residential")});
//                 edge = writer.AddEdge(vertex1, vertex3,
//                     attributes: new (string key, string value)[] {("highway", "primary")});
//             }
//             
//             var enumerator = network.GetEdgeEnumerator();
//             enumerator.MoveToEdge(edge);
//             
//             Assert.Equal(1U, enumerator.EdgeTypeId);
//         }
//
//         [Fact]
//         public void RoutingNetwork_EdgeType_AddEdge_NewEdgeTypeFunc_ShouldUpdateEdgeTypeId()
//         {
//             var routerDb = new RouterDb();
//             var network = new RoutingNetwork(routerDb);
//
//             var attributeSetMap = new AttributeSetMap(102948, a =>
//             {
//                 return a.Where(x => x.key == "highway");
//             });
//             routerDb.SetEdgeTypeMap(attributeSetMap);
//             
//             VertexId vertex1, vertex2;
//             EdgeId edge;
//             using (var writer = network.GetWriter())
//             {
//                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
//                 vertex2 = writer.AddVertex(4.801111221313477,51.26676859478893);
//
//                 edge = writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
//                 {
//                     ("highway", "residential"),
//                     ("maxspeed", "50")
//                 });
//             }
//
//             // update edge type func.
//             attributeSetMap = new AttributeSetMap(102948, a =>
//             {
//                 return a.Where(x => x.key == "highway" || x.key == "maxspeed");
//             });
//             routerDb.SetEdgeTypeMap(attributeSetMap);
//             
//             var enumerator = network.GetEdgeEnumerator();
//             enumerator.MoveToEdge(edge);
//             
//             Assert.Equal(1U, enumerator.EdgeTypeId);
//         }
//
//         [Fact]
//         public void RoutingNetwork_VertexEnumerator_Empty_ShouldNotReturnVertices()
//         {
//             var network = new RoutingNetwork(new RouterDb());
//
//             var enumerator = network.GetVertexEnumerator();
//             Assert.False(enumerator.MoveNext());
//         }
//
//         [Fact]
//         public void RoutingNetwork_VertexEnumerator_OneVertex_ShouldReturnOneVertex()
//         {
//             var network = new RoutingNetwork(new RouterDb());
//             VertexId vertex;
//             using (var writer = network.GetWriter())
//             {
//                 vertex = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
//             }
//
//             var enumerator = network.GetVertexEnumerator();
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(vertex, enumerator.Current);
//         }
//
//         [Fact]
//         public void RoutingNetwork_VertexEnumerator_TwoVertices_ShouldReturnTwoVertices()
//         {
//             var network = new RoutingNetwork(new RouterDb());
//             VertexId vertex1, vertex2;
//             using (var writer = network.GetWriter())
//             {
//                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
//                 vertex2 = writer.AddVertex(4.800467491149902,51.26896368721961);
//             }
//
//             var enumerator = network.GetVertexEnumerator();
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(vertex1, enumerator.Current);
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(vertex2, enumerator.Current);
//         }
//
//         [Fact]
//         public void RoutingNetwork_VertexEnumerator_TwoVertices_DifferentTiles_ShouldReturnTwoVertices()
//         {
//             var network = new RoutingNetwork(new RouterDb());
//             VertexId vertex1, vertex2;
//             using (var writer = network.GetWriter())
//             {
//                 vertex1 = writer.AddVertex(4.800467491149902,51.26896368721961);
//                 vertex2 = writer.AddVertex(5.801111221313477,51.26676859478893);
//             }
//
//             var enumerator = network.GetVertexEnumerator();
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(vertex1, enumerator.Current);
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(vertex2, enumerator.Current);
//         }
    }
}