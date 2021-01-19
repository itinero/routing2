using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Enumerators.Edges;
using Xunit;

namespace Itinero.Tests.Network.Enumerators.Edges
{
    public class IEdgeEnumeratorExtensionsTests
    {
        [Fact]
        public void IEdgeEnumeratorExtensions_GetCompleteShape_NoShape_Forward_ShouldReturnVertices()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[]
                {
                    (4.801073670387268, 51.268064181900094, (float?)null),
                    (4.801771044731140, 51.268886491558250, (float?)null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
                {
                    (0, 1, System.Array.Empty<(double longitude, double latitude, float? e)>())
                });

            var network = routerDb.Latest;

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[0]);
            enumerator.MoveNext();
            var shape = enumerator.GetCompleteShape().ToArray();
            Assert.Equal(2, shape.Length);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[0]);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[1]);
        }
        [Fact]
        public void IEdgeEnumeratorExtensions_GetCompleteShape_NoShape_Backward_ShouldReturnVertices()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[]
                {
                    (4.801073670387268, 51.268064181900094, (float?)null),
                    (4.801771044731140, 51.268886491558250, (float?)null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
                {
                    (0, 1, System.Array.Empty<(double longitude, double latitude, float? e)>())
                });

            var network = routerDb.Latest;

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[1]);
            enumerator.MoveNext();
            var shape = enumerator.GetCompleteShape().ToArray();
            Assert.Equal(2, shape.Length);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[0]);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[1]);
        }
        
        [Fact]
        public void IEdgeEnumeratorExtensions_GetCompleteShape_WithShape_Forward_ShouldReturnVerticesAndShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[]
                {
                    (4.801073670387268, 51.268064181900094, (float?)null),
                    (4.801771044731140, 51.268886491558250, (float?)null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude, float? e)[]
                    {
                        (4.800950288772583, 51.268426671236426, (float?)null),
                        (4.801242649555205, 51.268816008449830, (float?)null)
                    }),
                });
        
            var network = routerDb.Latest;
        
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[0]);
            enumerator.MoveNext();
            var shape = enumerator.GetCompleteShape().ToArray();
            Assert.Equal(4, shape.Length);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[0]);
            ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426, (float?)null), shape[1]);
            ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830, (float?)null), shape[2]);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[3]);
        }
        
        [Fact]
        public void IEdgeEnumeratorExtensions_GetCompleteShape_WithShape_Backward_ShouldReturnVerticesAndShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[]
                {
                    (4.801073670387268, 51.268064181900094, (float?)null),
                    (4.801771044731140, 51.268886491558250, (float?)null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude, float? e)[]
                    {
                        (4.800950288772583, 51.268426671236426, (float?)null),
                        (4.801242649555205, 51.268816008449830, (float?)null)
                    }),
                });
        
            var network = routerDb.Latest;
        
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[1]);
            enumerator.MoveNext();
            var shape = enumerator.GetCompleteShape().ToArray();
            Assert.Equal(4, shape.Length);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[0]);
            ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830, (float?)null), shape[1]);
            ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426, (float?)null), shape[2]);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[3]);
        }
    }
}