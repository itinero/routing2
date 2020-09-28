using System;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    public static class SnapPointSettingsExtensions
    {
        internal static Func<IEdgeEnumerator<RoutingNetwork>, bool> AcceptableFunc(this SnapPointSettings setting, 
            RoutingNetwork network)
        {
            var hasProfiles = setting.Profiles.Length > 0;
            if (!hasProfiles) return (_) => true;
            
            var costFunctions = setting.Profiles.Select(network.GetCostFunctionFor).ToArray();
            return (eEnum) =>
            {
                var allOk = true;
                
                foreach (var costFunction in costFunctions)
                {
                    var costs = costFunction.Get(eEnum, true, 
                        Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

                    var profileIsOk = costs.canAccess &&
                                      (!setting.CheckCanStopOn || costs.canStop);

                    if (setting.AnyProfile && profileIsOk) return true;

                    allOk = profileIsOk && allOk;
                }

                return allOk;
            };
        }
    }
}