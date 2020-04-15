using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Dijkstra
{
    internal static class PathTreeExtensions
    {
        /// <summary>
        /// Adds a new visit the path tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="vertex">The vertex.</param>
        /// <param name="edge">The edge.</param>
        /// <param name="previousPointer">The pointer to the previous entry.</param>
        /// <returns>A pointer to the visit.</returns>
        public static uint AddVisit(this PathTree tree, VertexId vertex, EdgeId edge, uint previousPointer)
        {
            var data0 = vertex.TileId;
            var data1 = vertex.LocalId;
            var data2 = edge.TileId;
            var data3 = edge.LocalId;
            var data4 = previousPointer;

            return tree.Add(data0, data1, data2, data3, data4);
        }

        /// <summary>
        /// Gets the visit at the given location.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="pointer">The pointer.</param>
        /// <returns>The visit.</returns>
        public static (VertexId vertex, EdgeId edge, uint previousPointer) GetVisit(this PathTree tree, uint pointer)
        {
            tree.Get(pointer, out var data0, out var data1, out var data2, out var data3, out var data4);

            return (new VertexId(data0, data1), new EdgeId(data2, data3), data4);
        }
    }
}