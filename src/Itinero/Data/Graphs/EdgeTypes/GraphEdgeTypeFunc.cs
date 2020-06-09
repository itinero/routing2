using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Graphs.EdgeTypes
{
    internal class GraphEdgeTypeFunc
    {
        private readonly int _id;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? _func;

        public GraphEdgeTypeFunc()
        {
            _id = 0;
            _func = null;
        }
        
        internal GraphEdgeTypeFunc(int id, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
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

        internal GraphEdgeTypeFunc NextVersion(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            return new GraphEdgeTypeFunc(_id + 1, func);
        }
    }
}