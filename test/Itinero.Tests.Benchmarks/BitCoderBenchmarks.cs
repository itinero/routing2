using System;
using BenchmarkDotNet.Attributes;
using Itinero.Network.Storage;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Benchmarks for varint encoding/decoding on native byte[].
/// </summary>
[MemoryDiagnoser]
public class BitCoderBenchmarks
{
    private byte[] _nativeArray = null!;

    // positions of encoded values for sequential reads.
    private int[] _positions = null!;
    private const int ValueCount = 10_000;

    [GlobalSetup]
    public void Setup()
    {
        _nativeArray = new byte[ValueCount * 5];

        var rng = new Random(42);
        _positions = new int[ValueCount];
        uint pos = 0;

        for (var i = 0; i < ValueCount; i++)
        {
            _positions[i] = (int)pos;

            // mix of value sizes: 70% small (1 byte), 20% medium (2 bytes), 10% large (3+ bytes).
            var r = rng.NextDouble();
            uint value;
            if (r < 0.7)
                value = (uint)rng.Next(0, 128);
            else if (r < 0.9)
                value = (uint)rng.Next(128, 16384);
            else
                value = (uint)rng.Next(16384, 2097152);

            var size = _nativeArray.SetDynamicUInt32(pos, value);
            pos += size;
        }
    }

    [Benchmark]
    public uint DecodeUInt32_NativeArray()
    {
        uint sum = 0;
        for (var i = 0; i < ValueCount; i++)
        {
            _nativeArray.GetDynamicUInt32(_positions[i], out var value);
            sum += value;
        }

        return sum;
    }
}
