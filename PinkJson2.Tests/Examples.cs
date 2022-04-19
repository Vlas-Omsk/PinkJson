using PinkJson2.Formatters;
using System;

namespace PinkJson2.Tests
{
    public static class Examples
    {
		public static void Start()
        {
			LinqToJsonTest();
			Console.WriteLine();

			DeserializeJsonTest();
			Console.WriteLine();

			SerializeJsonTest();
			Console.WriteLine();

			DynamicJsonTest();
			Console.WriteLine();

			JsonLexerTest();
			Console.WriteLine();
		}

		private static void LinqToJsonTest()
		{
			var array = new JsonArray();
			array.AddValueLast("Manual text");
			array.AddValueLast(new DateTime(2000, 5, 23));

			var o = new JsonObject();
			o.SetKey("MyArray", "test");

			var json = o.ToString(new PrettyFormatter());
			Console.WriteLine(json);
		}

		private class Movie
		{
			public string Name { get; set; }
			public DateTime ReleaseDate { get; set; }
			public string[] Genres { get; set; }
		}

		private static void DeserializeJsonTest()
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

		private class Product
		{
			public string Name { get; set; }
			public DateTime Expiry { get; set; }
			public string[] Sizes { get; set; }
		}

		private static void SerializeJsonTest()
		{
			var product = new Product();
			product.Name = "Apple";
			product.Expiry = new DateTime(2008, 12, 28);
			product.Sizes = new string[] { "Small" };
			var json = product.Serialize();
			Console.WriteLine(json.ToString(new PrettyFormatter()));
		}

		private static void DynamicJsonTest()
		{
			dynamic json = Json.Parse(@"{
                'octal': 0o52,
				'decimal': [42, 43, { 'testKey': 'testValue' }, 45],
				'hex': 0x2A,
				'binary': 0b00101010
            }".Replace('\'', '"'));

			json.@decimal[2] = new JsonArrayValue(new JsonArray(new JsonArrayValue(new JsonArray(new JsonArrayValue("hello")))));

			IJson newJson = Json.Parse(json.ToString());

			Console.WriteLine(newJson.ToString(new PrettyFormatter()));
		}

		private static void JsonLexerTest()
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
