namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// A lower bound on the cost still to pay from a location to the nearest target, in
/// edge cost units. Supplying one turns the search into A*; 0 is always valid and
/// reduces it to Dijkstra. Overestimating returns worse routes with no error.
/// </summary>
internal delegate double HeuristicFunc(double longitude, double latitude);
