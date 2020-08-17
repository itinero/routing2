using System;
using System.Linq;
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

        internal Func<NetworkEdgeEnumerator, bool> AcceptableFunc(Network network)
        {
            var hasProfiles = this.Profiles != null && this.Profiles.Length > 0;
            if (!hasProfiles) return (_) => true;
            
            var profileHandlers = this.Profiles.Select(network.GetCostFunctionFor).ToArray();
            return (eEnum) =>
            {
                var allOk = true;
                
                foreach (var profileHandler in profileHandlers)
                {
                    profileHandler.MoveTo(eEnum);

                    var profileIsOk = profileHandler.CanAccess() &&
                                      (!this.CheckCanStopOn || profileHandler.CanStop());

                    if (this.AnyProfile && profileIsOk) return true;

                    allOk = profileIsOk && allOk;
                }

                return allOk;
            };
        }
    }
}