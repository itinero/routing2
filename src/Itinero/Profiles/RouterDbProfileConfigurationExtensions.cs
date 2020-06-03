using System.Collections.Generic;

namespace Itinero.Profiles
{
    internal static class RouterDbProfileConfigurationExtensions
    {
        public static bool HasAll(this RouterDbProfileConfiguration profileConfiguration, IEnumerable<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                if (!profileConfiguration.HasProfile(profile.Name)) return false;
            }

            return true;
        }
    }
}