namespace Itinero.Routing.Alternatives
{
    /// <summary>
    /// The alternative route settings.
    /// </summary>
    public class AlternativeRouteSettings
    {
        /// <summary>
        /// The maximum number of alternative routes.
        /// </summary>
        /// <remarks>
        /// It is possible less alternatives are returned depending on the other settings.
        /// </remarks>
        public int MaxNumberOfAlternativeRoutes { get; set; } = 3;
        
        /// <summary>
        /// The percentage of edges that are allowed to be overlapping with any other route.
        /// </summary>
        /// <remarks>
        /// When an alternative overlaps more than this given threshold is will not be considered a viable alternative route.
        /// </remarks>
        public double MaxPercentageOfEqualEdges { get; set; } = 0.2;

        /// <summary>
        /// The maximum increase in weight.
        /// </summary>
        /// <remarks>
        /// When the weight of the next potential alternative exceeds the weight in this setting the search for alternatives stops. 
        /// </remarks>
        public double MaxWeightIncreasePercentage { get; set; } = 1.5;
    }
}