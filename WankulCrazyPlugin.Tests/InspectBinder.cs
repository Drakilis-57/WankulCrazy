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
            var assemblyPath = Path.GetFullPath(@"..\..\..\..\libs\Assembly-CSharp.dll");
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            var binderUI = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "CollectionBinderUI");
            _output.WriteLine("=== CollectionBinderUI ===");
            if (binderUI != null)
            {
                foreach (var m in binderUI.Methods)
                {
                    if (m.Name == "SetCardCollected")
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"TARGET: {m.ReturnType.Name} {m.Name}({ps})");
                        if (m.HasBody)
                        {
                            foreach (var inst in m.Body.Instructions)
                            {
                                _output.WriteLine($"    {inst.OpCode} {inst.Operand}");
                            }
                        }
                    }
                }
            }
            var binderAnim = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "CollectionBinderFlipAnimCtrl");
            if (binderAnim != null)
            {
                foreach (var m in binderAnim.Methods)
                {
                    if (m.Name.Contains("SwitchExpansion") || m.Name.Contains("SwitchSortingMethod"))
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"{m.ReturnType.Name} {m.Name}({ps})");
                    }
                }
            }

            // Look for nested iterator types
            foreach (var nested in binderAnim.NestedTypes)
            {
                if (nested.Name.Contains("DelayAlbumSort"))
                {
                    _output.WriteLine($"Nested: {nested.Name}");
                    foreach (var m in nested.Methods)
                    {
                        _output.WriteLine($"  {m.ReturnType.Name} {m.Name}");
                    }
                }
            }
        }
    }
}
