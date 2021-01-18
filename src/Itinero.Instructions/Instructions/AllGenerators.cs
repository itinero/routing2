using Reminiscence.Collections;

namespace Itinero.Instructions.Instructions
{
    public class AllGenerators
    {
        private static List<IInstructionGenerator> Generators = new List<IInstructionGenerator>
        {
            new BaseInstructionGenerator(),
            new EndInstructionGenerator(),
            new StartInstructionGenerator(),
            new IntersectionInstruction.IntersectionInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowAllowGenerator(),
            new FollowBendGenerator()
        };

        public static Dictionary<string, IInstructionGenerator> AllGeneratorsDict = GetDict();

        private static Dictionary<string, IInstructionGenerator> GetDict()
        {
            var dict = new Dictionary<string, IInstructionGenerator>();

            foreach (var generator in Generators)
            {
                var name = generator.GetType().Name.ToLower();
                if (name.EndsWith("generator"))
                {
                    dict[name] = generator;
                    name = name.Substring(0, name.Length - "generator".Length);
                }
                
                if (name.EndsWith("instruction"))
                {
                    dict[name] = generator;
                    name = name.Substring(0,name.Length - "instruction".Length);
                }

                dict[name] = generator;
            }
            
            return dict;
        }
        
    }
    
}