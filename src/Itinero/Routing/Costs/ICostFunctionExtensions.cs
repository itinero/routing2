using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Costs {
    internal static class ICostFunctionExtensions {
        public static DijkstraWeightFunc GetDijkstraWeightFunc(this ICostFunction costFunction) {
            return (enumerator, edges) => {
                var (_, _, cost, turnCost) = costFunction.Get(enumerator, true, edges);

                return (cost, turnCost);
            };
        }
    }
}