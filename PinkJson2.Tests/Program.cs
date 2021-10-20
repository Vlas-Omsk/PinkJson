using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson2.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
			JsonParserPerformanceTest();
			OldJsonParserPerformanceTest();
			Console.ReadLine();
		}

		static void JsonParserPerformanceTest()
        {
			var dt = DateTime.Now;
			IJson json2;
			using (var lexer = new JsonLexer(File.OpenText("detectable.json")))
                json2 = JsonParser.Parse(lexer);
			Console.WriteLine((DateTime.Now - dt).TotalMilliseconds + " ms");
		}

		static void OldJsonParserPerformanceTest()
        {
			var dt = DateTime.Now;
			PinkJson.JsonArray json;
			json = new PinkJson.JsonArray(File.ReadAllText("detectable.json"));
			Console.WriteLine((DateTime.Now - dt).TotalMilliseconds + " ms");
		}

		static void JsonLexerTest()
        {
			var lexer = new JsonLexer(@"{
                'octal': 0o52,
				'decimal': 42,
				'hex': 0x2A,
				'binary': 0b00101010
            }".Replace('\'', '"'));

			foreach (var current in lexer)
			{
				Console.Write("Type: " + current.Type + " ");
				if (current.Type == TokenType.String)
					Console.Write("Value: " + current.Value);
				else if (current.Type == TokenType.Number)
					Console.Write("Value: " + current.Value);
				Console.WriteLine();
			}
		}
    }
}
