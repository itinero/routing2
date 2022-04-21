using System.Collections.Generic;

namespace Itinero.Profiles.EdgeTypesMap;

internal class LruCache<K, V>
{
    private readonly Dictionary<K, LinkedListNode<LruCacheItem<K, V>>> _cacheMap = new();
    private readonly int _capacity;
    private readonly LinkedList<LruCacheItem<K, V>> _lruList = new();

    public LruCache(int capacity)
    {
        _capacity = capacity;
    }

    public bool TryGet(K key, out V? value)
    {
        LinkedListNode<LruCacheItem<K, V>> node;
        if (!_cacheMap.TryGetValue(key, out node)) {
            value = default;
            return false;
        }

        value = node.Value.value;
        _lruList.Remove(node);
        _lruList.AddLast(node);
        return true;
    }

    public void Add(K key, V val)
    {
        if (_cacheMap.Count >= _capacity) {
            this.RemoveFirst();
        }

        LruCacheItem<K, V> cacheItem = new(key, val);
        LinkedListNode<LruCacheItem<K, V>> node = new(cacheItem);
        _lruList.AddLast(node);
        _cacheMap.Add(key, node);
    }

    private void RemoveFirst()
    {
        // Remove from LRUPriority
        LinkedListNode<LruCacheItem<K, V>> node = _lruList.First;
        _lruList.RemoveFirst();

        // Remove from cache
        _cacheMap.Remove(node.Value.key);
    }

    public int Count => _cacheMap.Count;
}

internal class LruCacheItem<K, V>
{
    public K key;
    public V value;

    public LruCacheItem(K k, V v)
    {
        key = k;
        value = v;
    }
}