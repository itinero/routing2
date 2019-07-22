using System;
using System.IO;
using Itinero.Data.Graphs.Coders;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Coders
{
    public class EdgeDataLayoutTests
    {
        [Fact]
        public void EdgeDataLayout_SizeShouldReturnNumberOfBytes()
        {
            var layout = new EdgeDataLayout();
            Assert.Equal(0, layout.Size);
            layout.Add("key1", EdgeDataType.Int16);
            Assert.Equal(2, layout.Size);
            layout.Add("key2", EdgeDataType.Byte);
            Assert.Equal(3, layout.Size);
        }

        [Fact]
        public void EdgeDataLayout_AddShouldAddAtEnd()
        {
            var layouts = new EdgeDataLayout();
            layouts.Add("key1", EdgeDataType.Byte);
            layouts.TryGet("key1", out var layout);
            Assert.Equal(0, layout.offset);
            layouts.Add("key2", EdgeDataType.Int32);
            layouts.TryGet("key2", out layout);
            Assert.Equal(1, layout.offset);
            layouts.Add("key3", EdgeDataType.Int32);
            layouts.TryGet("key3", out layout);
            Assert.Equal(5, layout.offset);
        }

        [Fact]
        public void EdgeDataLayout_ShouldNotBeAbleToAddDuplicateKeys()
        {
            var layout = new EdgeDataLayout();
            layout.Add("key1", EdgeDataType.Byte);
            Assert.Throws<ArgumentException>(() => { layout.Add("key1", EdgeDataType.Int16); });
        }

        [Fact]
        public void EdgeDataLayout_WriteToWriteFromShouldBeCopy()
        {
            var layouts = new EdgeDataLayout();
            layouts.Add("key1", EdgeDataType.Byte);
            layouts.Add("key2", EdgeDataType.Int32);
            layouts.Add("key3", EdgeDataType.Int32);

            using (var memoryStream = new MemoryStream())
            {
                layouts.WriteTo(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var copy = EdgeDataLayout.ReadFrom(memoryStream);
                copy.TryGet("key1", out var layout);
                Assert.Equal(0, layout.offset);
                copy.TryGet("key2", out layout);
                Assert.Equal(1, layout.offset);
                layouts.TryGet("key3", out layout);
                Assert.Equal(5, layout.offset);
            }
        }
    }
}