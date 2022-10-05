using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Itinero.Indexes;
using Itinero.Profiles;
using Itinero.Profiles.EdgeTypesMap;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Profiles.EdgeTypesMap;

/// <summary>
/// An edge types map based using profiles.
/// </summary>
internal class ProfilesEdgeTypeMap : AttributeSetMap
{
    private readonly ProfileEdgeTypeSetMinimizer _profileEdgeTypeSetMinimizer;

    public ProfilesEdgeTypeMap(IEnumerable<Profile> profiles)
        : base(GenerateGuidFromHash(profiles))
    {
        var sorted = profiles.ToArray();
        Array.Sort(sorted, (x, y) =>
            string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        _profileEdgeTypeSetMinimizer = new ProfileEdgeTypeSetMinimizer(sorted, "highway", "oneway", "access", "surface");
    }

    public override IEnumerable<(string key, string value)> Map(IEnumerable<(string key, string value)> attributes)
    {
        return _profileEdgeTypeSetMinimizer.MinimizeAttributes(attributes);
    }

    private static Guid GenerateGuidFromHash(IEnumerable<Profile> profiles)
    {
        // build a byte array with all hashes of each profile.
        var memoryStream = new MemoryStream();
        foreach (var t in profiles)
        {
            memoryStream.Write(
                BitConverter.GetBytes(t.GetHashCode()), 0, 4);
        }

        // calculate md5 from byte array.
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(memoryStream.ToArray());
        return new Guid(hash);
    }
}
