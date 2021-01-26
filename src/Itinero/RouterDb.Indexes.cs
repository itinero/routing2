using System;
using System.Collections.Generic;
using Itinero.Indexes;
using Itinero.Profiles;

namespace Itinero
{
    public sealed partial class RouterDb
    {
        private readonly AttributeSetIndex _edgeTypeIndex;
        private AttributeSetMap _edgeTypeMap;
        private readonly AttributeSetIndex _turnCostTypeIndex;
        private AttributeSetMap _turnCostTypeMap;

        internal RouterDbProfileConfiguration ProfileConfiguration { get; }

        public IEnumerable<(string key, string value)> GetEdgeType(uint id)
        {
            return _edgeTypeIndex.GetById(id);
        }

        internal void SetEdgeTypeMap(AttributeSetMap edgeTypeMap)
        {
            _edgeTypeMap = edgeTypeMap;
        }

        internal (int id, Func<IEnumerable<(string key, string value)>, uint> func) GetEdgeTypeMap()
        {
            return (_edgeTypeMap.Id,
                a => {
                    var m = _edgeTypeMap.Mapping(a);
                    return _edgeTypeIndex.Get(m);
                });
        }

        public IEnumerable<(string key, string value)> GetTurnCostType(uint id)
        {
            return _turnCostTypeIndex.GetById(id);
        }

        internal (int id, Func<IEnumerable<(string key, string value)>, uint> func) GetTurnCostTypeMap()
        {
            return (_turnCostTypeMap.Id, a => {
                var m = _turnCostTypeMap.Mapping(a);
                return _turnCostTypeIndex.Get(m);
            });
        }
    }
}