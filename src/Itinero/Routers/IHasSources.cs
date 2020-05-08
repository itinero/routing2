using System.Collections.Generic;

namespace Itinero.Routers
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