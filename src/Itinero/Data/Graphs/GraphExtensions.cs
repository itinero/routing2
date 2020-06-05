using System;

namespace Itinero.Data.Graphs
{
    internal static class GraphExtensions
    {
        public static Graph Mutate(this Graph graph, Action<IMutableGraph> mutate)
        {
            using var mutable = graph.GetAsMutable();
            mutate(mutable);
            return mutable.ToGraph();
        }

        public static void Write(this Graph graph, Action<Graph.GraphWriter> write)
        {
            using var writer = graph.GetWriter();
            write(writer);
        }

        public static (double longitude, double latitude) GetVertex(this Graph graph, VertexId vertexId)
        {
            if (!graph.TryGetVertex(vertexId, out var longitude, out var latitude)) throw new ArgumentOutOfRangeException(nameof(vertexId), "Vertex not found!");

            return (longitude, latitude);
        }
    }
}