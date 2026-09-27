using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;
using Xunit.Abstractions;

namespace WankulCrazyPlugin.Tests
{
    public class InspectBinder
    {
        private readonly ITestOutputHelper _output;

        public InspectBinder(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Test local d'inspection IL dépendant d'un chemin absolu hors-repo ou de libs locales")]
        public void Run()
        {
            string gameDll = @"E:\jeux\TCG Card Shop Simulator\Card Shop Simulator_Data\Managed\Assembly-CSharp.dll";
            if (!File.Exists(gameDll)) gameDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "Assembly-CSharp.dll");
            if (!File.Exists(gameDll)) gameDll = @"C:\Users\elias\Downloads\WankulCrazy\libs\Assembly-CSharp.dll";
            if (!File.Exists(gameDll)) return;
            var asm = AssemblyDefinition.ReadAssembly(gameDll);
            var restockScreen = asm.MainModule.Types.FirstOrDefault(t => t.Name == "RestockItemScreen");
            var evalMeth = restockScreen?.Methods.FirstOrDefault(m => m.Name == "EvaluateSorting");
            if (evalMeth != null)
            {
                foreach (var inst in evalMeth.Body.Instructions)
                {
                    _output.WriteLine(inst.ToString());
                }
            }
        }
    }
}
