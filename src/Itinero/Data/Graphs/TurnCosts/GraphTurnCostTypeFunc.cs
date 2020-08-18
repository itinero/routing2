using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Graphs.TurnCosts
{
    internal class GraphTurnCostTypeFunc
    {
        private readonly int _id;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? _func;

        public GraphTurnCostTypeFunc()
        {
            _id = 0;
            _func = null;
        }
        
        internal GraphTurnCostTypeFunc(int id, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
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

        internal GraphTurnCostTypeFunc NextVersion(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            return new GraphTurnCostTypeFunc(_id + 1, func);
        }
    }
}