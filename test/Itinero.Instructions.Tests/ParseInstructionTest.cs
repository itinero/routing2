using Itinero.Instructions.Instructions;
using Itinero.Instructions.ToText;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Itinero.Instructions.Tests
{
    public class ParseInstructionTest
    {
        [Fact]
        public void ParseRenderValue_SimpleValue_SubstitutionInstruction()
        {
            var parsed = FromJson.ParseRenderValue("Turn $turnDegrees around, so you are at $tUrnDeGReEs");
            var result = parsed.ToText(new BaseInstruction(0, 42));
            Assert.Equal("Turn 42 around, so you are at 42", result);
        }

        
        [Fact]
        public void ParseRenderValue_SubstitutionWithNoSpace_SubstitutionInstruction()
        {
            var parsed = FromJson.ParseRenderValue("Take the ${exitNumber}th exit");
            var result = parsed.ToText(new RoundaboutInstruction(0,5, 42, 5));
            Assert.Equal("Take the 5th exit", result);
        }
        [Fact]
        public void ParseCondition_AdvancedCondition_CorrectResult()
        {
            var input =
                "{\"start\": { \"$startDegrees>=-45&$startDegrees<=45\": \"Start north\"," +
                " \"$startDegrees>45&$startDegrees<=135\": \"Start east\"," +
                " \"$startDegrees>135&$startDegrees<=225\": \"Start south\", " +
                "\"$startDegrees>225\": \"Start west\" }}";

            var toText = FromJson.ParseInstructionToText(JObject.Parse(input));
            var north = toText.ToText(new StartInstruction(0, 0, 0));
            Assert.Equal("Start north", north);
            var east = toText.ToText(new StartInstruction(0, 50, 0));
            Assert.Equal("Start east", east);
            var west = toText.ToText(new StartInstruction(0, 275, 0));
            Assert.Equal("Start west", west);
        }

        [Fact]
        public void ParseCondition_TypeCondition_BasicCondition()
        {
            var (p, prior) = FromJson.ParseCondition("base");
            Assert.False(prior);
            Assert.True(p(new BaseInstruction(0, 42)));
            Assert.True(p(new BaseInstruction(0, 43)));
        }

        [Fact]
        public void ParseCondition_EqCondition_BasicCondition()
        {
            var (p, prior) = FromJson.ParseCondition("$turndegrees=42");
            Assert.False(prior);
            Assert.True(p(new BaseInstruction(0, 42)));
            Assert.False(p(new BaseInstruction(0, 43)));
        }

        [Fact]
        public void ParseCondition_CompareCondition_BasicCondition()
        {
            var (p, _) = FromJson.ParseCondition("$turndegrees<=42");
            Assert.True(p(new BaseInstruction(0, 35)));
            Assert.True(p(new BaseInstruction(0, 42)));
            Assert.False(p(new BaseInstruction(0, 43)));
            Assert.False(p(new BaseInstruction(0, 50)));

            (p, _) = FromJson.ParseCondition("$turndegrees<42");
            Assert.True(p(new BaseInstruction(0, 35)));
            Assert.False(p(new BaseInstruction(0, 42)));
            Assert.False(p(new BaseInstruction(0, 43)));
            Assert.False(p(new BaseInstruction(0, 50)));


            (p, _) = FromJson.ParseCondition("$turndegrees>42");
            Assert.False(p(new BaseInstruction(0, 35)));
            Assert.False(p(new BaseInstruction(0, 42)));
            Assert.True(p(new BaseInstruction(0, 43)));
            Assert.True(p(new BaseInstruction(0, 50)));

            (p, _) = FromJson.ParseCondition("$turndegrees>=42");
            Assert.False(p(new BaseInstruction(0, 35)));
            Assert.True(p(new BaseInstruction(0, 42)));
            Assert.True(p(new BaseInstruction(0, 43)));
            Assert.True(p(new BaseInstruction(0, 50)));
        }

        [Fact]
        public void ParseCondition_FallbackCondition_BasicCondition()
        {
            var (p, prior) = FromJson.ParseCondition("*");
            Assert.True(prior);
            Assert.True(p(new BaseInstruction(0, 42)));
            Assert.True(p(new BaseInstruction(0, 43)));
        }
    }
}