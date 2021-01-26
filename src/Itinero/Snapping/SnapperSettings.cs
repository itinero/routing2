namespace Itinero.Snapping {
    /// <summary>
    /// A settings objects for snapping options.
    /// </summary>
    public class SnapperSettings {
        /// <summary>
        /// A flag to enable the option of using any profile as valid instead of all.
        /// </summary>
        public bool AnyProfile { get; set; } = false;

        /// <summary>
        /// A flag to check the can stop on data.
        /// </summary>
        public bool CheckCanStopOn { get; set; } = true;

        /// <summary>
        /// Gets the maximum offset in meter.
        /// </summary>
        public double MaxOffsetInMeter { get; set; } = 1000;
    }
}