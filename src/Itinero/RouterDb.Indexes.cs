using System;
using System.Collections.Generic;
using Itinero.Indexes;

namespace Itinero
{
    public sealed partial class RouterDb
    {
        private readonly AttributeSetIndex _edgeTypeIndex;
        private AttributeSetMap _edgeTypeMap;
        private readonly AttributeSetIndex _turnCostTypeIndex;
        private AttributeSetMap _turnCostTypeMap;

        public IEnumerable<(string key, string value)> GetEdgeType(uint id)
        {
            return _edgeTypeIndex.GetById(id);
        }

        internal (int id, Func<IEnumerable<(string key, string value)>, uint> func) GetEdgeTypeMap()
        {
            return (_edgeTypeMap.Id, 
                (e) => _edgeTypeIndex.Get(e));
        }

        public IEnumerable<(string key, string value)> GetTurnCostType(uint id)
        {
            return _turnCostTypeIndex.GetById(id);
        }

        internal (int id, Func<IEnumerable<(string key, string value)>, uint> func) GetTurnCostTypeMap()
        {
            return (_turnCostTypeMap.Id, 
                (e) => _turnCostTypeIndex.Get(e));
        }
    }
}