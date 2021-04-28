namespace Itinero.Routing.Alternatives
{
    public class AlternativeRouteSettings
    {

        public int MaxNumberOfAlternativeRoutes { get; set;  }= 3;
        

        /// <summary>
        /// The percentage of edges that are allowed to be overlapping with any other route.
        /// </summary>
        public double MaxPercentageOfEqualEdges { get; set; } = 0.2;

    }
}