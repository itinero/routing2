namespace Itinero.MapMatching.Model;

/// <summary>
/// The model builder settings.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ModelBuilderSettings
{
    /// <summary>
    /// GPS noise standard deviation in meters. Controls the emission probability (Gaussian).
    /// Lower values make the matcher prefer candidates closer to the GPS point.
    /// </summary>
    /// <remarks>
    /// Newson &amp; Krumm (2009) use 4.07m (high quality GPS).
    /// GraphHopper uses 50m (very permissive).
    /// OSRM uses 5m. A value of 10m is a reasonable middle ground for consumer GPS.
    /// </remarks>
    public double SigmaZ { get; set; } = 10.0;

    /// <summary>
    /// Transition probability parameter in meters. Controls the exponential distribution
    /// over the absolute difference between great-circle distance and route distance.
    /// Higher values are more tolerant of indirect routes.
    /// </summary>
    /// <remarks>
    /// GraphHopper uses 2.0, Valhalla uses 3.0.
    /// </remarks>
    public double Beta { get; set; } = 5.0;

    /// <summary>
    /// The search radius in meters for finding snap point candidates.
    /// </summary>
    public double SearchRadius { get; set; } = 50;

    /// <summary>
    /// Minimum distance in meters between consecutive track points.
    /// Points closer than this are skipped to avoid noise-induced issues.
    /// </summary>
    /// <remarks>
    /// Valhalla uses 10m. GraphHopper filters points closer than 2 * sigma.
    /// </remarks>
    public double MinPointDistance { get; set; } = 10;

    /// <summary>
    /// Maximum number of consecutive unmatched track points to skip before breaking the model.
    /// </summary>
    public int MaxPointSkip { get; set; } = 3;

    /// <summary>
    /// Maximum great-circle distance in meters between consecutive track points
    /// before the trace is split into separate models.
    /// </summary>
    /// <remarks>
    /// Valhalla uses 2000m.
    /// </remarks>
    public double BreakageDistance { get; set; } = 2000;

    /// <summary>
    /// Maximum factor applied to the great-circle distance between consecutive points
    /// to limit routing search distance.
    /// </summary>
    public double MaxRouteDistanceFactor { get; set; } = 5.0;
}
