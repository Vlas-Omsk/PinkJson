using PinkJson2.Formatters;
using PinkJson2.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PinkJson2.Examples
{
    //  ____  _       _       _                 
    // |  _ \(_)_ __ | | __  | |___  ___  _ __  
    // | |_) | | '_ \| |/ /  | / __|/ _ \| '_ \ 
    // |  __/| | | | |   < |_| \__ \ (_) | | | |
    // |_|   |_|_| |_|_|\_\___/|___/\___/|_| |_|

    public static class Program
    {
        public static void Main(string[] args)
        {
            ShowLogo();

            Start();
        }

        private static void ShowLogo()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@" ____  _       _     ");
            Console.WriteLine(@"|  _ \(_)_ __ | | __ ");
            Console.WriteLine(@"| |_) | | '_ \| |/ / ");
            Console.WriteLine(@"|  __/| | | | |   <  ");
            Console.WriteLine(@"|_|   |_|_| |_|_|\_\ ");
            Console.WriteLine(@"                     ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.BackgroundColor = ConsoleColor.White;
            Console.SetCursorPosition(21, 0);
            Console.WriteLine(@"    _                 ");
            Console.SetCursorPosition(21, 1);
            Console.WriteLine(@"   | |___  ___  _ __  ");
            Console.SetCursorPosition(21, 2);
            Console.WriteLine(@"   | / __|/ _ \| '_ \ ");
            Console.SetCursorPosition(21, 3);
            Console.WriteLine(@"  _| \__ \ (_) | | | |");
            Console.SetCursorPosition(21, 4);
            Console.WriteLine(@" \___/___/\___/|_| |_|");
            Console.SetCursorPosition(21, 5);
            Console.WriteLine(@"                      ");
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void Start()
        {
            LinqTest();
            Console.WriteLine();

            CreateJsonTest();
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

        private static void LinqTest()
        {
            using var fileReader = File.OpenRead("Json/small.json");

            var stopwatch = Stopwatch.StartNew();

            using var lexer = new JsonLexer(fileReader);
            var parser = new JsonParser(lexer);

            var medications = parser
                .SelectPath(new JsonPath(new IJsonPathSegment[]
                {
                    new JsonPathObjectSegment("medications"),
                }));

            var names = medications
                .WhereObjects(x =>
                {
                    return x
                        .SelectPath(new JsonPath(new IJsonPathSegment[]
                        {
                            new JsonPathArraySegment(0),
                            new JsonPathObjectSegment("refills")
                        }))
                        .Single()
                        .Get<string>() == "Refill 3";
                })
                .SelectObjects(x => x
                    .SelectPath(new JsonPath(new IJsonPathSegment[]
                    {
                        new JsonPathArraySegment(0),
                        new JsonPathObjectSegment("name")
                    }))
                    .Single()
                    .Get<string>()
                );

            stopwatch.Stop();

            Console.WriteLine(medications.ToJsonString(new PrettyFormatter()));
            Console.WriteLine(string.Join(", ", names));
            Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms");
        }

        private static void CreateJsonTest()
        {
            var array = new JsonArray();
            array.AddValueLast("Manual text");
            array.AddValueLast(new DateTime(2000, 5, 23));

            var o = new JsonObject();
            o.SetKey("MyArray", array);

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
			}".Replace('\'', '"')).ToJson();

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
            Console.WriteLine(json.ToJsonString(new PrettyFormatter()));
        }

        private static void DynamicJsonTest()
        {
            dynamic json = Json.Parse(@"{
                'octal': 0o52,
				'decimal': [42, 43, { 'testKey': 'testValue' }, 45],
				'hex': 0x2A,
				'binary': 0b00101010
            }".Replace('\'', '"')).ToJson();

            json.@decimal[2] = new JsonArrayValue(new JsonArray(new JsonArrayValue(new JsonArray(new JsonArrayValue("hello")))));

            var newJson = Json.Parse(((IJson)json).ToString()).ToJson();

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
