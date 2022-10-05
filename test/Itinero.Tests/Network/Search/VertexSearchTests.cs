using System.Linq;
using Itinero.Network;
using Itinero.Network.Search;
using Xunit;

namespace Itinero.Tests.Network.Search
{
    public class VertexSearchTests
    {
        [Fact]
        public void VertexSearch_SearchVertexInBox_ShouldReturnOnlyVertexInBox()
        {
            var routerDb = new RouterDb();
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetMutableNetwork())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            }

            var vertices =
                routerDb.Latest.SearchVerticesInBox(((4.796, 51.267, (float?)null), (4.798, 51.265, (float?)null)));
            Assert.NotNull(vertices);

            var verticesList = vertices.ToList();
            Assert.Single(verticesList);
            Assert.Equal(vertex2, verticesList[0].vertex);
        }
    }
}
