using System;
using Xunit;

namespace PinkJson2.xUnitTests
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
    }
}
