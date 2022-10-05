using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Itinero.Profiles;

/// <summary>
/// Abstract representation of a profile.
/// </summary>
/// <remarks>
/// This is a single possible behaviour of a vehicle (e.g. 'bicycle.fastest').
/// </remarks>
public abstract class Profile
{
    /// <summary>
    /// Gets the name of this profile.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the edge factor for the given attributes.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>An edge factor.</returns>
    public abstract EdgeFactor Factor(IEnumerable<(string key, string value)> attributes);

    /// <summary>
    /// Gets a factor for the turn costs for the given attributes.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>A turn cost factor.</returns>
    public abstract TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes);

    /// <summary>
    /// Gets a stable hashcode, this hashcode doesn't change between runs.
    /// </summary>
    /// <remarks>
    /// This uses the name property by default, if using profiles with unique names this default implementation is fine.
    /// </remarks>
    /// <returns>The stable hash code.</returns>
    public override int GetHashCode()
    {
        // TODO: a slow hash, this can be better.
        // on the other hand this hash should only be computed once when the profile is added to a router db.
        var hash = Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(this.Name));
        return BitConverter.ToInt32(hash, 0);
    }

    /// <summary>
    /// Compares this profiles to the given profile. Returns true if the profiles are identical functionally.
    /// </summary>
    /// <remarks>
    /// This uses the name property by default, if using profiles with unique names this default implementation is fine.
    /// </remarks>
    /// <returns>True if the profiles have the same name.</returns>
    public override bool Equals(object obj)
    {
        if (obj is Profile profile)
        {
            return profile.Name.Equals(this.Name);
        }

        return false;
    }

    private static readonly MD5 Hasher = MD5.Create();
}
