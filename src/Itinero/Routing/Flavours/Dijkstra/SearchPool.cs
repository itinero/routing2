using System.Collections.Concurrent;
using System.Threading;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// Reuses search instances so their trees, heaps and visited sets are not rebuilt
/// per route. Rented per call and returned in a finally, not [ThreadStatic] — async
/// continuations can resume on a thread already running another search.
/// </summary>
internal static class SearchPool<T> where T : class, new()
{
    /// Bounds retention: each instance holds buffers sized for its largest search.
    private const int MaxRetained = 64;

    private static readonly ConcurrentBag<T> Pool = new();
    private static int _retained;

    public static T Rent()
    {
        if (!Pool.TryTake(out var item)) return new T();

        Interlocked.Decrement(ref _retained);
        return item;
    }

    public static void Return(T item)
    {
        if (Interlocked.Increment(ref _retained) > MaxRetained)
        {
            Interlocked.Decrement(ref _retained);
            return;
        }

        Pool.Add(item);
    }
}
