namespace Itinero.Routing.Flavours.Dijkstra.EdgeBased
{
    internal static class Extensions
    {
        public static bool Forward(this (SnapPoint sp, bool? direction) point)
        {
            return point.direction == null || point.direction.Value;
        }
        
        public static bool Backward(this (SnapPoint sp, bool? direction) point)
        {
            return point.direction == null || !point.direction.Value;
        }
    }
}