using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var documents = new { Name = "My Documents", Parent = root, Files = new[] { file, file } };

            ObjectSerializerOptions.Default.PreserveObjectsReferences = true;

            var json = Json.Serialize(documents);

            _output.WriteLine(json.ToString(new PrettyFormatter()));

            var obj = json.Deserialize(documents.GetType());

            var files = ((IEnumerable)obj.GetType().GetProperty("Files").GetValue(obj)).Cast<object>();

            Assert.Equal(files.ElementAt(0).GetType().GetProperty("Name").GetValue(files.ElementAt(1)), file.Name);
            Assert.Equal(files.ElementAt(1).GetType().GetProperty("Name").GetValue(files.ElementAt(1)), file.Name);
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
                deserializer.Deserialize(json, this, false);
                DeserializeCallsCount++;

            }

            public IJson Serialize(ISerializer serializer)
            {
                var json = serializer.Serialize(this, false);
                json.SetKey("id", Guid.NewGuid());
                SerializeCallsCount++;
                return json;
            }
        }

        [Fact]
        public void CustomSerialize()
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

            Assert.Equal(Product2.SerializeCallsCount, Product2.DeserializeCallsCount);
            Assert.Equal(product.Name, newProduct.Name);
            Assert.Equal(product.Expiry, newProduct.Expiry);
            Assert.Equal(product.Sizes, newProduct.Sizes);
            Assert.Equal(newProduct, newProduct.MetaProduct);
        }
    }
}
