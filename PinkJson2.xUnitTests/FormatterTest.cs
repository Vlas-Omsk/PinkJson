using PinkJson2.Formatters;
using System;
using Xunit;

namespace PinkJson2.xUnitTests
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
    }
}
