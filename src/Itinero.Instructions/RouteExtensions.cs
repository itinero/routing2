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
    }
}