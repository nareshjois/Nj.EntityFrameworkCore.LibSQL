namespace Nj.LibSql.Data.Internal;

/// <summary>
/// A simple thread-safe LRU (least-recently-used) cache implementation.
/// </summary>
internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new();

    public LruCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0");
        }

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default!;
            return false;
        }
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                node.Value.Value = value;
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return;
            }

            if (_cache.Count >= _capacity)
            {
                var lru = _lruList.Last;
                if (lru != null)
                {
                    _lruList.RemoveLast();
                    _cache.Remove(lru.Value.Key);
                    lru.Value.Dispose();
                }
            }

            var cacheItem = new CacheItem(key, value);
            var newNode = _lruList.AddFirst(cacheItem);
            _cache[key] = newNode;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var node))
            {
                return false;
            }

            _lruList.Remove(node);
            _cache.Remove(key);
            node.Value.Dispose();
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var node in _lruList)
            {
                node.Dispose();
            }

            _cache.Clear();
            _lruList.Clear();
        }
    }

    private sealed class CacheItem(TKey key, TValue value) : IDisposable
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;

        public void Dispose()
        {
            if (Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
