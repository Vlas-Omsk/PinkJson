using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson2.Tests
{
    class Program
    {
		class TestArr : List<Test>
        {
        }

		class Test
        {
			public Test TestObj;
			[JsonProperty("abc")]
			public int t = 123;
			public int[] arr = new int[] { 12, 34 };
			public TestArr arr2;
			public string TestString { get; private set; } = "helllo";

			public Test()
            {

            }

			private Test(IJson json)
            {

            }
        }

        static void Main(string[] args)
        {
			//var json = new ObjectSerializer().Serialize(new Test() { TestObj = new Test() });
			//Console.WriteLine(json.ToString(new PrettyFormatter()));

			var anonymousObject = new[] { new
			{
				obj = new { test = new Test() { TestObj = new Test() { arr2 = new TestArr() { new Test() { t = 0 }, new Test() { t = 1 } } } } },
				arr = new object[] { "str", 123 }
			} };
			var objectSerializer = new ObjectSerializer();

			var json = anonymousObject.Serialize();
			json[0]["obj"]["test"]["abc"].Value = 456;
			Console.WriteLine(json.ToString(new PrettyFormatter()));

			var test = json[0]["obj"]["test"].Deserialize<Test>();

			//JsonParserPerformanceTest();
			//OldJsonParserPerformanceTest();
			Console.ReadLine();
		}

		static void JsonParserPerformanceTest()
        {
			var dt = DateTime.Now;
			Json.Parse(File.OpenText("detectable.json"));
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
