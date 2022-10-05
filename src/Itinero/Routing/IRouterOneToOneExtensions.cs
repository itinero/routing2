using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Routes;
using Itinero.Routes.Paths;

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
    /// <returns>The path.</returns>
    public static async Task<Result<Path>> Path(this IRouterOneToOne oneToOneRouter, CancellationToken cancellationToken)
    {
        var sources = new[] { oneToOneRouter.Source };
        var targets = new[] { oneToOneRouter.Target };

        if (!sources.TryToUndirected(out var sourcesUndirected) ||
            !targets.TryToUndirected(out var targetsUndirected))
        {
            return (await oneToOneRouter.CalculateAsync(sources, targets)).First().First();
        }

        return (await oneToOneRouter.CalculateAsync(sourcesUndirected, targetsUndirected, cancellationToken)).First().First();
    }

    /// <summary>
    /// Calculates the route.
    /// </summary>
    /// <param name="oneToOneRouter">The router.</param>
    /// <returns>The route.</returns>
    public static async Task<Result<Route>> CalculateAsync(this IRouterOneToOne oneToOneRouter, CancellationToken cancellationToken = default)
    {
        var path = await oneToOneRouter.Path(cancellationToken);
        if (path.IsError)
        {
            return new Result<Route>(path.ErrorMessage);
        }
        return oneToOneRouter.Settings.RouteBuilder.Build(oneToOneRouter.Network, oneToOneRouter.Settings.Profile, path.Value);
    }

    /// <summary>
    /// Calculates the weights.
    /// </summary>
    /// <param name="oneToOneWeightRouter">The router.</param>
    /// <returns>The weight</returns>
    public static Task<Result<double?>> CalculateAsync(this IRouterWeights<IRouterOneToOne> oneToOneWeightRouter)
    {
        return Task.FromResult(null);

        // var profileHandler = oneToOneWeightRouter.Router.Network.GetCostFunctionFor(
        //     oneToOneWeightRouter.Router.Settings.Profile);
        // return oneToOneWeightRouter.Router.Path().Weight(profileHandler.GetForwardWeight);
    }
}
