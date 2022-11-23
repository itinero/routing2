using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routes.Paths;

/// <summary>
/// Extensions for path.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// Calculates the weight of the path given the weight function.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="getWeight">The weight function.</param>
    /// <returns>The total weight.</returns>
    public static double? Weight(this Result<Path> path, Func<RoutingNetworkEdgeEnumerator, double> getWeight)
    {
        if (path.IsError)
        {
            return null;
        }

        return path.Value.Weight(getWeight);
    }

    /// <summary>
    /// Calculates the weight of the path given the weight function.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="getWeight">The weight function.</param>
    /// <returns>The total weight.</returns>
    public static double Weight(this Path path, Func<RoutingNetworkEdgeEnumerator, double> getWeight)
    {
        var weight = 0.0;

        var edgeEnumerator = path.RoutingNetwork.GetEdgeEnumerator();
        foreach (var (edge, direction, offset1, offset2) in path)
        {
            if (!edgeEnumerator.MoveTo(edge, direction))
            {
                throw new InvalidDataException("An edge in the path is not found!");
            }

            var edgeWeight = getWeight(edgeEnumerator);
            if (offset1 != 0 || offset2 != ushort.MaxValue)
            {
                edgeWeight *= (double)(offset2 - offset1) / ushort.MaxValue;
            }

            weight += edgeWeight;
        }

        return weight;
    }

    /// <summary>
    /// Returns true if the path is next.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="next">The next path.</param>
    /// <returns>True if the next path</returns>
    public static bool IsNext(this Path path, Path next)
    {
        var last = path.Last;
        var first = next.First;

        // check if the same edge and if the offsets match.
        if (last.edge == first.edge &&
            last.direction == first.direction)
        {
            return path.Offset2 == next.Offset1;
        }

        // check if the same vertices at the end.
        if (next.Offset1 != 0 ||
            path.Offset2 != ushort.MaxValue)
        {
            return false;
        }

        var edgeEnumerator = path.RoutingNetwork.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(last.edge, last.direction);
        var lastVertex = edgeEnumerator.Head;
        edgeEnumerator.MoveTo(first.edge, first.direction);
        var firstVertex = edgeEnumerator.Tail;

        return lastVertex == firstVertex;
    }

    /// <summary>
    /// Merges the paths.
    /// </summary>
    /// <param name="paths">The paths.</param>
    /// <returns>The merged path.</returns>
    public static Path? Merge(this IEnumerable<Path> paths)
    {
        Path? merged = null;
        RoutingNetworkEdgeEnumerator? enumerator = null;
        foreach (var path in paths)
        {
            merged ??= new Path(path.RoutingNetwork);
            enumerator ??= path.RoutingNetwork.GetEdgeEnumerator();

            if (merged.Count == 0)
            {
                merged.Offset1 = path.Offset1;
            }
            else if (!merged.IsNext(path))
            {
                throw new InvalidDataException(
                    $"Paths cannot be concatenated.");
            }

            var isFirst = true;
            foreach (var (edge, direction, _, _) in path)
            {
                if (isFirst)
                {
                    isFirst = false;
                    if (merged.Count > 0 &&
                        merged.Last.edge == edge)
                    {
                        // first edge may be the same edge as
                        // previous path, don't add it again.
                        continue;
                    }
                }
                
                merged.Append(edge, direction);
            }

            merged.Offset2 = path.Offset2;
        }

        return merged;
    }

    /// <summary>
    /// Removes the first and/or last edge if they are not part of the path via the offset properties.
    /// </summary>
    /// <param name="path">The path.</param>
    public static void Trim(this Path path)
    {
        if (path.Count <= 1)
        {
            return;
        }

        if (path.Offset1 == ushort.MaxValue)
        {
            path.RemoveFirst();
        }

        if (path.Count <= 1)
        {
            return;
        }

        if (path.Offset2 == 0)
        {
            path.RemoveLast();
        }
    }

    public static bool HasLength(this Path path)
    {
        if (path.Count > 1) return true;

        return path.Offset1 != path.Offset2;
    }
}
