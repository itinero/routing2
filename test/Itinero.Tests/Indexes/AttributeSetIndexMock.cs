using System;
using System.Collections.Generic;
using Itinero.Indexes;

namespace Itinero.Tests.Indexes;

/// <summary>
/// An index that answers for any id, for tests whose profile ignores the attributes.
/// </summary>
public class AttributeSetIndexMock : IAttributeSetIndex
{
    private readonly Func<uint, IEnumerable<(string key, string value)>> _getById;

    public AttributeSetIndexMock(Func<uint, IEnumerable<(string key, string value)>>? getById = null)
    {
        _getById = getById ?? (_ => Array.Empty<(string key, string value)>());
    }

    public uint Count => uint.MaxValue;

    public IEnumerable<(string key, string value)> GetById(uint id)
    {
        return _getById(id);
    }
}
