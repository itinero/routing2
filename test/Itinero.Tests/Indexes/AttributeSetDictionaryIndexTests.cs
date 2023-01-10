using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Indexes;
using Xunit;

namespace Itinero.Tests.Indexes;

public class AttributeSetDictionaryIndexTests
{
    [Fact]
    public void AttributeSetIndex_Get_OutOfRange_ShouldThrow()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        Assert.Throws<ArgumentOutOfRangeException>(() => attributeSetIndex.GetById(100));
    }

    [Fact]
    public void AttributeSetIndex_Get_Empty_ShouldReturn0()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        var attributeSetId = attributeSetIndex.Get(new (string key, string value)[0]);
        Assert.Equal(0U, attributeSetId);
    }

    [Fact]
    public void AttributeSetIndex_Get_First_ShouldAdd()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        var attributeSetId = attributeSetIndex.Get(new (string key, string value)[] { ("highway", "residential") });
        Assert.Equal(1U, attributeSetId);

        var edgeTypeAttributes = attributeSetIndex.GetById(attributeSetId).ToArray();
        Assert.Single(edgeTypeAttributes);
        Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
    }

    [Fact]
    public void AttributeSetIndex_Get_Second_ShouldAdd()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        attributeSetIndex.Get(new (string key, string value)[] { ("highway", "residential") });
        var attributeSetId = attributeSetIndex.Get(new (string key, string value)[] { ("highway", "primary") });
        Assert.Equal(2U, attributeSetId);

        var edgeTypeAttributes = attributeSetIndex.GetById(attributeSetId).ToArray();
        Assert.Single(edgeTypeAttributes);
        Assert.Equal(("highway", "primary"), edgeTypeAttributes[0]);
    }

    [Fact]
    public void AttributeSetIndex_Get_SecondIdentical_ShouldGet()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        attributeSetIndex.Get(new (string key, string value)[] { ("highway", "residential") });
        var attributeSetId = attributeSetIndex.Get(new (string key, string value)[] { ("highway", "residential") });
        Assert.Equal(1U, attributeSetId);

        var edgeTypeAttributes = attributeSetIndex.GetById(attributeSetId).ToArray();
        Assert.Single(edgeTypeAttributes);
        Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
    }

    [Fact]
    public void AttributeSetIndex_Get_SecondIdentical_OrderShouldNotMatter_ShouldGet()
    {
        var attributeSetIndex = new AttributeSetDictionaryIndex();

        attributeSetIndex.Get(new (string key, string value)[] { ("highway", "residential"), ("maxspeed", "50") });
        var attributeSetId = attributeSetIndex.Get(new (string key, string value)[]
            {("maxspeed", "50"), ("highway", "residential")});
        Assert.Equal(1U, attributeSetId);

        var edgeTypeAttributes = attributeSetIndex.GetById(attributeSetId).ToArray();
        Assert.Equal(2, edgeTypeAttributes.Length);
        Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
        Assert.Equal(("maxspeed", "50"), edgeTypeAttributes[1]);
    }

    [Fact]
    public async Task AttributeSetIndex_Serialize_Empty_ShouldDeserializeEmpty()
    {
        var expected = new AttributeSetDictionaryIndex();

        var stream = new MemoryStream();
        await expected.WriteTo(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var attributeSetIndex = new AttributeSetDictionaryIndex();
        await attributeSetIndex.ReadFrom(stream);
        Assert.Equal(1U, attributeSetIndex.Count);
    }

    [Fact]
    public async Task AttributeSetIndex_Serialize_One_ShouldDeserializeOne()
    {
        var expected = new AttributeSetDictionaryIndex();
        var type1 = expected.Get(new (string key, string value)[] { ("highway", "residential") });

        var stream = new MemoryStream();
        await expected.WriteTo(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var attributeSetIndex = new AttributeSetDictionaryIndex();
        await attributeSetIndex.ReadFrom(stream);
        Assert.Equal(2U, attributeSetIndex.Count);
        var edgeType1 = attributeSetIndex.GetById(type1).ToArray();
        Assert.Equal(("highway", "residential"), edgeType1[0]);
    }

    [Fact]
    public async Task AttributeSetIndex_Serialize_Two_ShouldDeserializeTwo()
    {
        var expected = new AttributeSetDictionaryIndex();
        var type1 = expected.Get(new (string key, string value)[] { ("highway", "residential") });
        var type2 = expected.Get(new (string key, string value)[] { ("highway", "primary"), ("maxspeed", "50") });

        var stream = new MemoryStream();
        await expected.WriteTo(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var attributeSetIndex = new AttributeSetDictionaryIndex();
        await attributeSetIndex.ReadFrom(stream);
        Assert.Equal(3U, attributeSetIndex.Count);
        var edgeType1 = attributeSetIndex.GetById(type1).ToArray();
        Assert.Equal(("highway", "residential"), edgeType1[0]);
        var edgeType2 = attributeSetIndex.GetById(type2).ToArray();
        Assert.Equal(("highway", "primary"), edgeType2[0]);
        Assert.Equal(("maxspeed", "50"), edgeType2[1]);
    }
}
