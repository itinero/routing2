using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Types.Generators;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class BaseInstructionTest
    {
        [Fact]
        public void GenerateBaseInstruction_SmallRoute_TurnsLeft()
        {
            var route = RouteScaffolding.GenerateRoute(
                (RouteScaffolding.P(
                    (3.2200777530670166, 51.21591482715479, null)
                ), new List<(string, string)> {
                    ("name", "Elf-Julistraat")
                }),
                (RouteScaffolding.P(
                    (3.220316469669342,
                        51.21548471911082, null),
                    (3.2207858562469482,
                        51.21558888627144, null)
                ), new List<(string, string)> {
                    ("name", "Klaverstraat")
                })
            );


            var baseInstruction = new BaseInstructionGenerator().Generate(
                new IndexedRoute(route), 0);
            Assert.NotNull(baseInstruction);
            Assert.Equal(-90, baseInstruction.TurnDegrees);
        }

        [Fact]
        public void GenerateBaseInstruction_SmallRoute_TurnsRight()
        {
            var route = RouteScaffolding.GenerateRoute(
                (RouteScaffolding.P(
                    (3.2207858562469482, 51.21558888627144, null)
                ), new List<(string, string)> {
                    ("name", "Klaverstraat")
                }),
                (RouteScaffolding.P(
                    (3.220316469669342, 51.21548471911082, null),
                    (3.2200777530670166, 51.21591482715479, null)
                ), new List<(string, string)> {
                    ("name", "Elf-Julistraat")
                })
            );


            var baseInstruction = new BaseInstructionGenerator().Generate(
                new IndexedRoute(route), 0);
            Assert.NotNull(baseInstruction);
            Assert.Equal(90, baseInstruction.TurnDegrees);
        }
    }
}