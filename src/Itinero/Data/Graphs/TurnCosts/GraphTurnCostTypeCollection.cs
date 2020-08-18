using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;

namespace Itinero.Data.Graphs.TurnCosts
{
    /// <summary>
    /// Keeps track of turn cost types.
    /// </summary>
    /// <remarks>
    /// A collection of turn cost types that:
    /// - Can only grow.
    /// - Cannot change data in existing ids.
    /// </remarks>
    internal class GraphTurnCostTypeCollection
    {
        private readonly List<IReadOnlyList<(string key, string value)>> _turnCostTypes;
        private readonly Dictionary<IReadOnlyList<(string key, string value)>, uint> _turnCostTypesIndex;

        public GraphTurnCostTypeCollection()
        {
            _turnCostTypesIndex = new Dictionary<IReadOnlyList<(string key, string value)>, uint>(TurnCostTypeEqualityComparer.Default);
            _turnCostTypes = new List<IReadOnlyList<(string key, string value)>>();
        }

        private GraphTurnCostTypeCollection(List<IReadOnlyList<(string key, string value)>> turnCostTypes)
        {
            _turnCostTypes = turnCostTypes;

            _turnCostTypesIndex = new Dictionary<IReadOnlyList<(string key, string value)>, uint>(TurnCostTypeEqualityComparer.Default);
            for (var p = 0; p < _turnCostTypes.Count; p++)
            {
                _turnCostTypesIndex[_turnCostTypes[p]] = (uint)p;
            }
        }

        /// <summary>
        /// Gets the number of distinct turn cost types.
        /// </summary>
        public uint Count => (uint)_turnCostTypes.Count;
        
        /// <summary>
        /// Gets the turn cost type for the given id.
        /// </summary>
        /// <param name="turnCostTypeId">The id.</param>
        /// <returns>The attributes of the turn cost type.</returns>
        public IEnumerable<(string key, string value)> GetById(uint turnCostTypeId)
        {
            if (turnCostTypeId > this.Count) throw new ArgumentOutOfRangeException(nameof(turnCostTypeId));

            return _turnCostTypes[(int)turnCostTypeId];
        }
        
        /// <summary>
        /// Gets the turn cost type id id for the given type attributes set.
        /// </summary>
        /// <param name="turnCostType">The turn cost type attributes.</param>
        /// <returns>The profile id, if any.</returns>
        public uint Get(IEnumerable<(string key, string value)> turnCostType)
        {
            var turnCostTypeArray = turnCostType.ToArray();
            
            // sort array.
            Array.Sort(turnCostTypeArray, (x, y) => x.CompareTo(y));
            
            // check if profile already there.
            if (_turnCostTypesIndex.TryGetValue(turnCostTypeArray, out var turnCostTypeId))
            {
                return turnCostTypeId;
            }
            
            // add new turn cost type.
            turnCostTypeId = (uint)_turnCostTypes.Count;
            _turnCostTypes.Add(turnCostTypeArray);
            _turnCostTypesIndex.Add(turnCostTypeArray, turnCostTypeId);

            return turnCostTypeId;
        }
        
        private class TurnCostTypeEqualityComparer : IEqualityComparer<IReadOnlyList<(string key, string value)>>
        {
            public static readonly TurnCostTypeEqualityComparer Default = new TurnCostTypeEqualityComparer();
            
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
            stream.WriteVarInt32(_turnCostTypes.Count);
            foreach (var attributes in _turnCostTypes)
            {
                stream.WriteVarInt32(attributes.Count);
                foreach (var (key, value) in attributes)
                {
                    stream.WriteWithSize(key);
                    stream.WriteWithSize(value);
                }
            }
        }

        internal static GraphTurnCostTypeCollection Deserialize(Stream stream)
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
            
            return new GraphTurnCostTypeCollection(edgeTypes);
        }
    }
}