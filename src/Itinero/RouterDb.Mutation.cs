using System;
using Itinero.Network;
using Itinero.Network.Mutation;

namespace Itinero;

public sealed partial class RouterDb
{
    private RoutingNetworkMutator? _mutable;

    /// <summary>
    /// Returns true if there is already a writer.
    /// </summary>
    public bool HasMutableNetwork => _mutable != null;

    /// <summary>
    /// Gets a mutable version of the latest network.
    /// </summary>
    /// <returns>The mutable version.</returns>
    public RoutingNetworkMutator GetMutableNetwork()
    {
        if (_mutable != null) throw new InvalidOperationException($"Only one mutable version is allowed at one time." +
                                                $"Check {nameof(this.HasMutableNetwork)} to check for a current mutable.");

        return this.Latest.GetAsMutable();
    }

    internal RoutingNetworkMutator SetAsMutable(RoutingNetworkMutator mutable)
    {
        if (_mutable != null) throw new InvalidOperationException($"Only one mutable version is allowed at one time." +
                                                                  $"Check {nameof(this.HasMutableNetwork)} to check for a current mutable.");

        _mutable = mutable;
        return _mutable;
    }

    void IRouterDbMutable.Finish(RoutingNetwork newNetwork)
    {
        this.Latest = newNetwork;

        _mutable = null;
    }
}
