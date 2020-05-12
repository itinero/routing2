using System;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.DataStructures
{
    internal static class PathExtensions
    {
        public static double Weight(this Path path, Func<(EdgeId edge, bool direction), double> getWeight)
        {
            var weight = 0.0;
            
            for (var i = 0; i < path.Count; i++)
            {
                var edge = path[i];

                var edgeWeight = getWeight(edge);

                if (i == 0)
                {
                    edgeWeight *= (1 - ((double) path.Offset1 / ushort.MaxValue));
                }
                else if (i == path.Count - 1)
                {
                    edgeWeight *= (1 - ((double) path.Offset2 / ushort.MaxValue));
                }

                weight += edgeWeight;
            }

            return weight;
        }

        /// <summary>
        /// Removes the first and/or last edge if they are not part of the path via the offset properties.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void Trim(this Path path)
        {
            if (path.Count <= 1) return;
            
            var first = path[0];
            if ((first.forward && path.Offset1 == ushort.MaxValue) ||
                (!first.forward && path.Offset1 == 0))
            {
                path.RemoveFirst();
                first = path[0];
                path.Offset1 = first.forward ? (ushort)0 : ushort.MaxValue;
            }
            
            if (path.Count <= 1) return;

            var last = path[path.Count - 1];
            if ((last.forward && path.Offset1 == 0) ||
                (!last.forward && path.Offset1 == ushort.MaxValue))
            {
                path.RemoveLast();
                last = path[path.Count - 1];
                path.Offset2 = last.forward ? ushort.MaxValue : (ushort)0;
            }
        }
    }
}