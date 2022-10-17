using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PinkJson2.Formatters;
using System;

namespace PinkJson2.Benchmarks
{
	[MemoryDiagnoser]
    public class ParseAndStringifyMinifiedBenchmark
    {
		[Params("Json/small.json", "Json/medium.json", "Json/large.json")]
		public string FilePath { get; set; }

        [Benchmark]
        public void PinkJsonFast()
        {
            using (var streamReader = new StreamReader(FilePath))
                Json.Parse(streamReader).ToJsonString(new MinifiedFormatter());
        }

        [Benchmark(Baseline = true)]
        public void PinkJson()
        {
            using (var streamReader = new StreamReader(FilePath))
                Json.Parse(streamReader).ToJson().ToString(new MinifiedFormatter());
        }

        [Benchmark]
        public void NewtonsoftJson()
        {
            var serializer = new JsonSerializer();
            using (var streamReader = new StreamReader(FilePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                ((JToken)serializer.Deserialize(jsonTextReader)).ToString(Formatting.None);
        }
    }
}
