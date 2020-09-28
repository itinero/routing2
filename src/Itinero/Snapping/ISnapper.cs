using System;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    /// <summary>
    /// Abstract representation of a snapper.
    /// </summary>
    public interface ISnapper
    {
        /// <summary>
        /// Use the given profile for snapping.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="settings">Function to set the settings.</param>
        /// <returns>The setup snappable.</returns>
        ISnappable Using(Profile profile, Action<SnapperSettings>? settings = null);
    }
}