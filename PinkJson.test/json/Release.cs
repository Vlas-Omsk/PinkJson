#if DEBUG == false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PinkJson;
using System.Threading;
using System.Diagnostics;

namespace json
{
    static class Release
    {
        static void SerializeArray()
        {
            int[,] numbers = new int[,]
            {
                { 9, 5, -9 },
                { -11, 4, 0 },
                { 6, 115, 3 },
                { -12, -9, 71 },
                { 1, -6, -1 }
            };

            JsonArray JArray = JsonArray.FromArray(numbers, false);

            Console.WriteLine(JArray.ToString());
        }

        static void DeserializeArray()
        {
            string json = "[9, \"Five\", -9, -11, 4, 0, 6, 115, 3, -12, -9, 71, 1, -6, -1]";

            object[] numbers = JsonArray.ToArray<object>(new JsonArray(json));

            Console.WriteLine(numbers[7] + ", " + numbers[1]);
        }

        struct Product
        {
            public Product(string name)
            {
                Name = name;
                Expiry = default;
                Sizes = null;
            }

            private string Name;
            public DateTime Expiry;
            public string[] Sizes;
        }

        static void SerializeObject()
        {
            Product product = new Product("Apple");
            product.Expiry = new DateTime(2008, 12, 28);
            product.Sizes = new string[] { "Small" };

            Json JObject = Json.FromObject(product, true);

            Console.WriteLine(JObject.ToFormatString());
        }

        class Movie
        {
            public string Name;
            public string ReleaseDate;
            public string[] Genres { get; set; }
        }

        static void DeserealizeObject()
        {
            string json = @"{
                'Name': 'Bad Boys',
                'ReleaseDate': '1995-4-7T00:00:00',
                'Genres': [
                    'Action',
                    'Comedy'
                ]
            }".Replace('\'', '"');

            Movie m = Json.ToObject<Movie>(new Json(json));

            Console.WriteLine(m.Name);
        }

        static void SerializeAnonymous()
        {
            var anonymous = new 
            {
                Name = "Bad Boys",
                ReleaseDate = "1995-4-7T00:00:00",
                Genres = new[]
                {
                    "Action",
                    "Comedy"
                }
            };

            Json JObject = Json.FromAnonymous(anonymous);

            Console.WriteLine(JObject.ToFormatString());
        }

        static void CreateJson()
        {
            JsonArray array = new JsonArray();
            array.Add("Manual text");
            array.Add(new DateTime(2000, 5, 23));

            Json o = new Json() { "MyArray" };
            o["MyArray"].Value = array;
            //or
            //Json o = new Json() { ("MyArray", array) };

            Console.WriteLine(o.ToFormatString());
        }

        static void PathsTest()
        {
            var json = new JsonArray(@"[
                {
                    '_id': '5973782bdb9a930533b05cb2',
                    'isActive': true,
                    'balance': '$1,446.35',
                    'age': 32,
                    'eyeColor': 'green',
                    'name': 'Logan Keller',
                    'gender': 'male',
                    'company': 'ARTIQ',
                    'email': 'logankeller@artiq.com',
                    'phone': '+1 (952) 533-2258',
                    'friends': [
                        {
                            'id': 0,
                            'name': 'Colon Salazar'
                        },
                        {
                            'id': 1,
                            'name': 'French Mcneil'
                        },
                        {
                            'id': 2,
                            'name': 'Carol Martin'
                        }
                    ],
                    'favoriteFruit': 'banana'
                }
            ]".Replace('\'', '"'));

            Console.WriteLine("json[0][\"phone\"].Value => " + json[0]["phone"].Value);
            Console.WriteLine("json.GetDynamic()._0.phone.Value => " + json.GetDynamic()._0.phone.Value);
            Console.WriteLine();
            Console.WriteLine("json[0][\"friends\"][2][\"name\"].Value => " + json[0]["friends"][2]["name"].Value);
            Console.WriteLine("json.GetDynamic()._0.friends._2.name.Value => " + json.GetDynamic()._0.friends._2.name.Value);
        }

        static void Main()
        {
            Console.WriteLine($"=== SerializeArray ===");
            Run(SerializeArray);
            Console.WriteLine($"\r\n=== DeserializeArray ===");
            Run(DeserializeArray);
            Console.WriteLine($"\r\n=== SerializeObject ===");
            Run(SerializeObject);
            Console.WriteLine($"\r\n=== DeserealizeObject ===");
            Run(DeserealizeObject);
            Console.WriteLine($"\r\n=== SerializeAnonymous ===");
            Run(SerializeAnonymous);
            Console.WriteLine($"\r\n=== CreateJson ===");
            Run(CreateJson);
            Console.WriteLine($"\r\n=== PathsTest ===");
            Run(PathsTest);

            Console.ReadLine();
        }

        static void Run(Action action)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            action();
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
        }
    }
}

#endif