using System;
using System.Runtime.CompilerServices;
using Itinero.Data.Events;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero
{
    /// <summary>
    /// Represents a router db.
    /// </summary>
    public sealed class RouterDb : IRouterDbWritable
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

        private RouterDbWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public RouterDbWriter GetWriter()
        {
            if (_writer != null) throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                                     $"Check {nameof(HasWriter)} to check for a current writer.");
            _writer = new RouterDbWriter(this);
            return _writer;
        }
        
        void IRouterDbWritable.SetLatest(Network newNetwork)
        {
            this.Network = newNetwork;
        }

        void IRouterDbWritable.ClearWriter()
        {
            _writer = null;
        }
    }
}