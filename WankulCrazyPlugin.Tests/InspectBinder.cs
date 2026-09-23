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
            var assemblyPath = Path.GetFullPath(@"../../../../libs/Assembly-CSharp.dll");
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            var inv = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "InventoryBase");
            _output.WriteLine("=== InventoryBase ===");
            if (inv != null)
            {
                foreach (var m in inv.Methods)
                {
                    if (m.Name.Contains("GetItemData"))
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"METHOD: {m.ReturnType.Name} {m.Name}({ps})");
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

            var playCardUI = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "PlayCardSetUI");
            _output.WriteLine("=== PlayCardSetUI ===");
            if (playCardUI != null)
            {
                foreach (var m in playCardUI.Methods)
                {
                    if (m.Name == "LateUpdate")
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"METHOD: {m.ReturnType.Name} {m.Name}({ps})");
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

            var restockUI = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "RestockItemPanelUI");
            _output.WriteLine("=== RestockItemPanelUI ===");
            if (restockUI != null)
            {
                foreach (var m in restockUI.Methods)
                {
                    if (m.Name == "Init")
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"METHOD: {m.ReturnType.Name} {m.Name}({ps})");
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

            var levelUpUI = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "LevelUpNotificationUI");
            _output.WriteLine("=== LevelUpNotificationUI ===");
            if (levelUpUI != null)
            {
                foreach (var m in levelUpUI.Methods)
                {
                    if (m.Name.Contains("RefreshUnlockableTextData"))
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _output.WriteLine($"METHOD: {m.ReturnType.Name} {m.Name}({ps})");
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
        }
    }
}
