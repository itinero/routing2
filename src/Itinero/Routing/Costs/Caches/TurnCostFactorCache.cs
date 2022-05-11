using Itinero.Profiles;

namespace Itinero.Routing.Costs.Caches;

internal class TurnCostFactorCache
{
    private TurnCostFactor?[] _cache;

    public TurnCostFactorCache()
    {
        _cache = new TurnCostFactor?[1024];
    }

    public TurnCostFactor? Get(uint typeId)
    {
        return _cache.Length <= typeId ? null : _cache[typeId];
    }

    public void Set(uint typeId, TurnCostFactor factor)
    {
        var cache = _cache;
        if (cache.Length <= typeId) {
            var newSize = _cache.Length + 1024;
            while (newSize <= typeId) {
                newSize += 1024;
            }

            var newCache = new TurnCostFactor?[newSize];
            cache.CopyTo(newCache, 0);

            newCache[typeId] = factor;
            _cache = newCache;
            return;
        }

        cache[typeId] = factor;
    }
}