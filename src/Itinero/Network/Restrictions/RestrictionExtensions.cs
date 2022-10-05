using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Restrictions
{
    /// <summary>
    /// Contains extension methods to work with restricted sequences.
    /// </summary>
    public static class RestrictionExtensions
    {
        /// <summary>
        /// Inverts the given restriction returning all possible sequences in the network starting with the same edges except the last edge.
        /// </summary>
        /// <param name="restrictedSequence">The restricted sequence.</param>
        /// <param name="mutableNetworkEdgeEnumerator">The enumerator to query edges.</param>
        /// <returns>All sequences starting with the same edges but ending with a different one.</returns>
        public static IEnumerable<IEnumerable<(EdgeId edge, bool forward)>> Invert<T>(
            this IEnumerable<(EdgeId edge, bool forward)> restrictedSequence,
            EdgeEnumerator<T> mutableNetworkEdgeEnumerator)
            where T : IEdgeEnumerable
        {
            var firstPart = new List<(EdgeId edge, bool forward)>(restrictedSequence);
            if (firstPart.Count < 2)
            {
                yield break; // no inverse possible.
            }

            // get last.
            var last = firstPart[firstPart.Count - 1];

            // enumerate all edges except the u-turn and the original sequence.
            var secondToLast = firstPart[firstPart.Count - 2];
            mutableNetworkEdgeEnumerator.MoveToEdge(secondToLast.edge, secondToLast.forward);
            mutableNetworkEdgeEnumerator.MoveTo(mutableNetworkEdgeEnumerator.Head);
            while (mutableNetworkEdgeEnumerator.MoveNext())
            {
                var id = mutableNetworkEdgeEnumerator.EdgeId;
                if (id == secondToLast.edge &&
                    mutableNetworkEdgeEnumerator.Forward != secondToLast.forward)
                {
                    continue;
                }

                if (id == last.edge &&
                    mutableNetworkEdgeEnumerator.Forward == last.forward)
                {
                    continue;
                }

                yield return firstPart.Take(firstPart.Count - 1)
                    .Append((Id: mutableNetworkEdgeEnumerator.EdgeId, mutableNetworkEdgeEnumerator.Forward));
            }
        }
    }
}
