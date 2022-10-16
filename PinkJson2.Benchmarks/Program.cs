using BenchmarkDotNet.Running;

namespace PinkJson2.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<ParseAndStringifyMinifiedBenchmark>();

            var benchmark = new ParseAndStringifyBenchmark() { FilePath = "Json\\large.json" };
            benchmark.PinkJson();
        }
    }
}