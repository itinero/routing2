using System;
using System.Collections.Generic;
using Itinero.Indexes;
using Itinero.Profiles;

namespace Itinero
{
    public sealed partial class RouterDb
    {
        private readonly AttributeSetIndex _edgeTypeIndex;
        private readonly AttributeSetIndex _turnCostTypeIndex;
        private readonly AttributeSetMap _turnCostTypeMap;

        internal RouterDbProfileConfiguration ProfileConfiguration { get; }

        /// <summary>
        /// Gets the edge type attributes for the given id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The attributes.</returns>
        public IEnumerable<(string key, string value)> GetEdgeType(uint id)
        {
            return _edgeTypeIndex.GetById(id);
        }

        /// <summary>
        /// The edge type map.
        /// </summary>
        internal AttributeSetMap EdgeTypeMap { get; set; }

        /// <summary>
        /// Gets the edge type count.
        /// </summary>
        public long EdgeTypeCount => _edgeTypeIndex.Count;
        
        /// <summary>
        /// Gets the turn attributes for the given type.
        /// </summary>
        /// <param name="id">The id or index.</param>
        /// <returns>The attributes.</returns>
        internal (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) GetEdgeTypeMap()
        {
            return (this.EdgeTypeMap.Id,
                a => {
                    var m = this.EdgeTypeMap.Map(a);
                    return _edgeTypeIndex.Get(m);
                });
        }

        /// <summary>
        /// Gets the turn cost type count.
        /// </summary>
        public long TurnCostTypeCount => _turnCostTypeIndex.Count;
        
        /// <summary>
        /// Gets the turn attributes for the given type.
        /// </summary>
        /// <param name="id">The id or index.</param>
        /// <returns>The attributes.</returns>
        public IEnumerable<(string key, string value)> GetTurnCostType(uint id)
        {
            return _turnCostTypeIndex.GetById(id);
        }

        internal (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) GetTurnCostTypeMap()
        {
            return (_turnCostTypeMap.Id, a => {
                var m = _turnCostTypeMap.Map(a);
                return _turnCostTypeIndex.Get(m);
            });
        }
    }
}