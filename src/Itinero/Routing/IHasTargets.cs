using System.Collections.Generic;

namespace Itinero.Routing
{
    /// <summary>
    /// Abstract representation of a router with targets.
    /// </summary>
    public interface IHasTargets
    {
        /// <summary>
        /// Gets the targets.
        /// </summary>
        IReadOnlyList<(SnapPoint sp, bool? direction)> Targets { get; }
    }
}