using System.Collections.Generic;
using Itinero.Snapping;

namespace Itinero.Routing
{
    /// <summary>
    /// Abstract representation of a router with sources.
    /// </summary>
    public interface IHasSources : IRouter
    {
        /// <summary>
        /// Gets the sources.
        /// </summary>
        IReadOnlyList<(SnapPoint sp, bool? direction)> Sources { get; }
    }
}