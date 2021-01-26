using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    /// <summary>
    /// Abstract representation of a snapper.
    /// </summary>
    public interface ISnapper : ILocationsSnapper
    {
        /// <summary>
        /// Use the given settings for snapping.
        /// </summary>
        /// <param name="settings">Function to set the settings.</param>
        /// <returns>The setup snappable.</returns>
        ILocationsSnapper Using(Action<SnapperSettings> settings);

        /// <summary>
        /// Use the given profile for snapping.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="settings">Function to set the settings.</param>
        /// <returns>The setup snappable.</returns>
        ILocationsSnapper Using(Profile profile, Action<SnapperSettings>? settings = null);

        /// <summary>
        /// Snaps to the given vertices.
        /// </summary>
        /// <param name="vertices">The vertices to snap to.</param>
        /// <returns>The results if any. Snapping will fail if a vertex has no edges.</returns>
        IEnumerable<Result<SnapPoint>> To(IEnumerable<(VertexId vertexId, EdgeId? edgeId)> vertices);
    }
}