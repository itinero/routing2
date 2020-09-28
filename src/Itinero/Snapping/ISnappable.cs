using System.Collections.Generic;
using Itinero.Network;

namespace Itinero.Snapping
{
    /// <summary>
    /// A snapper end point, all setup and ready to use.
    /// </summary>
    public interface ISnappable
    {
        /// <summary>
        /// Snaps to the given locations.
        /// </summary>
        /// <param name="locations">The locations.</param>
        /// <returns>The snapped locations.</returns>
        IEnumerable<Result<SnapPoint>> To(IEnumerable<(double longitude, double latitude)> locations);

        /// <summary>
        /// Snaps to the given vertices.
        /// </summary>
        /// <param name="vertices">The vertices to snap to.</param>
        /// <returns>The results if any. Snapping will fail if a vertex has no edges.</returns>
        IEnumerable<Result<SnapPoint>> To(IEnumerable<(VertexId vertexId, EdgeId? edgeId)> vertices);
    }
}