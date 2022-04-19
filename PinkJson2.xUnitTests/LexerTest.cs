using System;
using Xunit;

namespace PinkJson2.xUnitTests
{
    public class LexerTest
    {
        [Fact]
        public void NumberFormatsTest()
        {
			var lexer = new JsonLexer(@"{
                'octal': 0o52,
				'decimal': 42,
				'hex': 0x2A,
				'binary': 0b00101010
            }".Replace('\'', '"'));

			foreach (var current in lexer)
			{
				if (current.Type == TokenType.Number)
					Assert.Equal(42, (int)current.Value);
			}
		}
    }
}
