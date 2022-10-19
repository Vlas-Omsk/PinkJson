using BenchmarkDotNet.Running;
using PinkJson2.Formatters;
using PinkJson2.KeyTransformers;
using PinkJson2.Serializers;

namespace PinkJson2.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<ParseAndStringifyMinifiedBenchmark>();

            //var benchmark = new ParseAndStringifyMinifiedBenchmark();
            //benchmark.Setup();
            //benchmark.FilePath = "Json/small.json";
            //benchmark.PinkJson();
        }
    }
}