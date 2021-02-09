using System.Collections.Generic;
using Itinero.Instructions.Types;
using Itinero.Routes;

namespace Itinero.Instructions
{
    public static class RouteExtensions
    {
        /// <summary>
        ///     Adds instructions to the route,
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public static RouteWithInstructions WithInstructions(this Route route, InstructionsGenerator generator)
        {
            return new(route, generator);
        }


    }
}