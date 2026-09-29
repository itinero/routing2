using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Data;

/// <summary>
/// A global id set mapping global edge ids to local edge ids.
/// </summary>
/// <remarks>
/// Open-addressed over flat arrays instead of a dictionary: at one write per forward internal edge
/// of every inserted tile, a node per entry means an allocation plus a random-address probe.
/// </remarks>
public sealed class GlobalEdgeIdSet : IEnumerable<(GlobalEdgeId globalId, EdgeId edgeId)>
{
    // Well above the core count so a resize, which holds one stripe for a rehash, blocks a small
    // fraction of writers rather than all of them.
    private const int Stripes = 64;

    private const int StripeMask = Stripes - 1;

    // Sized so the whole table starts around 2^20 slots, reached without rehashing.
    private const int InitialStripeCapacity = 1 << 14;

    // Keys unpacked rather than kept as a GlobalEdgeId so a probe compares a long first and only
    // looks at the node indices on a hit.
    private sealed class Stripe
    {
        public long[] Ways = new long[InitialStripeCapacity];
        public ushort[] Tails = new ushort[InitialStripeCapacity];
        public ushort[] Heads = new ushort[InitialStripeCapacity];
        public uint[] TileIds = new uint[InitialStripeCapacity];
        public uint[] LocalIds = new uint[InitialStripeCapacity];

        // Its own array because any key value is legal, including zero.
        public bool[] Occupied = new bool[InitialStripeCapacity];

        public int Count;
    }

    private readonly Stripe[] _stripes = CreateStripes();

    private static Stripe[] CreateStripes()
    {
        var stripes = new Stripe[Stripes];
        for (var s = 0; s < Stripes; s++) stripes[s] = new Stripe();

        return stripes;
    }

    // Stripe from the high bits, slot from the low bits, so the two are independent.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HashOf(GlobalEdgeId id) =>
        HashCode.Combine(id.EdgeId, id.Tail, id.Head);

    /// <summary>
    /// Sets a new mapping.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <param name="edgeId">The local edge id.</param>
    public void Set(GlobalEdgeId globalEdgeId, EdgeId edgeId)
    {
        var hash = HashOf(globalEdgeId);
        var stripe = _stripes[hash & StripeMask];
        lock (stripe)
        {
            // Grow before inserting: the probe loop relies on there always being a free slot.
            if ((stripe.Count + 1) * 10 >= stripe.Ways.Length * 7) Grow(stripe);

            Insert(stripe, globalEdgeId, edgeId, hash);
        }
    }

    private static void Insert(Stripe stripe, GlobalEdgeId id, EdgeId edgeId, int hash)
    {
        var mask = stripe.Ways.Length - 1;
        var slot = (hash >>> 6) & mask;
        while (stripe.Occupied[slot])
        {
            if (stripe.Ways[slot] == id.EdgeId && stripe.Tails[slot] == id.Tail &&
                stripe.Heads[slot] == id.Head)
            {
                // Overwrite, matching the dictionary indexer this replaced.
                stripe.TileIds[slot] = edgeId.TileId;
                stripe.LocalIds[slot] = edgeId.LocalId;
                return;
            }

            slot = (slot + 1) & mask;
        }

        stripe.Occupied[slot] = true;
        stripe.Ways[slot] = id.EdgeId;
        stripe.Tails[slot] = id.Tail;
        stripe.Heads[slot] = id.Head;
        stripe.TileIds[slot] = edgeId.TileId;
        stripe.LocalIds[slot] = edgeId.LocalId;
        stripe.Count++;
    }

