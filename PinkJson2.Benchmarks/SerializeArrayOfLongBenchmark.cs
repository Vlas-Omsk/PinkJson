using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;

namespace PinkJson2.Benchmarks
{
    [MemoryDiagnoser]
    public class SerializeArrayOfLongBenchmark
    {
        private object _obj;

        [GlobalSetup]
        public void Setup()
        {
            _obj = Enumerable.Range(0, 10_000_000).Select(x => (long)x).ToArray();
        }

        [Benchmark(Baseline = true)]
        public string PinkJson()
        {
            return _obj.Serialize().ToJsonString();
        }

        [Benchmark]
        public string NewtonsoftJson()
        {
            return JsonConvert.SerializeObject(_obj);
        }
    }
}
