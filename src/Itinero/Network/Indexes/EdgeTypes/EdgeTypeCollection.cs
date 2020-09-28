using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Data;
using Itinero.IO;

namespace Itinero.Network.Indexes.EdgeTypes
{
    /// <summary>
    /// Keeps track of edge types.
    /// </summary>
    /// <remarks>
    /// A collection of edges types that:
    /// - Can only grow.
    /// - Cannot change data in existing ids.
    /// </remarks>
    internal class EdgeTypeCollection
    {
        private readonly List<IReadOnlyList<(string key, string value)>> _edgeProfiles;
        private readonly Dictionary<IReadOnlyList<(string key, string value)>, uint> _edgeProfilesIndex;

        public EdgeTypeCollection()
        {
            _edgeProfilesIndex = new Dictionary<IReadOnlyList<(string key, string value)>, uint>(EdgeProfileEqualityComparer.Default);
            _edgeProfiles = new List<IReadOnlyList<(string key, string value)>>();
        }

        private EdgeTypeCollection(List<IReadOnlyList<(string key, string value)>> edgeProfiles)
        {
            _edgeProfiles = edgeProfiles;

            _edgeProfilesIndex = new Dictionary<IReadOnlyList<(string key, string value)>, uint>(EdgeProfileEqualityComparer.Default);
            for (var p = 0; p < _edgeProfiles.Count; p++)
            {
                _edgeProfilesIndex[_edgeProfiles[p]] = (uint)p;
            }
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

        internal void Serialize(Stream stream)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write pairs.
            stream.WriteVarInt32(_edgeProfiles.Count);
            foreach (var attributes in _edgeProfiles)
            {
                stream.WriteVarInt32(attributes.Count);
                foreach (var (key, value) in attributes)
                {
                    stream.WriteWithSize(key);
                    stream.WriteWithSize(value);
                }
            }
        }

        internal static EdgeTypeCollection Deserialize(Stream stream)
        {
            // get version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unexpected version #.");
            
            // read pairs.
            var count = stream.ReadVarInt32();
            var edgeTypes = new List<IReadOnlyList<(string key, string value)>>(count);
            for (var i = 0; i < count; i++)
            {
                var c = stream.ReadVarInt32();
                var attribute = new (string key, string value)[c];
                for (var a = 0; a < c; a++)
                {
                    var key = stream.ReadWithSizeString();
                    var value = stream.ReadWithSizeString();
                    attribute[a] = (key, value);
                }
                
                edgeTypes.Add(attribute);
            }
            
            return new EdgeTypeCollection(edgeTypes);
        }
    }
}