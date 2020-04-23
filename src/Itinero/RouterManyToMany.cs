using System.Collections.Generic;

namespace Itinero
{
    public class RouterManyToMany
    {
        internal Router Router { get; set; }
        
        internal IReadOnlyList<(SnapPoint sp, bool? direction)> Sources { get; set; }
        
        internal IReadOnlyList<(SnapPoint sp, bool? direction)> Targets { get; set; }
    }

    /// <summary>
    /// Contains extension methods for the many to one router.
    /// </summary>
    public static class RouterManyToManyExtensions
    {
        /// <summary>
        /// Configures a many to one route.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="points">The departure locations.</param>
        /// <returns>A route.</returns>
        public static RouterManyToMany To(this RouterManyToOne router, IReadOnlyList<SnapPoint> points)
        {
            return new RouterManyToMany()
            {
                Router = router.Router,
                Sources = router.Sources,
                Targets = points.ToDirected()
            };
        }
    }
}