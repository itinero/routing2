namespace Itinero.Snapping;

/// <summary>
/// A settings objects for snapping options.
/// </summary>
public class SnapperSettings
{
    /// <summary>
    /// A flag to enable the option of using any profile as valid instead of all.
    /// </summary>
    public bool AnyProfile { get; set; } = false;

    /// <summary>
    /// A flag to check the can stop on data.
    /// </summary>
    public bool CheckCanStopOn { get; set; } = true;

    /// <summary>
    /// The maximum distance of any snapping point or vertex returned.
    /// </summary>
    public double MaxDistance { get; set; } = 100;

    /// <summary>
    /// The offset in meter for the bounding box to check for potential edges or vertices.
    /// </summary>
    public double OffsetInMeter { get; set; } = 100;

    /// <summary>
    /// The maximum offset in meter for the bounding box to check for potential edges or vertices. This is used when there is nothing found and is different from OffsetInMeter.
    /// </summary>
    public double OffsetInMeterMax { get; set; } = 100;
}
