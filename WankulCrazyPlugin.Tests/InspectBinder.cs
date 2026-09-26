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




            Assert.NotNull(assembly);
        }
    }
}
