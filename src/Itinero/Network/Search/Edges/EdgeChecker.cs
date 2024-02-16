using System.Threading;
using System.Threading.Tasks;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search.Edges;

/// <summary>
/// Abstract definition of an edge checker.
/// </summary>
public interface IEdgeChecker
{
    /// <summary>
    /// A fast-path check that returns a boolean if the status of the edge is known, otherwise returns null and an advanced check needs to be done.
    /// </summary>
    /// <param name="edgeEnumerator"></param>
    /// <returns></returns>
    bool? IsAcceptable(IEdgeEnumerator<RoutingNetwork> edgeEnumerator);

    /// <summary>
    /// Runs the checks for the given edge.
    /// </summary>
    /// <param name="edgeEnumerator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> RunCheckAsync(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, CancellationToken cancellationToken = default);
}
