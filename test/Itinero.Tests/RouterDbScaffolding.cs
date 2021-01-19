using System.Collections.Generic;
using Itinero.Network;
namespace Itinero.Tests
{
    internal static class RouterDbScaffolding
    {
        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb((double longitude, double latitude, float? e)[] vertices, 
            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] edges)
        {            
            var routerDb = new RouterDb();

            using var writer = routerDb.GetMutableNetwork();
            var vertexIds = new VertexId[vertices.Length];
            for (var v = 0; v < vertices.Length; v++)
            {
                vertexIds[v] = writer.AddVertex(vertices[v].longitude, vertices[v].latitude, vertices[v].e);
            }

            var edgeIds = new EdgeId[edges.Length];
            for (var e = 0; e < edges.Length; e++)
            {
                edgeIds[e] = writer.AddEdge(vertexIds[edges[e].from], vertexIds[edges[e].to], edges[e].shape);
            }

            return (routerDb,vertexIds,edgeIds);
        }
    }
}