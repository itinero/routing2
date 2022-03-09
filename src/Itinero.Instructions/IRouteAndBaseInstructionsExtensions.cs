using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions;

/// <summary>
/// Extensions on top of a generated route and base instructions object.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class IRouteAndBaseInstructionsExtensions
{
    /// <summary>
    /// Generates text for all the base instructions for the given language code.
    /// </summary>
    /// <param name="routeAndBaseInstructions">The route and base instructions.</param>
    /// <param name="languageCode">The language code.</param>
    public static IRouteAndInstructions ForLanguage(this IRouteAndBaseInstructions routeAndBaseInstructions, 
        string languageCode = "en")
    {
        if (!routeAndBaseInstructions.Languages.TryGetValue(languageCode, out var languageGenerator)) {
            throw new ArgumentOutOfRangeException($"Language {languageCode} not configured");
        }

        var instructions = routeAndBaseInstructions.BaseInstructions.Select(baseInstruction => 
            new Instruction(baseInstruction, new Dictionary<string, string>() {{ languageCode, languageGenerator.ToText(baseInstruction) }} )).ToList();

        return new RouteAndInstructions(routeAndBaseInstructions.Route, instructions);
    }
    
    /// <summary>
    /// Generates text for all the base instructions for the given language code.
    /// </summary>
    /// <param name="routeAndBaseInstructions">The route and base instructions.</param>
    /// <param name="languageCodes">The language codes.</param>
    public static IRouteAndInstructions ForLanguage(this IRouteAndBaseInstructions routeAndBaseInstructions, 
        IEnumerable<string> languageCodes)
    {
        var languageGenerators = languageCodes.Select(languageCode => {
            if (!routeAndBaseInstructions.Languages.TryGetValue(languageCode, out var languageGenerator)) {
                throw new ArgumentOutOfRangeException($"Language {languageCode} not configured");
            }

            return (languageCode, languageGenerator);
        });

        var instructions = routeAndBaseInstructions.BaseInstructions.Select(baseInstruction => {
            var text = new Dictionary<string, string>();
            foreach (var (languageCode, languageGenerator) in languageGenerators) {
                text[languageCode] = languageGenerator.ToText(baseInstruction);
            }

            return new Instruction(baseInstruction, text);
        }).ToList();

        return new RouteAndInstructions(routeAndBaseInstructions.Route, instructions);
    }
}