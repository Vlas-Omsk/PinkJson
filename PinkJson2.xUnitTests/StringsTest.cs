using System;
using Xunit;

namespace PinkJson2.xUnitTests
{
    public class StringsTest
    {
        [Fact]
        public void EscapeStringTest()
        {
            var str = "123\b\b4\a5\f6\n7\r8\t9\00\"1\\234".EscapeString();
            var str2 = "123234".EscapeString();

            Assert.Equal("123\\b\\b4\\a5\\f6\\n7\\r8\\t9\\00\\\"1\\\\234", str);
            Assert.Equal("123234", str2);
        }
    }
}
