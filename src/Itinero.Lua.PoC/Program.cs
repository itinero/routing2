using System;
using Neo.IronLua;

namespace Itinero.Lua.PoC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            using var lua = new Neo.IronLua.Lua();
            dynamic env = lua.CreateEnvironment();
            env.dochunk(
                lua.CompileChunk("/home/pietervdvn/anyways-open/routing-profiles/itinero2/bicycle.fast.lua",
                new LuaCompileOptions()));
            Console.WriteLine(env.factor); // Access a variable in C#

            var t = new LuaTable();
            t.SetMemberValue("highway", "residential");
            t.SetMemberValue("oneway", "yes");

            var resultTable = new LuaTable();
            Console.WriteLine("Factor gave "+ env.factor(t, resultTable));
            foreach (var member in resultTable.Members) {
                Console.WriteLine(member.Key +" -> "+ member.Value);
            }
        }
    }
}