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
			IJson json2 = Json.Parse(File.OpenText("detectable.json"));
			Console.WriteLine((DateTime.Now - dt).TotalMilliseconds + " ms");
		}

		static void OldJsonParserPerformanceTest()
        {
			var dt = DateTime.Now;
			PinkJson.JsonArray json = new PinkJson.JsonArray(File.ReadAllText("detectable.json"));
			Console.WriteLine((DateTime.Now - dt).TotalMilliseconds + " ms");
		}

		static void DynamicJsonTest()
        {
			dynamic json = Json.Parse(@"{
                'octal': 0o52,
				'decimal': [42, 43, { 'testKey': 'testValue' }, 45],
				'hex': 0x2A,
				'binary': 0b00101010
            }".Replace('\'', '"'));
			var b = json.@decimal._2 = new JsonArrayValue(new JsonArray(new JsonArrayValue(new JsonArray(new JsonArrayValue("hello")))));
			IJson ddd = Json.Parse(json.ToString());
			Console.WriteLine(ddd.ToString(new PrettyFormatter()));
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
