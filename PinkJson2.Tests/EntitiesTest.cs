using System;
using Xunit;

namespace PinkJson2.Examples
{
    public class EntitiesTest
    {
        [Fact]
        public void SetByKeyTest()
        {
            var o = new JsonObject();
            o.SetKey("test_key", "test_value");

            Assert.Equal("test_value", o["test_key"].Get<string>());
        }

        [Fact]
        public void ReplaceByKeyTest()
        {
            var o = new JsonObject()
            {
                { "test_key", "test_value" }
            };
            o.SetKey("test_key", "test_value2");

            Assert.Equal("test_value2", o["test_key"].Get<string>());
        }
    }
}
