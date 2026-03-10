using Itinero.MapMatching.Model;
using Itinero.Profiles;

namespace Itinero.MapMatching;

/// <summary>
/// The map matcher settings.
/// </summary>
public class MapMatcherSettings
{
    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Profile? Profile { get; set; }

    /// <summary>
    /// The maximum distance ratio between great circle distance and the route distance between two samples.
    /// </summary>
    public double MaxDistanceRatio { get; set; } = 5.0;

    /// <summary>
    /// The maximum distance between a sample and a potential snapping point.
    /// </summary>
    public double MaxSnappingDistance { get; set; } = 50;

    /// <summary>
    /// Ratio between node costs and transition costs
    /// </summary>
    /// <remarks>
    /// 1 = only transitions.
    /// 0 = only node weights.
    /// </remarks>
    public double TransitionOrSnappingRatio { get; set; } = 0.5;

    internal ModelBuilderSettings ModelBuilderSettings
    {
        get
        {
            return new ModelBuilderSettings()
            {
                MaxDistanceRatio = this.MaxDistanceRatio,
                MaxSnappingDistance = this.MaxSnappingDistance,
                TransitionOrSnappingRatio = this.TransitionOrSnappingRatio
            };
        }
    }
}
