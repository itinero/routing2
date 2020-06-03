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
    }
}