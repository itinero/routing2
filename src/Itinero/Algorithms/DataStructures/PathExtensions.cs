using System;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.DataStructures
{
    internal static class PathExtensions
    {
        public static double Weight(this Path path, Func<(EdgeId edge, bool direction), double> getWeight)
        {
            var weight = 0.0;

            foreach (var (edge, direction, offset1, offset2) in path)
            {
                var edgeWeight = getWeight((edge, direction));
                if (offset1 != 0 || offset2 != ushort.MaxValue)
                {
                    edgeWeight *= ((double) (offset2 - offset1) / ushort.MaxValue);
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