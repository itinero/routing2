using System;
using Itinero.Routes;

namespace Itinero.Instructions
{
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

        public static string DegreesToText(this int degrees)
        {
            var cutoff = 30;
            if (-cutoff < degrees && degrees < cutoff)
            {
                return "straight on";
            }

            var direction = "left";
            if (degrees > 0)
            {
                direction = "right";
            }

            degrees = Math.Abs(degrees);
            if (degrees > 180 - cutoff)
            {
                return "sharp " + direction;
            }

            if (degrees < 2 * cutoff)
            {
                return "slightly " + direction;
            }

            return direction;
        }
    }
}
