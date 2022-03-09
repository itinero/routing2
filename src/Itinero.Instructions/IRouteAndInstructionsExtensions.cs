using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Routes;

namespace Itinero.Instructions;

/// <summary>
/// Contains extension methods for further operations after instruction generation.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class IRouteAndInstructionsExtensions
{
    /// <summary>
    /// Adds the instructions to the route object.
    /// </summary>
    /// <param name="routeAndInstructions">The route and the instructions.</param>
    /// <param name="keyForLanguage">A callback to configure the key per language code.</param>
    /// <returns>Another augmented route and the same instructions.</returns>
    public static IRouteAndInstructions AugmentRoute(this IRouteAndInstructions routeAndInstructions, Func<string, string>? keyForLanguage = null)
    {
        // 'instructions' and 'shapeMeta' might have different boundaries - so reusing the old shapeMeta's is not possible

        keyForLanguage ??= l => $"instruction:{l}";
        
        var route = routeAndInstructions.Route;
        var shapeMetas = routeAndInstructions.Route.ShapeMeta;
        var instructions = routeAndInstructions.Instructions;
        
        var metas = new List<Route.Meta>();
        var instructionPointer = 0;
        var shapeMetaPointer = 0;
        var routeCount = shapeMetas.Last().Shape;

        Route.Meta? lastMeta = null;
        while ((lastMeta?.Shape ?? 0) < routeCount && instructionPointer < instructions.Count &&
               shapeMetaPointer < shapeMetas.Count) {
            var currentMeta = shapeMetas[shapeMetaPointer];
            var currentInstruction = instructions[instructionPointer];


            var latestIncludedPoint = Math.Min(
                currentMeta.Shape,
                currentInstruction.BaseInstruction.ShapeIndexEnd);

            var attributes = new List<(string key, string value)>(currentMeta.Attributes);
            foreach (var (languageCode, text) in currentInstruction.Text) {
                attributes.Add((keyForLanguage(languageCode), text));
            }
            var distance = route.DistanceBetween(lastMeta?.Shape ?? 0, latestIncludedPoint);
            var speed = currentMeta.Distance / currentMeta.Time;

            var meta = new Route.Meta {
                Shape = latestIncludedPoint,
                AttributesAreForward = currentMeta.AttributesAreForward,
                Attributes = attributes,
                Profile = currentMeta.Profile,
                Distance = distance,
                Time = speed * distance
            };

            if (currentMeta.Shape == meta.Shape) {
                shapeMetaPointer++;
            }

            if (currentInstruction.BaseInstruction.ShapeIndexEnd == meta.Shape) {
                instructionPointer++;
            }

            metas.Add(meta);
            lastMeta = meta;
        }

        var augmentedRoute = new Route {
            Attributes = route.Attributes,
            Branches = route.Branches,
            Profile = route.Profile,
            Shape = route.Shape,
            Stops = route.Stops,
            TotalDistance = route.TotalDistance,
            TotalTime = route.TotalTime,
            ShapeMeta = metas
        };

        return new RouteAndInstructions(augmentedRoute, instructions);
    }
}