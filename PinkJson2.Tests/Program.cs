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
        static void Main(string[] args)
        {
			Console.ReadLine();
		}

		static void LinqToJson()
        {
			var array = new JsonArray();
			array.AddLast(new JsonArrayValue("Manual text"));
			array.AddLast(new JsonArrayValue(new DateTime(2000, 5, 23)));

			var o = new JsonObject();
			o["MyArray"] = new JsonKeyValue("MyArray", array);

			var json = o.ToString(new PrettyFormatter());
			Console.WriteLine(json);
		}

		class Movie
        {
			public string Name { get; set; }
			public DateTime ReleaseDate { get; set; }
			public string[] Genres { get; set; }
		}

		static void DeserializeJson()
        {
			var json = Json.Parse(@"{
			  'Name': 'Bad Boys',
			  'ReleaseDate': '1995-4-7T00:00:00',
			  'Genres': [
				'Action',
				'Comedy'
			  ]
			}".Replace('\'', '"'));

			var m = json.Deserialize<Movie>();
			var name = m.Name;
			Console.WriteLine(name);
		}

		class Product
        {
			public string Name { get; set; }
			public DateTime Expiry { get; set; }
			public string[] Sizes { get; set; }
		}

		static void SerializeJsonTest()
        {
			var product = new Product();
			product.Name = "Apple";
			product.Expiry = new DateTime(2008, 12, 28);
			product.Sizes = new string[] { "Small" };
			var json = product.Serialize();
			Console.WriteLine(json.ToString(new PrettyFormatter()));
		}

		static void JsonParserPerformanceTest()
        {
			var dt = DateTime.Now;
			Json.Parse(File.OpenText("detectable.json"));
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
