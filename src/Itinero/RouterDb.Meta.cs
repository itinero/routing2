using System.Collections.Generic;

namespace Itinero;

public sealed partial class RouterDb
{
    /// <summary>
    /// Gets or sets the meta-data attributes list.
    /// </summary>
    public List<(string key, string value)> Meta { get; set; } = new();
}
