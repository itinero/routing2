using System.IO;

namespace Itinero.Profiles.Serialization
{
    internal static class StreamExtensions
    {
        public static void WriteProfile(this Stream stream, Profile profile, IProfileSerializer profileSerializer)
        {
            profileSerializer.WriteTo(stream, profile);
        }

        public static Profile ReadProfile(this Stream stream, IProfileSerializer profileSerializer)
        {
            return profileSerializer.ReadFrom(stream);
        }
    }
}