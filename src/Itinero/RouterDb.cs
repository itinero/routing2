using System;
using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero
{
    /// <summary>
    /// Represents a router db.
    /// </summary>
    public sealed partial class RouterDb : IRouterDbMutable
    {
        /// <summary>
        /// Creates a new router db.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public RouterDb(RouterDbConfiguration? configuration = null)
        {
            configuration ??= RouterDbConfiguration.Default;
            
            Latest = new RoutingNetwork(this, configuration.Zoom);
        }

        private RouterDb(RoutingNetwork routingNetwork)
        {
            this.Latest = routingNetwork;
        }

        /// <summary>
        /// Gets the latest.
        /// </summary>
        public RoutingNetwork Latest { get; private set; }

        /// <summary>
        /// Gets the usage notifier.
        /// </summary>
        public DataUseNotifier UsageNotifier { get; } = new DataUseNotifier();
        
        internal RouterDbProfileConfiguration ProfileConfiguration { get; private set; } = new RouterDbProfileConfiguration();
        
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
                                                                      $"Check {nameof(HasMutableNetwork)} to check for a current mutable.");
            _mutable = this.Latest.GetAsMutable();
            return _mutable;
        }
        
        void IRouterDbMutable.Finish(RoutingNetwork newNetwork, RouterDbProfileConfiguration profileConfiguration)
        {
            this.Latest = newNetwork;
            this.ProfileConfiguration = profileConfiguration;
        }
    }
}