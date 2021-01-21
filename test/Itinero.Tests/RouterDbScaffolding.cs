using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
namespace Itinero.Tests
{
    internal static class RouterDbScaffolding
    {
        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb(
            (double longitude, double latitude)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                attributes)[] edges) {
            var routerDb = new RouterDb();

            using var writer = routerDb.GetMutableNetwork();
            var vertexIds = new VertexId[vertices.Length];
            for (var v = 0; v < vertices.Length; v++)
            {
                vertexIds[v] = writer.AddVertex(vertices[v].longitude, vertices[v].latitude);
            }

            var edgeIds = new EdgeId[edges.Length];
            for (var e = 0; e < edges.Length; e++)
            {
                edgeIds[e] = writer.AddEdge(vertexIds[edges[e].from], vertexIds[edges[e].to], edges[e].shape, edges[e].attributes);
            }

            return (routerDb,vertexIds,edgeIds);
        }

        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb((double longitude, double latitude)[] vertices, 
            (int from, int to, IEnumerable<(double longitude, double latitude)>? shape)[] edges) {
            return BuildRouterDb(vertices, edges.Select(v => (v.from, v.to, v.shape, new List<(string, string)>())).ToArray());
        }
    }
}