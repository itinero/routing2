using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Itinero.Instructions.Configuration;
using Itinero.Instructions.ToText;
using Itinero.Instructions.Types.Generators;

namespace Itinero.Instructions;

/// <summary>
/// Settings for the route instruction generator.
/// </summary>
public class RouteInstructionGeneratorSettings
{
    /// <summary>
    /// Individual instruction generators.
    /// </summary>
    public List<IInstructionGenerator> Generators { get; private set; } = new List<IInstructionGenerator>();

    /// <summary>
    /// Text generators per language code.
    /// </summary>
    public Dictionary<string, IInstructionToText> Languages { get; private set; } =
        new Dictionary<string, IInstructionToText>();
    
    private static readonly Lazy<RouteInstructionGeneratorSettings> DefaultLazy = new(() => {
        using var stream =
            typeof(IndexedRoute).Assembly.GetManifestResourceStream(
                "Itinero.Instructions.Configuration.default.json");
        return FromStream(stream ?? throw new InvalidOperationException("Default not found"));
    });
    
    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static RouteInstructionGeneratorSettings Default => DefaultLazy.Value;

    /// <summary>
    /// Loads settings from a file.
    /// </summary>
    /// <param name="path">The path to the settings file.</param>
    /// <param name="customGenerators">The custom generators.</param>
    /// <returns>The settings.</returns>
    public static RouteInstructionGeneratorSettings FromConfigFile(string path,
        IEnumerable<IInstructionGenerator>? customGenerators = null)
    {
        return FromStream(File.OpenRead(path));
    }
    
    /// <summary>
    /// Parses settings from the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="customGenerators">Any custom generators.</param>
    /// <returns></returns>
    public static RouteInstructionGeneratorSettings FromStream(Stream stream,
        IEnumerable<IInstructionGenerator>? customGenerators = null)
    {
        // collect all the hardcoded generators.
        var allGenerators = new Dictionary<string, IInstructionGenerator>(AllGenerators.Generators
            .ToDictionary(x => x.Name, x=> x));
        
        // add custom generators.
        if (customGenerators != null) {
            foreach (var customGenerator in customGenerators) {
                allGenerators[customGenerator.Name] = customGenerator;
            }
        }

        // read settings.
        using var streamReader = new StreamReader(stream);
        var (generators, translations) =
            ConfigurationParser.ParseRouteToInstructions(
                JsonDocument.Parse(streamReader.ReadToEnd()).RootElement, allGenerators);

        return new RouteInstructionGeneratorSettings() {
            Generators = new List<IInstructionGenerator>(generators),
            Languages = translations
        };
    }
}