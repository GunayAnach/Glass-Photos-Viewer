namespace GlassPhotos.Core;

public sealed class AsyncLruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly object _gate = new();
    private readonly Dictionary<TKey, Entry> _entries;
    private readonly LinkedList<TKey> _recency = new();

    public AsyncLruCache(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _entries = new Dictionary<TKey, Entry>(comparer);
    }

    public Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                Touch(existing.Node);
                return existing.Value;
            }

            var value = valueFactory(key);
            var node = _recency.AddFirst(key);
            _entries.Add(key, new Entry(value, node));
            TrimToCapacity();
            _ = RemoveFailedLoadAsync(key, value);
            return value;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_gate)
        {
            if (!_entries.Remove(key, out var entry)) return false;
            _recency.Remove(entry.Node);
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _recency.Clear();
        }
    }

    private async Task RemoveFailedLoadAsync(TKey key, Task<TValue> value)
    {
        try
        {
            await value.ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var entry) && ReferenceEquals(entry.Value, value))
                {
                    _entries.Remove(key);
                    _recency.Remove(entry.Node);
                }
            }
        }
    }

    private void Touch(LinkedListNode<TKey> node)
    {
        _recency.Remove(node);
        _recency.AddFirst(node);
    }

    private void TrimToCapacity()
    {
        while (_entries.Count > _capacity && _recency.Last is { } oldest)
        {
            _entries.Remove(oldest.Value);
            _recency.RemoveLast();
        }
    }

    private sealed record Entry(Task<TValue> Value, LinkedListNode<TKey> Node);
}
