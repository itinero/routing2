namespace Itinero.MapMatching.Model;

/// <summary>
/// The model builder settings.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ModelBuilderSettings
{
    /// <summary>
    /// The maximum distance ratio between great circle distance and the route distance between two samples.
    /// </summary>
    public double MaxDistanceRatio { get; set; } = 3.0;

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
}
