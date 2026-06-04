using Itinero.Network;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// Profile-bound lookup of the main-N classification for an edge.
/// </summary>
/// <param name="edgeId">The edge.</param>
/// <param name="isLocalAccess">L-tag for the edge (from the cost function's <c>localAccess</c>
/// field). Passed in rather than re-derived because the caller already has it.</param>
/// <returns>
/// <c>true</c>: edge is in main-N; <c>false</c>: edge is not in main-N (island, local pocket,
/// or L-tagged); <c>null</c>: classification has not yet produced a verdict for this tile.
/// </returns>
/// <remarks>
/// Returned by <see cref="IsMainNFuncExtensions.GetIsMainNFunc"/> as a profile-bound closure
/// over <see cref="RoutingNetworkIslandManager.IsMainN"/>. Consumed by the access-aware
/// edge-based Dijkstra to drive the leftMain state-bit transitions.
/// </remarks>
internal delegate bool? IsMainNFunc(EdgeId edgeId, bool isLocalAccess);
