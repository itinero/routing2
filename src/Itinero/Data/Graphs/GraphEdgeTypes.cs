using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Graphs
{
    /// <summary>
    /// Keeps track of edge types.
    /// </summary>
    /// <remarks>
    /// This collection of edges types:
    /// - Can only grow.
    /// - Cannot change data in existing ids.
    /// </remarks>
    internal class GraphEdgeTypes
    {
        private readonly List<IEnumerable<(string key, string value)>> _edgeProfiles;
        private readonly Dictionary<IReadOnlyList<(string key, string value)>, uint> _edgeProfilesIndex;

        public GraphEdgeTypes()
        {
            _edgeProfilesIndex = new Dictionary<IReadOnlyList<(string key, string value)>, uint>(EdgeProfileEqualityComparer.Default);
            _edgeProfiles = new List<IEnumerable<(string key, string value)>>();
        }

        /// <summary>
        /// Gets the number of distinct edge profiles.
        /// </summary>
        public uint Count => (uint)_edgeProfiles.Count;
        
        /// <summary>
        /// Gets the edge profile for the given id.
        /// </summary>
        /// <param name="edgeProfileId">The id.</param>
        /// <returns>The attributes in the edge profile.</returns>
        public IEnumerable<(string key, string value)> GetById(uint edgeProfileId)
        {
            if (edgeProfileId > this.Count) throw new ArgumentOutOfRangeException(nameof(edgeProfileId));

            return _edgeProfiles[(int)edgeProfileId];
        }
        
        /// <summary>
        /// Gets the edge profile id for the given edge type attributes set.
        /// </summary>
        /// <param name="edgeType">The edge type attributes.</param>
        /// <returns>The profile id, if any.</returns>
        public uint Get(IEnumerable<(string key, string value)> edgeType)
        {
            var edgeTypeArray = edgeType.ToArray();
            
            // sort array.
            Array.Sort(edgeTypeArray, (x, y) => x.CompareTo(y));
            
            // check if profile already there.
            if (_edgeProfilesIndex.TryGetValue(edgeTypeArray, out var edgeProfileId))
            {
                return edgeProfileId;
            }
            
            // add new profile.
            edgeProfileId = (uint)_edgeProfiles.Count;
            _edgeProfiles.Add(edgeTypeArray);
            _edgeProfilesIndex.Add(edgeTypeArray, edgeProfileId);

            return edgeProfileId;
        }
        
        private class EdgeProfileEqualityComparer : IEqualityComparer<IReadOnlyList<(string key, string value)>>
        {
            public static readonly EdgeProfileEqualityComparer Default = new EdgeProfileEqualityComparer();
            
            public bool Equals(IReadOnlyList<(string key, string value)> x, IReadOnlyList<(string key, string value)> y)
            {
                if (x.Count != y.Count) return false;

                for (var i = 0; i < x.Count; i++)
                {
                    var xPair = x[i];
                    var yPair = y[i];

                    if (xPair != yPair) return false;
                }

                return true;
            }

            public int GetHashCode(IReadOnlyList<(string key, string value)> obj)
            {
                var hash = obj.Count.GetHashCode();

                foreach (var pair in obj)
                {
                    hash ^= pair.GetHashCode();
                }

                return hash;
            }
        }
    }
}