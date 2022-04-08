using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PinkJson2.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
			//  ____  _       _       _                 
			// |  _ \(_)_ __ | | __  | |___  ___  _ __  
			// | |_) | | '_ \| |/ /  | / __|/ _ \| '_ \ 
			// |  __/| | | | |   < |_| \__ \ (_) | | | |
			// |_|   |_|_| |_|_|\_\___/|___/\___/|_| |_|

			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Magenta;
			Console.WriteLine(@" ____  _       _    ");
			Console.WriteLine(@"|  _ \(_)_ __ | | __");
			Console.WriteLine(@"| |_) | | '_ \| |/ /");
			Console.WriteLine(@"|  __/| | | | |   < ");
			Console.WriteLine(@"|_|   |_|_| |_|_|\_\");
			Console.WriteLine(@"                    ");
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.BackgroundColor = ConsoleColor.White;
			Console.SetCursorPosition(20, 0);
			Console.WriteLine(@"   _                 ");
			Console.SetCursorPosition(20, 1);
			Console.WriteLine(@"  | |___  ___  _ __  ");
			Console.SetCursorPosition(20, 2);
			Console.WriteLine(@"  | / __|/ _ \| '_ \ ");
			Console.SetCursorPosition(20, 3);
			Console.WriteLine(@"|_| \__ \ (_) | | | |");
			Console.SetCursorPosition(20, 4);
			Console.WriteLine(@"___/|___/\___/|_| |_|");
			Console.SetCursorPosition(20, 5);
			Console.WriteLine(@"                     ");
			Console.ResetColor();

			Console.WriteLine();

			// new: 9 ms
			JsonParserPerformanceTest("json.txt");
			Console.WriteLine();

			// old: 157 ms
			// new: 83 ms
			JsonParserPerformanceTest("detectable.json");
			Console.WriteLine();

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

			Console.ReadLine();
		}

		static void JsonParserPerformanceTest(string fileName)
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(fileName))
				Console.WriteLine(string.Concat(md5.ComputeHash(stream).Select(x => x.ToString("X2"))));

			var sw = Stopwatch.StartNew();

			using (var stream = File.OpenText(fileName))
				Json.Parse(stream);

			sw.Stop();

			long objectsCount;

			using (var stream = File.OpenText(fileName))
            {
				var json = Json.Parse(stream);
				objectsCount = GetLength(json as IEnumerable<IJson>);
			}

			var fileInfo = new FileInfo(fileName);

			Console.WriteLine("Elapsed " + sw.ElapsedMilliseconds + " ms");
			Console.WriteLine("File length " + fileInfo.Length + " symbols");
			Console.WriteLine("Json objects count " + objectsCount);
		}

		static long GetLength(IEnumerable<IJson> json)
        {
			long length = 0;
            foreach (var item in json)
            {
                if (item.Value is IEnumerable<IJson> list)
					length += GetLength(list);
                length++;
            }
            return length;
        }

		static void LinqToJsonTest()
        {
			var array = new JsonArray();
			array.AddValueLast("Manual text");
			array.AddValueLast(new DateTime(2000, 5, 23));

			var o = new JsonObject();
			o["MyArray"] = array;

			var json = o.ToString(new PrettyFormatter());
			Console.WriteLine(json);
		}

		class Movie
        {
			public string Name { get; set; }
			public DateTime ReleaseDate { get; set; }
			public string[] Genres { get; set; }
		}

		static void DeserializeJsonTest()
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