    // Doubles a stripe and rehashes it. Caller holds the stripe.
    private static void Grow(Stripe stripe)
    {
        var oldWays = stripe.Ways;
        var oldTails = stripe.Tails;
        var oldHeads = stripe.Heads;
        var oldTileIds = stripe.TileIds;
        var oldLocalIds = stripe.LocalIds;
        var oldOccupied = stripe.Occupied;
        var capacity = oldWays.Length * 2;

        stripe.Ways = new long[capacity];
        stripe.Tails = new ushort[capacity];
        stripe.Heads = new ushort[capacity];
        stripe.TileIds = new uint[capacity];
        stripe.LocalIds = new uint[capacity];
        stripe.Occupied = new bool[capacity];
        stripe.Count = 0;

        for (var i = 0; i < oldWays.Length; i++)
        {
            if (!oldOccupied[i]) continue;

            var id = GlobalEdgeId.Create(oldWays[i], oldTails[i], oldHeads[i]);
            Insert(stripe, id, new EdgeId(oldTileIds[i], oldLocalIds[i]), HashOf(id));
        }
    }

    /// <summary>
    /// Removes a mapping.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <remarks>
    /// Tombstone-free: the rest of the probe run is reinserted, so every probe can still stop at the
    /// first free slot.
    /// </remarks>
    public void Remove(GlobalEdgeId globalEdgeId)
    {
        var hash = HashOf(globalEdgeId);
        var stripe = _stripes[hash & StripeMask];
        lock (stripe)
        {
            if (!TryFind(stripe, globalEdgeId, hash, out var slot)) return;

            var mask = stripe.Ways.Length - 1;
            stripe.Occupied[slot] = false;
            stripe.Count--;

            var next = (slot + 1) & mask;
            while (stripe.Occupied[next])
            {
                var id = GlobalEdgeId.Create(stripe.Ways[next], stripe.Tails[next], stripe.Heads[next]);
                var edgeId = new EdgeId(stripe.TileIds[next], stripe.LocalIds[next]);
                stripe.Occupied[next] = false;
                stripe.Count--;
                Insert(stripe, id, edgeId, HashOf(id));
                next = (next + 1) & mask;
            }
        }
    }

    private static bool TryFind(Stripe stripe, GlobalEdgeId id, int hash, out int slot)
    {
        var mask = stripe.Ways.Length - 1;
        slot = (hash >>> 6) & mask;
        while (stripe.Occupied[slot])
        {
            if (stripe.Ways[slot] == id.EdgeId && stripe.Tails[slot] == id.Tail &&
                stripe.Heads[slot] == id.Head)
            {
                return true;
            }

            slot = (slot + 1) & mask;
        }

        return false;
    }

    /// <summary>
    /// Gets a mapping if it exists.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <param name="edgeId">The edge associated with the given global edge, if any.</param>
    /// <returns>True if a mapping exists, false otherwise.</returns>
    public bool TryGet(GlobalEdgeId globalEdgeId, out EdgeId edgeId)
    {
        var hash = HashOf(globalEdgeId);
        var stripe = _stripes[hash & StripeMask];
        lock (stripe)
        {
            if (!TryFind(stripe, globalEdgeId, hash, out var slot))
            {
                edgeId = default;
                return false;
            }

            edgeId = new EdgeId(stripe.TileIds[slot], stripe.LocalIds[slot]);
            return true;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    /// <remarks>
    /// Snapshots each stripe under its lock rather than yielding while holding it, so a slow
    /// consumer cannot block inserts.
    /// </remarks>
    public IEnumerator<(GlobalEdgeId globalId, EdgeId edgeId)> GetEnumerator()
    {
        foreach (var stripe in _stripes)
        {
            List<(GlobalEdgeId, EdgeId)> snapshot;
            lock (stripe)
            {
                snapshot = new List<(GlobalEdgeId, EdgeId)>(stripe.Count);
                for (var i = 0; i < stripe.Ways.Length; i++)
                {
                    if (!stripe.Occupied[i]) continue;

                    snapshot.Add((GlobalEdgeId.Create(stripe.Ways[i], stripe.Tails[i], stripe.Heads[i]),
                        new EdgeId(stripe.TileIds[i], stripe.LocalIds[i])));
                }
            }

            foreach (var entry in snapshot) yield return entry;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
