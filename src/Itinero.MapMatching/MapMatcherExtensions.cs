using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;

namespace Itinero.MapMatching;

/// <summary>
/// Contains extensions
/// </summary>
public static class MapMatcherExtensions
{
    /// <summary>
    /// Returns a route for each segment of the track matched.
    /// </summary>
    /// <param name="matcher">The matcher.</param>
    /// <param name="match">The match.</param>
    /// <returns>The resulting route(s).</returns>
    public static Result<IEnumerable<Route>> Routes(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        var routes = new List<Route>();
        foreach (var path in match)
        {
            var route = RouteBuilder.Default.Build(matcher.RoutingNetwork, matcher.Settings.Profile, path);
            routes.Add(route);
        }

        return routes;
    }

    public static IEnumerable<Route> Routes(this MapMatcher matcher, IEnumerable<MapMatch> matches)
    {
        foreach (var match in matches)
        {
            foreach (var r in matcher.Route(match))
            {
                yield return r;
            }
        }
    }

    public static IEnumerable<Route> Route(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        var paths = matcher.MergedPaths(match);

        var routes = new List<Route>();
        foreach (var path in paths)
        {
            var route = RouteBuilder.Default.Build(matcher.RoutingNetwork, matcher.Settings.Profile, path);
            routes.Add(route);
        }

        return routes;
    }

    public static IEnumerable<Path> MergedPaths(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        IEnumerable<Path> current = match;
        while (true)
        {
            var (merged1, hasMerges1) = MapMatcherExtensions.MergePaths(current,
                (p1, p2) => p1.TryAppend(p2));

            var (merged2, hasMerges2) = MapMatcherExtensions.MergePaths(merged1,
                (p1, p2) => p1.TryMergeAsUTurn(p2));
            current = merged2;

            if (!hasMerges1 && !hasMerges2) break;
        }

        return current;
    }

    private static (IEnumerable<Path> paths, bool merged) MergePaths(IEnumerable<Path> paths, Func<Path, Path, Path?> merge)
    {
        var mergedPaths = new List<Path>();

        Path? currentPath = null;
        var hasMerges = false;
        foreach (var path in paths)
        {
            path.Trim();
            if (!path.HasLength()) continue;

            if (currentPath == null)
            {
                currentPath = path;
                continue;
            }

            var merged = merge(currentPath, path);
            if (merged == null)
            {
                mergedPaths.Add(currentPath);
                currentPath = path;
                continue;
            }

            hasMerges = true;
            currentPath = merged;
        }

        if (currentPath != null)
        {
            mergedPaths.Add(currentPath);
        }

        return (mergedPaths, hasMerges);
    }

    private static Path? TryAppend(this Path path, Path next)
    {
        if (!path.IsNext(next)) return null;

        return new[] { path, next }.Merge();
    }
}
