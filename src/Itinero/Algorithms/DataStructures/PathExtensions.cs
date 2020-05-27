using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.DataStructures
{
    /// <summary>
    /// Extensions for path.
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Calculates the weight of the path given the weight function.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="getWeight">The weight function.</param>
        /// <returns>The total weight.</returns>
        public static double? Weight(this Result<Path> path, Func<RouterDbEdgeEnumerator, double> getWeight)
        {
            if (path.IsError) return null;

            return path.Value.Weight(getWeight);
        }
        
        /// <summary>
        /// Calculates the weight of the path given the weight function.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="getWeight">The weight function.</param>
        /// <returns>The total weight.</returns>
        public static double Weight(this Path path, Func<RouterDbEdgeEnumerator, double> getWeight)
        {
            var weight = 0.0;

            var edgeEnumerator = path.RouterDb.GetEdgeEnumerator();
            foreach (var (edge, direction, offset1, offset2) in path)
            {
                if (!edgeEnumerator.MoveToEdge(edge, direction)) throw new InvalidDataException("An edge in the path is not found!");
                var edgeWeight = getWeight(edgeEnumerator);
                if (offset1 != 0 || offset2 != ushort.MaxValue)
                {
                    edgeWeight *= ((double) (offset2 - offset1) / ushort.MaxValue);
                }

                weight += edgeWeight;
            }

            return weight;
        }

        /// <summary>
        /// Returns true if the path is next.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="next">The next path.</param>
        /// <returns>True if the next path</returns>
        public static bool IsNext(this Path path, Path next)
        {
            var last = path.Last;
            var first = next.First;

            // check if the same edge and if the offsets match.
            if (last.edge == first.edge &&
                last.direction == first.direction)
            {
                var offset2 = (ushort)(ushort.MaxValue - next.Offset1);
                return (path.Offset2 == offset2);
            }
            
            // check if the same vertices at the end.
            if (next.Offset1 != 0 ||
                path.Offset2 != ushort.MaxValue) return false;
            
            var edgeEnumerator = path.RouterDb.GetEdgeEnumerator();
            edgeEnumerator.MoveToEdge(last.edge, last.direction);
            var lastVertex = edgeEnumerator.To;
            edgeEnumerator.MoveToEdge(first.edge, first.direction);
            var firstVertex = edgeEnumerator.From;

            return lastVertex == firstVertex;
        }

        /// <summary>
        /// Merges the paths.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>The merged path.</returns>
        public static Path? Merge(this IEnumerable<Path> paths)
        {
            Path? merged = null;
            RouterDbEdgeEnumerator? enumerator = null;
            foreach (var path in paths)
            {
                merged ??= new Path(path.RouterDb);
                enumerator ??= path.RouterDb.GetEdgeEnumerator();

                if (merged.Count == 0)
                {
                    merged.Offset1 = path.Offset1;
                }
                else if(!merged.IsNext(path))
                {
                    throw new InvalidDataException(
                    $"Paths cannot be concatenated.");
                }

                foreach (var (edge, direction, _, _) in path)
                {
                    if (!enumerator.MoveToEdge(edge, direction)) throw new InvalidDataException(
                        $"Edge not found.");
                    
                    merged.Append(edge, enumerator.To);
                }

                merged.Offset2 = path.Offset2;
            }

            return merged;
        }

        /// <summary>
        /// Removes the first and/or last edge if they are not part of the path via the offset properties.
        /// </summary>
        /// <param name="path">The path.</param>
        internal static void Trim(this Path path)
        {
            if (path.Count <= 1) return;
            
            if (path.Offset1 == ushort.MaxValue)
            {
                path.RemoveFirst();
                path.Offset1 = 0;
            }
            
            if (path.Count <= 1) return;
            
            if (path.Offset2 == 0)
            {
                path.RemoveLast();
                path.Offset2 = ushort.MaxValue;
            }
        }
    }
}