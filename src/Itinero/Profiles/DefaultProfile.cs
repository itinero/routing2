using System;
using System.Collections.Generic;

namespace Itinero.Profiles;

/// <summary>
/// A default profile where all factors are '1'.
/// </summary>
public sealed class DefaultProfile : Profile
{
    private readonly Func<IEnumerable<(string key, string value)>, EdgeFactor>? _getEdgeFactor;
    private readonly Func<IEnumerable<(string key, string value)>, TurnCostFactor>? _getTurnCostFactor;

    /// <summary>
    /// Creates a new default profile.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="getEdgeFactor">A customizable function to get edge factors.</param>
    /// <param name="getTurnCostFactor">A customizable function to get turn cost factors.</param>
    public DefaultProfile(string name = "Default",
        Func<IEnumerable<(string key, string value)>, EdgeFactor>? getEdgeFactor = null,
        Func<IEnumerable<(string key, string value)>, TurnCostFactor>? getTurnCostFactor = null)
    {
        this.Name = name;
        _getEdgeFactor = getEdgeFactor;
        _getTurnCostFactor = getTurnCostFactor;
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
    {
        return _getEdgeFactor?.Invoke(attributes) ?? new EdgeFactor(1, 1, 1, 1);
    }

    /// <inheritdoc/>
    public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
    {
        return _getTurnCostFactor?.Invoke(attributes) ?? Profiles.TurnCostFactor.Empty;
    }
}
