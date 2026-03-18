using Reminiscence.Arrays;

namespace Itinero.Network.Tiles;

internal static class ArrayBaseExtensions
{
    /// <summary>
    /// Increase the array size with a fixed step (but never more) to ensure the given position fits.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="position">The position.</param>
    /// <param name="step">The steps to add.</param>
    /// <param name="fill">The default value to set.</param>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    public static void EnsureMinimumSize<T>(this ArrayBase<T> array, long position,
        long step = 16, T fill = default)
    {
        if (array.Length > position)
        {
            return;
        }

        var increase = System.Math.DivRem(position - array.Length, step, out _) + 1;
        increase *= step;

        var size = array.Length;
        array.Resize(array.Length + increase);
        for (var i = size; i < array.Length; i++)
        {
            array[i] = fill;
        }
    }

    /// <summary>
    /// Increase the native byte array size with a fixed step (but never more) to ensure the given position fits.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="position">The position.</param>
    /// <param name="step">The steps to add.</param>
    public static void EnsureMinimumSize(ref byte[] array, long position, long step = 16)
    {
        if (array.Length > position) return;
        var newSize = array.Length + step;
        while (newSize <= position) newSize += step;
        System.Array.Resize(ref array, (int)newSize);
    }

    /// <summary>
    /// Increase the native uint array size with a fixed step (but never more) to ensure the given position fits.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="position">The position.</param>
    /// <param name="step">The steps to add.</param>
    public static void EnsureMinimumSize(ref uint[] array, long position, long step = 16)
    {
        if (array.Length > position) return;
        var newSize = array.Length + step;
        while (newSize <= position) newSize += step;
        System.Array.Resize(ref array, (int)newSize);
    }

    /// <summary>
    /// Increase the native string array size with a fixed step (but never more) to ensure the given position fits.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="position">The position.</param>
    /// <param name="step">The steps to add.</param>
    public static void EnsureMinimumSize(ref string[] array, long position, long step = 16)
    {
        if (array.Length > position) return;
        var newSize = array.Length + step;
        while (newSize <= position) newSize += step;
        System.Array.Resize(ref array, (int)newSize);
    }
}
