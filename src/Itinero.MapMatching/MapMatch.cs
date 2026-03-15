using System.Collections;
using System.Collections.Generic;
using Itinero.Profiles;
using Itinero.Routes.Paths;

namespace Itinero.MapMatching;

/// <summary>
/// Represents the result of a matching.
/// </summary>
public class MapMatch : IReadOnlyList<Path>
{
    private readonly IReadOnlyList<Path> _paths;

    internal MapMatch(Track source, Profile profile,
        IReadOnlyList<Path> paths, IReadOnlyList<int> matchedTrackPointIndices)
    {
        this.Source = source;
        this.Profile = profile;
        _paths = paths;
        this.MatchedTrackPointIndices = matchedTrackPointIndices;
    }

    /// <summary>
    /// The track point indices that were matched. Each raw path [i] connects
    /// MatchedTrackPointIndices[i] to MatchedTrackPointIndices[i+1].
    /// </summary>
    public IReadOnlyList<int> MatchedTrackPointIndices { get; }

    /// <summary>
    /// The source track.
    /// </summary>
    internal Track Source { get; }

    /// <summary>
    /// Gets the profile used for matching.
    /// </summary>
    public Profile Profile { get; }

    /// <summary>
    /// Gets the number of routes in this result.
    /// </summary>
    public int Count => _paths.Count;

    /// <summary>
    /// Gets the matched route at the given index.
    /// </summary>
    /// <param name="i">The index.</param>
    public Path this[int i] => _paths[i];

    /// <inheritdoc/>
    public IEnumerator<Path> GetEnumerator()
    {
        for (var i = 0; i < this.Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
