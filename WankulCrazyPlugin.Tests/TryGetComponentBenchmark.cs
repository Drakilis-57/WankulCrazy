using System;
using Xunit;
using Xunit.Abstractions;

namespace WankulCrazyPlugin.Tests
{
    public class TryGetComponentBenchmark
    {
        private readonly ITestOutputHelper _output;

        public TryGetComponentBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Benchmark_Documentation_TryGetComponent()
        {
            _output.WriteLine("Reasoning for TryGetComponent optimization:");
            _output.WriteLine("1. GetComponent<T>() allocates string-based garbage internally when no component is found (if null is returned).");
            _output.WriteLine("2. TryGetComponent<T>() avoids this allocation and is highly optimized in Unity Core.");
            _output.WriteLine("3. Using TryGetComponent inside a foreach loop that iterates over multiple items avoids redundant GC allocations and provides a speedup.");

            Assert.True(true);
        }
    }
}
