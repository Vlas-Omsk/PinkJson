using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;

namespace PinkJson2.Benchmarks
{
	[MemoryDiagnoser]
    public class ParseToJsonBenchmark
    {
		[Params("Json/small.json", "Json/medium.json", "Json/large.json")]
		public string FilePath { get; set; }

        [Benchmark(Baseline = true)]
        public void PinkJson()
        {
            using (var streamReader = new StreamReader(FilePath))
                Json.Parse(streamReader).ToJson();
        }

		[Benchmark]
		public void NewtonsoftJson()
		{
            var serializer = new JsonSerializer();
            using (var streamReader = new StreamReader(FilePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                serializer.Deserialize(jsonTextReader);
        }
    }
}
