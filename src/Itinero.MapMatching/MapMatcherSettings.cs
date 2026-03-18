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
    /// GPS noise standard deviation in meters. Controls the emission probability (Gaussian).
    /// Lower values make the matcher prefer candidates closer to the GPS point.
    /// </summary>
    public double SigmaZ { get; set; } = 10.0;

    /// <summary>
    /// Transition probability parameter in meters. Controls the exponential distribution
    /// over the absolute difference between great-circle distance and route distance.
    /// Higher values are more tolerant of indirect routes.
    /// </summary>
    public double Beta { get; set; } = 5.0;

    /// <summary>
    /// The search radius in meters for finding snap point candidates.
    /// </summary>
    public double SearchRadius { get; set; } = 50;

    /// <summary>
    /// Minimum distance in meters between consecutive track points.
    /// Points closer than this are skipped to avoid noise-induced issues.
    /// </summary>
    public double MinPointDistance { get; set; } = 10;

    /// <summary>
    /// Maximum number of consecutive unmatched track points to skip before breaking the model.
    /// </summary>
    public int MaxPointSkip { get; set; } = 3;

    /// <summary>
    /// Maximum great-circle distance in meters between consecutive track points
    /// before the trace is split into separate models.
    /// </summary>
    public double BreakageDistance { get; set; } = 2000;

    /// <summary>
    /// Maximum factor applied to the great-circle distance between consecutive points
    /// to limit routing search distance.
    /// </summary>
    public double MaxRouteDistanceFactor { get; set; } = 5.0;

    internal ModelBuilderSettings ModelBuilderSettings
    {
        get
        {
            return new ModelBuilderSettings()
            {
                SigmaZ = this.SigmaZ,
                Beta = this.Beta,
                SearchRadius = this.SearchRadius,
                MinPointDistance = this.MinPointDistance,
                MaxPointSkip = this.MaxPointSkip,
                BreakageDistance = this.BreakageDistance,
                MaxRouteDistanceFactor = this.MaxRouteDistanceFactor
            };
        }
    }
}
