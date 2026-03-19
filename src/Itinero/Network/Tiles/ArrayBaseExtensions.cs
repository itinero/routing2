namespace Itinero.Network.Tiles;

internal static class ArrayBaseExtensions
{
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
    /// Increase the native int array size with a fixed step (but never more) to ensure the given position fits.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="position">The position.</param>
    /// <param name="step">The steps to add.</param>
    public static void EnsureMinimumSize(ref int[] array, long position, long step = 16)
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
