using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace PinkJson2.xUnitTests
{
    public class ObjectSerializerTest
    {
        private ITestOutputHelper _output;

        public ObjectSerializerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SerializeExceptionTest()
        {
            var json = new ArgumentNullException("test_patam").Serialize();

            Assert.NotNull(json);
        }

        private class Directory
        {
            public string Name { get; set; }
            public Directory Parent { get; set; }
            public File MetaFile { get; set; }
            [JsonProperty(DeserializeToType = typeof(File[]))]
            public IEnumerable<File> Files { get; set; }
        }

        private class File
        {
            public string Name { get; set; }
            public Directory Parent { get; set; }
        }

        [Fact]
        public void SerializeSelfReferencesTest()
        {
            var root = new Directory { Name = "Root" };
            var documents = new Directory { Name = "My Documents", Parent = root };
            var file = new File { Name = "ImportantLegalDocument.docx", Parent = documents };

            root.MetaFile = file;
            documents.Files = new List<File> { file, file };

            ObjectSerializerOptions.Default.PreserveObjectsReferences = true;

            var json = Json.Serialize(documents);

            _output.WriteLine(json.ToString(new PrettyFormatter()));

            var obj = json.Deserialize<Directory>();

            Assert.Equal(obj.Parent.MetaFile.Name, file.Name);
            Assert.Equal(obj.Files.ElementAt(0).Name, file.Name);
            Assert.Equal(obj.Files.ElementAt(1).Name, file.Name);
        }

        [Fact]
        public void SerializeSelfReferencesAnonymouseTest()
        {
            var root = new { Name = "Root" };
            var file = new { Name = "ImportantLegalDocument.docx", Root = root };
            var documents = new { Name = "My Documents", Parent = root, Files = new[] { file, file, file } };

            documents.Files[0] = new { Name = "ImportantLegalDocument2.docx", Root = root };

            ObjectSerializerOptions.Default.PreserveObjectsReferences = true;

            var json = Json.Serialize(documents);

            _output.WriteLine(json.ToString(new PrettyFormatter()));

            var obj = json.Deserialize(documents.GetType());
            var files = ((IEnumerable)obj.GetType().GetProperty("Files").GetValue(obj)).Cast<object>();

            Assert.Equal(documents.Files[0].Name, (string)files.ElementAt(0).GetType().GetProperty("Name").GetValue(files.ElementAt(0)));
            Assert.Equal(documents.Files[1].Name, (string)files.ElementAt(1).GetType().GetProperty("Name").GetValue(files.ElementAt(1)));
            Assert.Equal(documents.Files[2].Name, (string)files.ElementAt(2).GetType().GetProperty("Name").GetValue(files.ElementAt(2)));
        }

        private class Product
        {
            public string Name { get; set; }
            public DateTime Expiry { get; set; }
            public string[] Sizes { get; set; }
        }

        [Fact]
        public void SerializeJsonTest()
        {
            var product = new Product();
            product.Name = "Apple";
            product.Expiry = new DateTime(2008, 12, 28);
            product.Sizes = new string[] { "Small" };
            var json = product.Serialize();

            Assert.IsType<JsonObject>(json);
            Assert.IsType<JsonObject>(json.Value);
            Assert.IsType<JsonArray>(json["Sizes"].Value);
        }

        private class Product2 : IJsonSerializable, IJsonDeserializable
        {
            public string Name { get; set; }
            public DateTime Expiry { get; set; }
            public string[] Sizes { get; set; }
            public Product2 MetaProduct { get; set; }

            public static int SerializeCallsCount { get; set; } = 0;
            public static int DeserializeCallsCount { get; set; } = 0;

            public void Deserialize(IDeserializer deserializer, IJson json)
            {
                if (!json.ContainsKey("id"))
                    throw new Exception("id is gone");
                deserializer.Deserialize(json, this);
                DeserializeCallsCount++;

            }

            public IJson Serialize(ISerializer serializer)
            {
                var json = serializer.Serialize(this);
                json.SetKey("id", Guid.NewGuid());
                SerializeCallsCount++;
                return json;
            }
        }

        [Fact]
        public void CustomSerializeTest()
        {
            var product = new Product2();
            product.Name = "Apple";
            product.Expiry = new DateTime(2008, 12, 28);
            product.Sizes = new string[] { "Small" };
            product.MetaProduct = product;

            Product2.SerializeCallsCount = 0;
            Product2.DeserializeCallsCount = 0;

            var json = product.Serialize(new ObjectSerializerOptions() { PreserveObjectsReferences = true });
            var newProduct = json.Deserialize<Product2>();

            Assert.Equal(1, Product2.SerializeCallsCount);
            Assert.Equal(1, Product2.DeserializeCallsCount);
            Assert.Equal(product.Name, newProduct.Name);
            Assert.Equal(product.Expiry, newProduct.Expiry);
            Assert.Equal(product.Sizes, newProduct.Sizes);
            Assert.Equal(newProduct, newProduct.MetaProduct);
        }

        [Fact]
        public void ValueTypeSerializeTest()
        {
            var dict = new List<DictionaryEntry>();
            dict.Add(new DictionaryEntry("test1", "test1_value"));
            dict.Add(new DictionaryEntry("test2", "test2_value"));
            dict.Add(new DictionaryEntry("test3", "test3_value"));

            var json = dict.Serialize(new ObjectSerializerOptions() { PreserveObjectsReferences = true });
            var dict2 = json.Deserialize<DictionaryEntry[]>();

            _output.WriteLine(json.ToString(new PrettyFormatter()));
            _output.WriteLine(dict[0].Key.ToString());
            _output.WriteLine(dict2[0].Key.ToString());

            Assert.Equal(dict[0].Key, dict2[0].Key);
            Assert.Equal(dict[1].Key, dict2[1].Key);
            Assert.Equal(dict[2].Key, dict2[2].Key);

            Assert.Equal(dict[0].Value, dict2[0].Value);
            Assert.Equal(dict[1].Value, dict2[1].Value);
            Assert.Equal(dict[2].Value, dict2[2].Value);
        }

        private class Config : IDeserializationCallback
        {
            [JsonProperty(IsValueType = true, DeserializerIgnore = true)]
            public IPAddress IPAddress { get; set; }
            public int Port { get; set; }

            public void OnDeserialization(object sender)
            {
                IPAddress = IPAddress.Parse("127.0.0.1");
            }
        }

        [Fact]
        public void DeserializationCallbackTest()
        {
            var config = new Config();
            config.IPAddress = IPAddress.Parse("127.0.0.1");
            config.Port = 1234;

            var json = config.Serialize(new ObjectSerializerOptions()).ToString();
            var newConfig = Json.Parse(json).ToJson().Deserialize<Config>();

            Assert.Equal(config.IPAddress, newConfig.IPAddress);
            Assert.Equal(config.Port, newConfig.Port);
        }

        [Fact]
        public void SerializationConstructorWithSelfReferencesTest()
        {
            var ex2 = new Exception("2");
            var ex1 = new Exception("1", ex2);
            ex2.GetType()
                .GetField("_innerException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ex2, ex1);

            var jsonj = ex1.Serialize(new ObjectSerializerOptions() { PreserveObjectsReferences = true });
            var json = jsonj.ToString();
            var ex1_clone = Json.Parse(json).ToJson().Deserialize<Exception>();

            Assert.Equal(ex1.Message, ex1_clone.Message);
            Assert.True(ex1.InnerException.InnerException == ex1);
            Assert.True(ex1.InnerException.InnerException.InnerException.InnerException == ex1);
        }

#nullable enable

        private class NullableTest1
        {
            public int? Nullable1 { get; set; }
            public string? Nullable2 { get; set; }
            public Guid? Nullable3 { get; set; }
            public int? Nullable4 { get; set; }
            public Guid? Nullable5 { get; set; }
            public NullableTest2? Nullable6 { get; set; }
            public NullableTest2? Nullable7 { get; set; }
        }

        private class NullableTest2
        {
            public double? Nullable1 { get; set; }
        }

        [Fact]
        public void NullableTypeSerializeTest()
        {
            var obj2 = new NullableTest2()
            {
                Nullable1 = 12.43
            };
            var obj1 = new NullableTest1()
            {
                Nullable1 = 231,
                Nullable2 = "test_str",
                Nullable3 = Guid.NewGuid(),
                Nullable4 = null,
                Nullable5 = null,
                Nullable6 = obj2,
                Nullable7 = null
            };

            var json = Json.Parse(obj1.Serialize().ToString()).ToJson();
            var obj1_copy = json.Deserialize<NullableTest1>();

            Assert.Equal(obj1.Nullable1, obj1_copy.Nullable1);
            Assert.Equal(obj1.Nullable2, obj1_copy.Nullable2);
            Assert.Equal(obj1.Nullable3, obj1_copy.Nullable3);
            Assert.Equal(obj1.Nullable4, obj1_copy.Nullable4);
            Assert.Equal(obj1.Nullable5, obj1_copy.Nullable5);
            Assert.Equal(obj1.Nullable6?.Nullable1, obj1_copy.Nullable6?.Nullable1);
            Assert.Equal(obj1.Nullable7?.Nullable1, obj1_copy.Nullable7?.Nullable1);
        }

        [Fact]
        public void JsonKeyValueDeserializeTest()
        {
            var json = new JsonKeyValue("key_test", "29e4796c-8afb-45a1-abff-35782faed48d");
            var guid = json.Deserialize<Guid?>();
            var json2 = new JsonKeyValue("key_test", null);
            var guid2 = json2.Deserialize<Guid?>();

            Assert.NotNull(guid);
#pragma warning disable CS8629 // Nullable value type may be null.
            Assert.Equal(guid.Value, Guid.Parse("29e4796c-8afb-45a1-abff-35782faed48d"));
#pragma warning restore CS8629 // Nullable value type may be null.
            Assert.Null(guid2);
        }

#nullable restore

        [Fact]
        public void JsonArrayWithNullValueTest()
        {
            var json = new object[]
            {
                1,
                "2",
                null,
                3.3,
                null
            }.Serialize();

            Assert.Equal(1, json[0].Get<int>());
            Assert.Equal("2", json[1].Get<string>());
            Assert.Null(json[2].Value);
            Assert.Equal(3.3, json[3].Get<double>());
            Assert.Null(json[4].Value);
        }

        [Fact]
        public void SerializeJsonObjectToJsonTest()
        {
            var json = new JsonObject()
            {
                new JsonKeyValue("testKey1", "testValue1"),
                new JsonKeyValue("testKey2", "testValue2"),
                new JsonKeyValue("testKey3", "testValue3"),
            };

            var serializedJson = json.Serialize();

            Assert.Equal(json, serializedJson);
        }

        [Fact]
        public void SerializeJsonArrayToJsonTest()
        {
            var json = new JsonArray()
            {
                new JsonArrayValue("testValue1"),
                new JsonArrayValue("testValue2"),
                new JsonArrayValue("testValue3"),
            };

            var serializedJson = json.Serialize();

            Assert.Equal(json, serializedJson);
        }

        [Fact]
        public void SerializeStringsDictionaryTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "testKey1", "testValue1" },
                { "testKey2", "testValue2" },
                { "testKey3", "testValue3" },
            };

            var json = dict.Serialize();

            Assert.IsType<JsonObject>(json);

            var dict2 = json.Deserialize<Dictionary<string, string>>();

            Assert.Equal(dict, dict2);
        }

        [Fact]
        public void SerializeDictionaryTest()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "testValue1" },
                { 2, "testValue2" },
                { 3, "testValue3" },
            };

            var json = dict.Serialize();

            Assert.IsType<JsonArray>(json);

            var dict2 = json.Deserialize<Dictionary<int, string>>();

            Assert.Equal(dict, dict2);
        }
    }
}
