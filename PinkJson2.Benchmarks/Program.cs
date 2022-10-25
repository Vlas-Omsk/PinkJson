﻿using BenchmarkDotNet.Running;
using PinkJson2.Formatters;
using PinkJson2.KeyTransformers;
using PinkJson2.Serializers;

namespace PinkJson2.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ParseToJsonBenchmark>();

            //var benchmark = new SerializeBenchmark();
            //benchmark.Setup();
            //benchmark.FilePath = "Json/small.json";
            //benchmark.PinkJson();
        }
    }
}