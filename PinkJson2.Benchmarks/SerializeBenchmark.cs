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
            public List<Lab> Labs { get; set; }
            public List<Imaging> Imaging { get; set; }
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

        private object _obj;

        [GlobalSetup]
        public void Setup()
        {
            O obj;

            using (var streamReader = new StreamReader("Json/small.json"))
                obj = Json.Parse(streamReader).ToJson().Deserialize<O>(new ObjectSerializerOptions()
                {
                    KeyTransformer = new CamelCaseKeyTransformer()
                });

            for (var i = 0; i < 15; i++)
                obj.Labs.AddRange(obj.Labs);
            for (var i = 0; i < 15; i++)
                obj.Imaging.AddRange(obj.Imaging);
            for (var i = 0; i < 15; i++)
            {
                var j = 0;
                foreach (var item in obj.Medications.ToArray())
                    obj.Medications.Add(item.Key + i * (++j), item.Value);
            }

            _obj = obj;
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
