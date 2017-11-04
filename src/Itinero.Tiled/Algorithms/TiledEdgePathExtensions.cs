using Itinero.Algorithms.Weights;
using System;
using System.Collections.Generic;
using System.Text;

namespace Itinero.Tiled.Algorithms
{
    public static class TiledEdgePathExtensions
    {
        /// <summary>
        /// Appends the given path in reverse to the edge path.
        /// </summary>
        public static TiledEdgePath<T> Append<T>(this TiledEdgePath<T> path, TiledEdgePath<T> reversePath, WeightHandler<T> weightHandler)
            where T : struct
        {
            if (path.Vertex != reversePath.Vertex)
            {
                throw new System.Exception("Cannot append path that ends with a different vertex.");
            }

            while (reversePath.From != null)
            {
                var localWeight = weightHandler.Subtract(reversePath.Weight, reversePath.From.Weight);
                path = new TiledEdgePath<T>(reversePath.From.Vertex, weightHandler.Add(path.Weight, localWeight), -reversePath.Edge, path);
                reversePath = reversePath.From;
            }
            return path;
        }
    }
}