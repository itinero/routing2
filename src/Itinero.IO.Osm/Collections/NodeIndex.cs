namespace Itinero.IO.Osm.Collections
{
    /// <summary>
    /// A cache for node coordinates.
    /// </summary>
    internal sealed class NodeIndex
    {
        private readonly UnsignedNodeIndex _negativeNodeIndex;
        private readonly UnsignedNodeIndex _positiveNodeIndex;

        public NodeIndex()
        {
            _negativeNodeIndex = new UnsignedNodeIndex();
            _positiveNodeIndex = new UnsignedNodeIndex();
        }

        /// <summary>
        /// Adds a node id to the index.
        /// </summary>
        public void AddId(long id)
        {
            if (id >= 0)
            {
                _positiveNodeIndex.AddId(id);
            }
            else
            {
                _negativeNodeIndex.AddId(-id);
            }
        }

        /// <summary>
        /// Sorts and converts the index.
        /// </summary>
        public void SortAndConvertIndex()
        {
            _positiveNodeIndex.SortAndConvertIndex();
            _negativeNodeIndex.SortAndConvertIndex();
        }

        /// <summary>
        /// Gets the node id at the given index.
        /// </summary>
        public long this[long idx]
        {
            get
            {
                if (idx >= _negativeNodeIndex.Count)
                {
                    return _positiveNodeIndex[idx - _negativeNodeIndex.Count];
                }
                return _negativeNodeIndex[idx];
            }
        }

        /// <summary>
        /// Sets a vertex id for the given vertex.
        /// </summary>
        public void Set(long id, uint vertex)
        {
            if (id >= 0)
            {
                _positiveNodeIndex.Set(id, vertex);
            }
            else
            {
                _negativeNodeIndex.Set(-id, vertex);
            }
        }

        /// <summary>
        /// Gets the coordinate for the given node.
        /// </summary>
        public long TryGetIndex(long id)
        {
            if (id >= 0)
            {
                return _positiveNodeIndex.TryGetIndex(id);
            }
            else
            {
                var result = _negativeNodeIndex.TryGetIndex(-id);
                if (result == long.MaxValue)
                {
                    return long.MaxValue;
                }
                return -(result + 1);
            }
        }

        /// <summary>
        /// Sets the coordinate for the given index.
        /// </summary>
        public void SetIndex(long idx, float latitude, float longitude)
        {
            if (idx >= 0)
            {
                _positiveNodeIndex.SetIndex(idx, latitude, longitude);
            }
            else
            {
                idx = -idx - 1;
                _negativeNodeIndex.SetIndex(idx, latitude, longitude);
            }
        }
        /// <summary>
        /// Tries to get a core node and it's matching vertex.
        /// </summary>
        public bool TryGetCoreNode(long id, out uint vertex)
        {
            if (id >= 0)
            {
                return _positiveNodeIndex.TryGetCoreNode(id, out vertex);
            }
            else
            {
                return _negativeNodeIndex.TryGetCoreNode(-id, out vertex);
            }
        }
        
        /// <summary>
        /// Gets all relevant info on the given node.
        /// </summary>
        public bool TryGetValue(long id, out float latitude, out float longitude, out bool isCore, out uint vertex, out long idx)
        {
            if (id >= 0)
            {
                return _positiveNodeIndex.TryGetValue(id, out latitude, out longitude, out isCore, out vertex, out idx);
            }
            else
            {
                return _negativeNodeIndex.TryGetValue(-id, out latitude, out longitude, out isCore, out vertex, out idx);
            }
        }
    }
}
