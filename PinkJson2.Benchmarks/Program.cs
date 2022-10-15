﻿using BenchmarkDotNet.Running;

namespace PinkJson2.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<ParseToJsonBenchmark>();

            var benchmark = new ParseToJsonBenchmark() { FilePath = "Json\\large.json" };
            benchmark.PinkJson();
        }
    }
}