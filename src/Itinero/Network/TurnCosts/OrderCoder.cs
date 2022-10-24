using System;
using Reminiscence.Arrays;

namespace Itinero.Network.TurnCosts;

internal static class OrderCoder
{
    internal const int MaxOrderHeadTail = 14;

    public static void SetTailHeadOrder(this ArrayBase<byte> data, long i, byte? tail, byte? head)
    {
        if (tail.HasValue && tail.Value > MaxOrderHeadTail)
        {
            throw new ArgumentOutOfRangeException(nameof(tail),
                $"Maximum order exceeded.");
        }

        if (head.HasValue && head.Value > MaxOrderHeadTail)
        {
            throw new ArgumentOutOfRangeException(nameof(head),
                $"Maximum order exceeded.");
        }

        var d = 0;
        if (tail.HasValue)
        {
            d = tail.Value + 1;
        }

        if (head.HasValue)
        {
            d += (head.Value + 1) * 16;
        }

        data[i] = (byte)d;
    }

    public static void GetTailHeadOrder(this ArrayBase<byte> data, long i, ref byte? tail, ref byte? head)
    {
        tail = null;
        head = null;

        var d = data[i];
        if (d == 0)
        {
            return;
        }

        tail = null;
        var t = d % 16;
        if (t > 0)
        {
            tail = (byte)(t - 1);
        }

        var h = d / 16;
        if (h > 0)
        {
            head = (byte)(h - 1);
        }
    }
}
