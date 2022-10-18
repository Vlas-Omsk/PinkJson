using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using PinkJson2.Formatters;
using PinkJson2.KeyTransformers;
using PinkJson2.Serializers;
using System;

namespace PinkJson2.Benchmarks
{
    [MemoryDiagnoser]
    public class SerializeBenchmark
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

        private object _o;

        [GlobalSetup]
        public void Setup()
        {
            using (var streamReader = new StreamReader("Json/small.json"))
                _o = Json.Parse(streamReader).ToJson().Deserialize<O>(new ObjectSerializerOptions()
                {
                    KeyTransformer = new CamelCaseKeyTransformer()
                });

            //_o = Enumerable.Range(0, 10_000_000);
        }

        [Benchmark(Baseline = true)]
        public void PinkJson()
        {
            //return _o.Serialize().ToJsonString();
            foreach (var item in _o.Serialize())
                ;
        }

        [Benchmark]
        public string NewtonsoftJson()
        {
            return JsonConvert.SerializeObject(_o);
        }
    }
}
