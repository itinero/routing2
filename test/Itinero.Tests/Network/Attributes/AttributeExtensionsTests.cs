using System.IO;
using System.Text;
using Itinero.IO;
using Itinero.Network.Attributes;
using Xunit;

namespace Itinero.Tests.Network.Attributes;

public class AttributeExtensionsTests
{
    [Fact]
    public void AttributeExtensions_WriteAttributesTo_Empty_ShouldWrite1Byte()
    {
        var stream = new MemoryStream();

        var size = System.Array.Empty<(string key, string value)>().WriteAttributesTo(stream);

        Assert.Equal(1, size);
    }

    [Fact]
    public void AttributeExtensions_WriteAttributesTo_OneAttribute_ShouldWriteUnicodeWithSizes()
    {
        var stream = new MemoryStream();

        var size = new (string key, string value)[] {
                ("source", "OpenStreetMap")
            }.WriteAttributesTo(stream);

        Assert.Equal(1 +
                     1 + Encoding.Unicode.GetByteCount("source") +
                     1 + Encoding.Unicode.GetByteCount("OpenStreetMap"), size);
    }

    [Fact]
    public void AttributeExtensions_ReadAttributesFrom_Empty_ShouldReadEmpty()
    {
        var stream = new MemoryStream();
        stream.WriteVarInt32(0);

        Assert.Empty(stream.ReadAttributesFrom());
    }

    [Fact]
    public void AttributeExtensions_ReadWrite_1Attribute_ShouldBeIdenticalAfter()
    {
        var expected = new (string key, string value)[] {
                ("source", "OpenStreetMap")
            };

        // write
        var stream = new MemoryStream();
        expected.WriteAttributesTo(stream);

        // read.
        stream.Seek(0, SeekOrigin.Begin);
        var attributes = stream.ReadAttributesFrom();

        Assert.Equal(expected, attributes);
    }

    [Fact]
    public void AttributeExtensions_ReadWrite_2Attribute_ShouldBeIdenticalAfter()
    {
        var expected = new (string key, string value)[] {
                ("source", "OpenStreetMap"),
                ("date", "2025-01-20")
            };

        // write
        var stream = new MemoryStream();
        expected.WriteAttributesTo(stream);

        // read.
        stream.Seek(0, SeekOrigin.Begin);
        var attributes = stream.ReadAttributesFrom();

        Assert.Equal(expected, attributes);
    }
}
