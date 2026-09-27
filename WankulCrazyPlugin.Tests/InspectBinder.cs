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

        [Fact]
        public void Run()
        {
            // Try a few fallback paths including cross-platform relative paths for CI/Linux
            string gameDll = Path.Combine(Directory.GetCurrentDirectory(), "libs", "Assembly-CSharp.dll");

            if (!File.Exists(gameDll))
            {
                // Fallback for when running from bin/Debug/net8.0 etc.
                gameDll = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "libs", "Assembly-CSharp.dll");
            }
            if (!File.Exists(gameDll))
            {
                // Give up and skip test rather than crashing if file is not on runner
                _output.WriteLine("Could not find Assembly-CSharp.dll, skipping InspectBinder test.");
                return;
            }
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
