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
    }
}