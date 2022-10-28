using PinkJson2.Formatters;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PinkJson2.Examples
{
    public class FormatterTest
    {
        [Fact]
        public void MinifiedFormatterJsonToStringTest()
        {
            var json = new JsonArray()
            {
                new JsonArrayValue("testValue1"),
                new JsonArrayValue("testValue2"),
                new JsonArrayValue(new JsonObject()
                {
                    new JsonKeyValue("testKey1", "testValue1"),
                    new JsonKeyValue("testKey2", "testValue2"),
                    new JsonKeyValue("testKey3", "testValue3"),
                }),
            };

            var str = json.ToString(new MinifiedFormatter());

            Assert.Equal(@"[""testValue1"",""testValue2"",{""testKey1"":""testValue1"",""testKey2"":""testValue2"",""testKey3"":""testValue3""}]", str);
        }

        [Fact]
        public void PrettyFormatterJsonToStringTest()
        {
            var json = new JsonArray()
            {
                new JsonArrayValue("testValue1"),
                new JsonArrayValue("testValue2"),
                new JsonArrayValue(new JsonObject()
                {
                    new JsonKeyValue("testKey1", "testValue1"),
                    new JsonKeyValue("testKey2", "testValue2"),
                    new JsonKeyValue("testKey3", "testValue3"),
                }),
            };

            var str = json.ToString(new PrettyFormatter());

            Assert.Equal("[\r\n  \"testValue1\", \"testValue2\", {\r\n    \"testKey1\": \"testValue1\",\r\n    \"testKey2\": \"testValue2\",\r\n    \"testKey3\": \"testValue3\"\r\n  }\r\n]", str);
        }

        [Fact]
        public void FloatingPointNumbersToJsonStringTest()
        {
            var obj = new
            {
                positiveFloat = 2.98f,
                negativeFloat = -2.98f,
                positiveDouble = 2.98d,
                negativeDouble = -2.98d,
                positiveDecimal = 2.98m,
                negativeDecimal = -2.98m
            };

            var str = obj.Serialize().ToJsonString();

            Assert.Equal("{\"positiveFloat\":2.98,\"negativeFloat\":-2.98,\"positiveDouble\":2.98,\"negativeDouble\":-2.98,\"positiveDecimal\":2.98,\"negativeDecimal\":-2.98}", str);
        }

        [Fact]
        public void AsynchronousUsingOneFormatterTest()
        {
            var obj = Enumerable.Range(0, 1_000_000);

            var formatter = new PrettyFormatter();

            Parallel.Invoke(
                () => obj.Serialize().ToJsonString(formatter),
                () => obj.Serialize().ToJsonString(formatter),
                () => obj.Serialize().ToJsonString(formatter),
                () => obj.Serialize().ToJsonString(formatter)
            );
        }
    }
}
