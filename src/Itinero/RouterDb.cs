using System;
using System.Runtime.CompilerServices;
using Itinero.Data.Events;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero
{
    /// <summary>
    /// Represents a router db.
    /// </summary>
    public sealed class RouterDb : IRouterDbMutations
    {
        /// <summary>
        /// Creates a new router db.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public RouterDb(RouterDbConfiguration? configuration = null)
        {
            configuration ??= RouterDbConfiguration.Default;
            
            Network = new Network(this, configuration.Zoom);
        }

        /// <summary>
        /// Gets the latest.
        /// </summary>
        public Network Network { get; private set; }

        /// <summary>
        /// Gets the usage notifier.
        /// </summary>
        public DataUseNotifier UsageNotifier { get; } = new DataUseNotifier();
        
        internal RouterDbProfileConfiguration ProfileConfiguration { get; private set; } = new RouterDbProfileConfiguration();

        private MutableRouterDb? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public IMutableRouterDb GetAsMutable()
        {
            if (_writer != null) throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                                     $"Check {nameof(HasWriter)} to check for a current writer.");
            _writer = new MutableRouterDb(this);
            return _writer;
        }
        
        void IRouterDbMutations.Finish(Network newNetwork, RouterDbProfileConfiguration profileConfiguration)
        {
            this.Network = newNetwork;
            this.ProfileConfiguration = profileConfiguration;
        }
    }
}