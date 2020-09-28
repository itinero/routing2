using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Network.Indexes.TurnCosts
{
    internal class TurnCostTypeFunc
    {
        private readonly int _id;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? _func;

        public TurnCostTypeFunc()
        {
            _id = 0;
            _func = null;
        }
        
        internal TurnCostTypeFunc(int id, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            _id = id;
            _func = func;
        }

        public int Id => _id;

        internal IEnumerable<(string key, string value)> ToEdgeType(IEnumerable<(string key, string value)> attributes)
        {
            if (_func == null) return Enumerable.Empty<(string key, string value)>();

            return _func(attributes);
        }

        internal TurnCostTypeFunc NextVersion(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            return new TurnCostTypeFunc(_id + 1, func);
        }
    }
}