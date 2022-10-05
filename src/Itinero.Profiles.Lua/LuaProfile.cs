using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Logging;
using Neo.IronLua;

namespace Itinero.Profiles.Lua;

/// <summary>
/// Represents a dynamic routing profile that is based on a lua function.
/// </summary>
public class LuaProfile : Profile
{
    private static readonly global::Neo.IronLua.Lua _lua = new();
    private readonly dynamic _env = _lua.CreateEnvironment();

    private readonly bool _hasTurnFactor;

    private LuaProfile(LuaChunk chunk)
    {
        _env.dochunk(chunk);
        this.Name = _env.name;
        _hasTurnFactor = _env.turn_cost_factor != null;
        if (!_hasTurnFactor)
        {
            Logger.Log("LuaProfile Turnfactor", TraceEventType.Verbose,
                "The profile " + this.Name + " doesn't have a turn_cost_factor defined");
        }
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <summary>
    /// Loads a profile from a lua script file.
    /// </summary>
    /// <param name="path">The path to the lua file.</param>
    /// <returns>The profile.</returns>
    public static Profile LoadFromFile(string path)
    {
        var chunk = _lua.CompileChunk(path, new LuaCompileOptions());
        return new LuaProfile(chunk);
    }

    /// <summary>
    /// Loads profile from a raw lua script.
    /// </summary>
    /// <param name="script">The script.</param>
    /// <param name="name">The name of the script.</param>
    /// <returns>The profile.</returns>
    public static Profile Load(string script, string? name = null)
    {
        name ??= string.Empty;

        var chunk = _lua.CompileChunk(script, name, new LuaCompileOptions());
        return new LuaProfile(chunk);
    }

    /// <inheritdoc />
    public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
    {
        var attributesTable = new LuaTable();
        var resultTable = new LuaTable();
        foreach (var (k, v) in attributes)
        {
            attributesTable[k] = v;
        }

        _env.factor(attributesTable, resultTable);

        var forward = resultTable.GetDouble("forward") ?? 0;
        var backward = resultTable.GetDouble("backward") ?? 0;

        var speedForward = resultTable.GetDouble("forward_speed");
        if (speedForward == null)
        {
            // when forward_speed isn't explicitly filled, the assumption is that factors are in 1/(m/s)
            speedForward = 0;
            if (forward > 0)
            { // convert to m/s.
                speedForward = 1.0 / forward;
            }
        }
        else
        { // when forward_speed is filled, it's assumed to be in km/h, it needs to be convert to m/s.
            speedForward /= 3.6;
        }

        var speedBackward = resultTable.GetDouble("backward_speed");
        if (speedBackward == null)
        {
            // when backward_speed isn't explicitly filled, the assumption is that factors are in 1/(m/s)
            speedBackward = 0;
            if (backward > 0)
            { // convert to m/s.
                speedBackward = 1.0 / backward;
            }
        }
        else
        { // when forward_speed is filled, it's assumed to be in km/h, it needs to be convert to m/s.
            speedBackward /= 3.6;
        }

        var canstop = resultTable.GetBoolean("canstop") ?? (backward > 0 || forward > 0);

        return new EdgeFactor(
            (uint)(forward * 100),
            (uint)(backward * 100),
            (ushort)(speedForward * 100),
            (ushort)(speedBackward * 100),
            canstop
        );
    }

    /// <inheritdoc />
    public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
    {
        if (!_hasTurnFactor || !attributes.Any())
        {
            return Profiles.TurnCostFactor.Empty;
        }

        var attributesTable = new LuaTable();
        var resultTable = new LuaTable();
        foreach (var (k, v) in attributes)
        {
            attributesTable[k] = v;
        }

        _env.turn_cost_factor(attributesTable, resultTable);

        var factor = resultTable.GetDouble("factor") ?? 0;

        var turnCostFactor = Profiles.TurnCostFactor.Empty;
        if (factor < 0)
        {
            turnCostFactor = Profiles.TurnCostFactor.Binary;
        }
        else if (factor > 0)
        {
            turnCostFactor = new TurnCostFactor((uint)(factor * 10));
        }
        return turnCostFactor;
    }
}
