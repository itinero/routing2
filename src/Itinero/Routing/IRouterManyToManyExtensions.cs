using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;

namespace Itinero.Routing;

/// <summary>
/// Many to many extensions.
/// </summary>
public static class IRouterManyToManyExtensions
{
    /// <summary>
    /// Calculates the paths.
    /// </summary>
    /// <param name="manyToManyRouter">The router.</param>
    /// <returns>The paths.</returns>
    public static async Task<IReadOnlyList<IReadOnlyList<Result<Path>>>> Paths(this IRouterManyToMany manyToManyRouter, CancellationToken cancellationToken)
    {
        var sources = manyToManyRouter.Sources;
        var targets = manyToManyRouter.Targets;

        if (!sources.TryToUndirected(out var sourcesUndirected) ||
            !targets.TryToUndirected(out var targetsUndirected))
        {
            return await manyToManyRouter.CalculateAsync(sources, targets);
        }
        else
        {
            return await manyToManyRouter.CalculateAsync(sourcesUndirected, targetsUndirected, cancellationToken);
        }
    }

    /// <summary>
    /// Calculates the routes.
    /// </summary>
    /// <param name="manyToManyRouter">The router.</param>
    /// <returns>The paths.</returns>
    public static async Task<IReadOnlyList<IReadOnlyList<Result<Route>>>> Calculate(this IRouterManyToMany manyToManyRouter, CancellationToken cancellationToken)
    {
        var start = DateTime.Now.Ticks;
        var paths = await manyToManyRouter.Paths(cancellationToken);
        var end = DateTime.Now.Ticks;
        Console.WriteLine(new TimeSpan(end - start));
        return paths.Select(x =>
        {
            return x.Select(y =>
            {
                if (y.IsError) return y.ConvertError<Route>();

                return manyToManyRouter.Settings.RouteBuilder.Build(manyToManyRouter.Network,
                    manyToManyRouter.Settings.Profile, y);
            }).ToArray();
        }).ToArray();
    }

    /// <summary>
    /// Calculates the weights.
    /// </summary>
    /// <param name="manyToManyWeightRouter">The router.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Task<Result<IReadOnlyList<IReadOnlyList<double?>>>> Calculate(
        this IRouterWeights<IRouterManyToMany> manyToManyWeightRouter)
    {
        return Task.FromResult(null);

        // var profileHandler = manyToManyWeightRouter.Router.Network.GetCostFunctionFor(
        //     manyToManyWeightRouter.Router.Settings.Profile);
        // var paths = manyToManyWeightRouter.Router.Paths();
        // return paths.Select(x =>
        // {
        //     return x.Select(y => y.Weight(profileHandler.GetForwardWeight)).ToArray();
        // }).ToArray();
    }
}
