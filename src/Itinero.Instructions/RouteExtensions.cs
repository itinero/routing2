using Itinero.Routes;

namespace Itinero.Instructions
{
    public static class RouteExtensions
    {
        /// <summary>
        ///     Adds instructions to the route,
        /// </summary>
        /// <returns></returns>
        public static RouteWithInstructions WithInstructions(this Route route, InstructionsGenerator generator)
        {
            return new(route, generator);
        }

        /// <summary>
        ///     Adds the default instructions to the route,
        /// </summary>
        /// <returns></returns>
        public static RouteWithInstructions WithInstructions(this Route route)
        {
            return new(route, InstructionsGenerator.Default);
        }

        public static double DistanceBetween(this Route route, int shapeStart, int shapeEnd)
        {
            var sum = 0.0;
            for (var i = shapeStart; i < shapeEnd; i++) {
                sum += Utils.DistanceEstimateInMeter(route.Shape[i], route.Shape[i + 1]);
            }

            return sum;
        }

        /// <summary>
        ///     Gives the absolute bearing if travelling from the given shapeindex towards the next shapeIndex.
        /// </summary>
        /// <remarks>0° is north, 90° is east, -90° is west, both 180 and -180 are south. Gives null for the last point</remarks>
        /// <param name="route"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public static double? BearingAt(this Route route, int shape)
        {
            if (route.Shape.Count < shape + 2) { // Plus two, as we'll increase shape later on
                return null;
            }

            var current = route.Shape[shape];
            var next = route.Shape[shape + 1];

            return Utils.AngleBetween(current, next);
        }
    }
}