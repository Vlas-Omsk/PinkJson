using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;

namespace PinkJson2.Benchmarks
{
	[MemoryDiagnoser]
    public class ParseToJsonBenchmark
    {
		[Params("Json\\test1.json", "Json\\discord_detectable.json", "Json\\large-file.json")]
		public string FilePath;

        [Benchmark(Baseline = true)]
        public void PinkJsonFast()
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
