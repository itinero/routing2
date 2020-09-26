using System;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero
{
    /// <summary>
    /// A settings objects for snapping options.
    /// </summary>
    public class SnapPointSettings
    {
        /// <summary>
        /// The profiles the snapped edge has to be accessible to.
        /// </summary>
        public Profile[] Profiles { get; set; } = new Profile[0];

        /// <summary>
        /// A flag to enable the option of using any profile as valid instead of all.
        /// </summary>
        public bool AnyProfile { get; set; } = false;

        /// <summary>
        /// A flag to check the can stop on data.
        /// </summary>
        public bool CheckCanStopOn { get; set; } = true;

        /// <summary>
        /// Gets the maximum offset in meter.
        /// </summary>
        public double MaxOffsetInMeter { get; set; } = 1000;

        internal Func<NetworkEdgeEnumerator, bool> AcceptableFunc(Graph network)
        {
            var hasProfiles = this.Profiles.Length > 0;
            if (!hasProfiles) return (_) => true;
            
            var costFunctions = this.Profiles.Select(network.GetCostFunctionFor).ToArray();
            return (eEnum) =>
            {
                var allOk = true;
                
                foreach (var costFunction in costFunctions)
                {
                    var costs = costFunction.Get(eEnum, true, 
                        Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

                    var profileIsOk = costs.canAccess &&
                                      (!this.CheckCanStopOn || costs.canStop);

                    if (this.AnyProfile && profileIsOk) return true;

                    allOk = profileIsOk && allOk;
                }

                return allOk;
            };
        }
    }
}