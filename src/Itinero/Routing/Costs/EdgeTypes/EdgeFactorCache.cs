using Itinero.Profiles;

namespace Itinero.Routing.Costs.EdgeTypes
{
    internal class EdgeFactorCache
    {
        private EdgeFactor?[] _cache;
        
        public EdgeFactorCache()
        {
            _cache = new EdgeFactor?[1024];
        }

        public EdgeFactor? Get(uint edgeTypeId)
        {
            if (_cache.Length <= edgeTypeId) return null;

            return _cache[edgeTypeId];
        }

        public void Set(uint edgeTypeId, EdgeFactor factor)
        {
            var cache = _cache;
            if (cache.Length <= edgeTypeId)
            {
                var newSize = _cache.Length + 1024;
                while (newSize <= edgeTypeId) newSize += 1024;
                var newCache = new EdgeFactor?[newSize];
                cache.CopyTo(newCache, 0);

                newCache[edgeTypeId] = factor;
                _cache = newCache;
                return;
            }

            cache[edgeTypeId] = factor;
        }
    }
}