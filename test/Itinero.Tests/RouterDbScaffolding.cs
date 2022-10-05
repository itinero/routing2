using System.Collections.Generic;
using System.Linq;
using Itinero.Network;

namespace Itinero.Tests
{
    internal static class RouterDbScaffolding
    {
        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb(
            (double longitude, double latitude, float? e)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                List<(string, string)> attributes)[] edges)
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
                edgeIds[e] = writer.AddEdge(vertexIds[edges[e].from], vertexIds[edges[e].to], edges[e].shape,
                    edges[e].attributes);
            }

            return (routerDb, vertexIds, edgeIds);
        }

        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb(
            (double longitude, double latitude, float? e)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] edges)
        {
            return BuildRouterDb(vertices,
                edges.Select(v => (v.from, v.to, v.shape, new List<(string, string)>())).ToArray());
        }

        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb(
            (double longitude, double latitude)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude)>? shape)[] edges)
        {
            var edgesWithAttributes = edges.Select(e => (e.from, e.to, e.shape, new List<(string, string)>()));
            return BuildRouterDb(vertices, edgesWithAttributes.ToArray());
        }

        public static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildRouterDb(
            (double longitude, double latitude)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude)>? shape,
                List<(string, string)> attributes)[] edges)
        {
            (double longitude, double latitude, float? e)[] verticesArr =
                vertices.Select(v => (v.longitude, v.latitude, (float?)0f)).ToArray();


            var edgesWithE =
                new List<(int from, int to, IEnumerable<(double longitutde, double latitude, float? e)>? shape,
                    List<(string, string)> attributes)>();

            foreach (var edge in edges)
            {
                edgesWithE.Add((
                    edge.from, edge.to,
                    edge.shape.Select(c => (c.longitude, c.latitude, (float?)0f)).ToList(),
                    edge.attributes
                ));
            }

            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                List<(string, string)> attributes)[] edgesArr = edgesWithE.ToArray();

            return BuildRouterDb(
                verticesArr,
                edgesArr
            );
        }
    }
}
