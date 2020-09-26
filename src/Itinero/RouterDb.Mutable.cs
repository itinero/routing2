using System;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero
{
    public sealed partial class RouterDb 
    {
        private MutableRouterDb? _mutable;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasMutable => _mutable != null;
        
        /// <summary>
        /// Gets a mutable version.
        /// </summary>
        /// <returns>The mutable version.</returns>
        public IMutableRouterDb GetAsMutable()
        {
            if (_mutable != null) throw new InvalidOperationException($"Only one mutable version is allowed at one time." +
                                                                     $"Check {nameof(HasMutable)} to check for a current mutable.");
            _mutable = new MutableRouterDb(this);
            return _mutable;
        }
        
        void IRouterDbMutations.Finish(Graph newNetwork, RouterDbProfileConfiguration profileConfiguration)
        {
            this.Graph = newNetwork;
            this.ProfileConfiguration = profileConfiguration;
        }
    }
}