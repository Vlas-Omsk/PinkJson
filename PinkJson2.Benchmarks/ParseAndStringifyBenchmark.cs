using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using PinkJson2.Formatters;
using System;

namespace PinkJson2.Benchmarks
{
	[MemoryDiagnoser]
    public class ParseAndStringifyBenchmark
    {
		[Params("Json\\small.json", "Json\\medium.json", "Json\\large.json")]
		public string FilePath;

        [Benchmark(Baseline = true)]
        public void PinkJsonUltraFast()
        {
            using (var streamReader = new StreamReader(FilePath))
                Json.Parse(streamReader).ToString(new MinifiedFormatter());
        }

        [Benchmark]
        public void PinkJsonFast()
        {
            using (var streamReader = new StreamReader(FilePath))
                Json.Parse(streamReader).ToString(new PrettyFormatter());
        }

        [Benchmark]
        public void PinkJson()
		{
			using (var streamReader = new StreamReader(FilePath))
				Json.Parse(streamReader).ToJson().ToString(new PrettyFormatter());
		}

		[Benchmark]
		public void NewtonsoftJson()
		{
            var serializer = new JsonSerializer();
            using (var streamReader = new StreamReader(FilePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                serializer.Deserialize(jsonTextReader).ToString();
        }
    }
}
