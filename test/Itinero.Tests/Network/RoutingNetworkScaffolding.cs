using System.Collections.Generic;
using Itinero.Network;

namespace Itinero.Tests.Network {
    public static class RoutingNetworkScaffolding {
        public static (VertexId[] vertices, EdgeId[] edges) Write(this RoutingNetwork network,
            (double longitude, double latitude, float? e)[] vertices,
            (int from, int to)[]? edges = null) {
            var fullEdges =
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                    IEnumerable<(string key, string attribute)>? attributes)[edges?.Length ?? 0];

            if (edges != null) {
                for (var e = 0; e < fullEdges.Length; e++) {
                    fullEdges[e] = (edges[e].from, edges[e].to, null, null);
                }
            }

            return network.Write(vertices, fullEdges);
        }

        public static (VertexId[] vertices, EdgeId[] edges) Write(this RoutingNetwork network,
            (double longitude, double latitude, float? e)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]? edges) {
            var fullEdges =
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                    IEnumerable<(string key, string attribute)>? attributes)[edges?.Length ?? 0];

            if (edges != null) {
                for (var e = 0; e < fullEdges.Length; e++) {
                    fullEdges[e] = (edges[e].from, edges[e].to, edges[e].shape, null);
                }
            }

            return network.Write(vertices, fullEdges);
        }

        public static (VertexId[] vertices, EdgeId[] edges) Write(this RoutingNetwork network,
            (double longitude, double latitude, float? e)[] vertices,
            (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                IEnumerable<(string key, string attribute)>? attributes)[]? edges) {
            edges ??=
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
                    IEnumerable<(string key, string attribute)>? attributes)[0];
            using var writer = network.GetWriter();
            var vertexIds = new VertexId[vertices.Length];
            for (var v = 0; v < vertices.Length; v++) {
                vertexIds[v] = writer.AddVertex(vertices[v].longitude, vertices[v].latitude, (float?) null);
            }

            var edgeIds = new EdgeId[edges.Length];
            for (var e = 0; e < edges.Length; e++) {
                edgeIds[e] = writer.AddEdge(vertexIds[edges[e].from], vertexIds[edges[e].to],
                    edges[e].shape, edges[e].attributes);
            }

            return (vertexIds, edgeIds);
        }
    }
}