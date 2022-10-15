using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using PinkJson2.KeyTransformers;
using PinkJson2.Serializers;
using System;

namespace PinkJson2.Benchmarks
{
    [MemoryDiagnoser]
    public class DeserializeBenchmark
    {
        private class O
        {
            public Dictionary<string, Medication[]> Medications { get; set; }
            public Lab[] Labs { get; set; }
            public Imaging[] Imaging { get; set; }
        }

        private class Medication
        {
            public string Name { get; set; }
            public string Strength { get; set; }
            public string Dose { get; set; }
            public string Route { get; set; }
            public string Sig { get; set; }
            public string PillCount { get; set; }
            public string Refills { get; set; }
        }

        private class Lab
        {
            public string Name { get; set; }
            public string Time { get; set; }
            public string Location { get; set; }
        }

        private class Imaging
        {
            public string Name { get; set; }
            public string Time { get; set; }
            public string Location { get; set; }
        }

        [Benchmark(Baseline = true)]
        public void PinkJson()
        {
            using (var streamReader = new StreamReader("Json/small.json"))
                Json.Parse(streamReader).ToJson().Deserialize<O>(new ObjectSerializerOptions()
                {
                    KeyTransformer = new CamelCaseKeyTransformer()
                });
        }

        [Benchmark]
        public void NewtonsoftJson()
        {
            var serializer = new JsonSerializer();
            using (var streamReader = new StreamReader("Json/small.json"))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                serializer.Deserialize<O>(jsonTextReader);
        }
    }
}
