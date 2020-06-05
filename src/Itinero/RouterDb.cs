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
    public sealed partial class RouterDb : IRouterDbMutations
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
    }
}