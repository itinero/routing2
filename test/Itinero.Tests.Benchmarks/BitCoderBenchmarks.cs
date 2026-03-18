using System;
using BenchmarkDotNet.Attributes;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Benchmarks for varint encoding/decoding through ArrayBase vs native byte[].
/// This isolates the per-byte overhead of the MemoryArray abstraction.
/// </summary>
[MemoryDiagnoser]
public class BitCoderBenchmarks
{
    private MemoryArray<byte> _memoryArray = null!;
    private byte[] _nativeArray = null!;

    // positions of encoded values for sequential reads.
    private int[] _positions = null!;
    private const int ValueCount = 10_000;

    [GlobalSetup]
    public void Setup()
    {
        // encode a mix of small and large values into both arrays.
        _memoryArray = new MemoryArray<byte>(ValueCount * 5);
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

            var size = _memoryArray.SetDynamicUInt32(pos, value);

            // write same bytes to native array.
            for (var b = 0; b < size; b++)
            {
                _nativeArray[pos + b] = _memoryArray[pos + b];
            }

            pos += size;
        }
    }

    [Benchmark(Baseline = true)]
    public uint DecodeUInt32_MemoryArray()
    {
        uint sum = 0;
        for (var i = 0; i < ValueCount; i++)
        {
            _memoryArray.GetDynamicUInt32(_positions[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public uint DecodeUInt32_NativeArray()
    {
        uint sum = 0;
        for (var i = 0; i < ValueCount; i++)
        {
            var pos = _positions[i];
            sum += DecodeUInt32(_nativeArray, pos);
        }

        return sum;
    }

    /// <summary>
    /// Equivalent of GetDynamicUInt32 but on a plain byte[].
    /// This is what the code would look like after removing MemoryArray.
    /// </summary>
    private static uint DecodeUInt32(byte[] data, int i)
    {
        var d = data[i];
        if (d < 128) return d;

        uint value = (uint)d - 128;
        d = data[i + 1];
        if (d < 128) return value + ((uint)d << 7);

        value += (uint)(d - 128) << 7;
        d = data[i + 2];
        if (d < 128) return value + ((uint)d << 14);

        value += (uint)(d - 128) << 14;
        d = data[i + 3];
        if (d < 128) return value + ((uint)d << 21);

        value += (uint)(d - 128) << 21;
        return value + ((uint)data[i + 4] << 28);
    }
}
