using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Routes;
using Itinero.Routes.Paths;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;

namespace Itinero.Routing;

/// <summary>
/// One to one extensions.
/// </summary>
public static class IRouterOneToOneExtensions
{
    /// <summary>
    /// Calculates the path.
    /// </summary>
    /// <param name="oneToOneRouter">The router.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The path.</returns>
    public static async Task<Result<Path>> PathAsync(this IRouterOneToOne oneToOneRouter,
        CancellationToken cancellationToken = default)
    {
        if (oneToOneRouter.Source.direction == null && oneToOneRouter.Target.direction == null)
        {
            return await oneToOneRouter.CalculateAsync(oneToOneRouter.Source.sp, oneToOneRouter.Target.sp,
                cancellationToken);
        }

        return (await oneToOneRouter.CalculateAsync([oneToOneRouter.Source], [oneToOneRouter.Target]))[0][0];
    }

    /// <summary>
    /// Calculates the route.
    /// </summary>
    /// <param name="oneToOneRouter">The router.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The route.</returns>
    public static async Task<Result<Route>> CalculateAsync(this IRouterOneToOne oneToOneRouter,
        CancellationToken cancellationToken = default)
    {
        var path = await oneToOneRouter.PathAsync(cancellationToken);
        if (path.IsError)
        {
            return new Result<Route>(path.ErrorMessage);
        }

        return oneToOneRouter.Settings.RouteBuilder.Build(oneToOneRouter.Network, oneToOneRouter.Settings.Profile,
            path.Value);
    }

    /// <summary>
    /// Calculates the weights.
    /// </summary>
    /// <param name="oneToOneWeightRouter">The router.</param>
    /// <returns>The weight</returns>
    public static Task<Result<double?>> CalculateAsync(this IRouterWeights<IRouterOneToOne> oneToOneWeightRouter)
    {
        return Task.FromResult(new Result<double?>("Not implemented"));

        // var profileHandler = oneToOneWeightRouter.Router.Network.GetCostFunctionFor(
        //     oneToOneWeightRouter.Router.Settings.Profile);
        // return oneToOneWeightRouter.Router.Path().Weight(profileHandler.GetForwardWeight);
    }

    internal static async Task<Result<Path>> CalculateAsync(this IRouterOneToOne oneToOneRouter,
        SnapPoint source, SnapPoint target, CancellationToken cancellationToken)
    {
        var settings = oneToOneRouter.Settings;
        var routingNetwork = oneToOneRouter.Network;

        var profile = settings.Profile;
        var costFunction = routingNetwork.GetCostFunctionFor(profile);

        var maxBox = settings.MaxBoxFor(routingNetwork, [source, target]);

        var bidirectionalDijkstra = BidirectionalDijkstra.ForNetwork(routingNetwork);

        var (result, _) = await bidirectionalDijkstra.RunAsync(source, target, costFunction, async v =>
        {
            if (!routingNetwork.UsageNotifier.IsVertexDataReady(routingNetwork, v))
            {
                await routingNetwork.UsageNotifier.NotifyVertex(routingNetwork, v, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested) return false;
            return CheckMaxDistance(v);
        }, cancellationToken: cancellationToken);

        if (result == null) return new Result<Path>("Path not found");

        return result;

        bool CheckMaxDistance(VertexId v)
        {
            if (routingNetwork == null) throw new Exception("Router cannot be null here.");
            if (maxBox == null) return false;

            var vertex = routingNetwork.GetVertex(v);
            if (!maxBox.Value.Overlaps(vertex))
            {
                return true;
            }

            return false;
        }
    }
}
