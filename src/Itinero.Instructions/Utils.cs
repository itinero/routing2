using System;
using Itinero.Routes;

namespace Itinero.Instructions;

internal class Box<T>
{
    public T Content { get; set; }
}

internal static class Utils
{

    /**
     * Normalizes degrees to be between -180 (incl) and 180 (excl)
     */
    internal static int NormalizeDegrees(this double degrees)
    {
        if (degrees <= -180)
        {
            degrees += 360;
        }

        if (degrees > 180)
        {
            degrees -= 360;
        }

        return (int)degrees;
    }

}
