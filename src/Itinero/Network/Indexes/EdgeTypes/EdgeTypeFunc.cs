using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Network.Indexes.EdgeTypes
{
    internal class EdgeTypeFunc
    {
        private readonly int _id;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? _func;

        public EdgeTypeFunc()
        {
            _id = 0;
            _func = null;
        }
        
        internal EdgeTypeFunc(int id, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
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

        internal EdgeTypeFunc NextVersion(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            return new EdgeTypeFunc(_id + 1, func);
        }
    }
}