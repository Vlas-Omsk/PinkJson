using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PinkJson.Lexer;
using PinkJson.Parser;
using System.Threading;
using System.Diagnostics;

namespace json
{
    class Program
    {
        public struct Phone
        {
            public string Name;
            public string[] Tags;
            public Phone[] Phones;
            public dynamic dynamicHuinya;
        }

        public struct Test
        {
            public string LastName;
            private string FirstName;
            public Phone[] Phones;
            internal Int64[] Диапазон;

            public Test(string firstname)
            {
                FirstName = firstname;
                LastName = null;
                Phones = null;
                Диапазон = null;
            }
        }

        public static Test test = new Test("Vasily")
        {
            LastName = "Ivanov",
            Диапазон = new long[] { 19, 24 },
            Phones = new Phone[] {
                new Phone() {
                    Name="HUifon 10X",
                    Tags=new string[] { "Company HUepple\n\t", "Without root access\n\t", "WITHOUT FUCKING JACK 3.5" },
                    Phones = new Phone[] {
                        new Phone() {
                            Name="HUifon 10X",
                            Tags=new string[] { "Company HUepple\n\t", "Without root access\n\t", "WITHOUT FUCKING JACK 3.5" },
                        },
                        new Phone() {
                            Name = "LENOVO TAB3 7",
                            Tags = new string[] { "Very bad assembly\n\t", "Big 7' screen, but low resolution\n\t", "Capacious li-ion battery\n\t" }
                        }
                    }
                },
                new Phone() {
                    Name = "XIAOMI REDMI NOTE 7",
                    Tags = new string[] { "Very performance\r\t\n\b   TEST", "Big resolution", "Fast charge" },
                    dynamicHuinya = true
                }
            }
        };

        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var s = Json.FromStructure(test, true);
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            File.WriteAllText("test.json", s.ToFormatString());

            stopWatch.Start();
            var file = new Json(File.ReadAllText("test.json"));
            var testout = Json.ToStructure<Test>(new Json(file));
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");

            var ss = Json.FromStructure(testout, true);
            Console.WriteLine(ss.ToFormatString() + "\r\n\r\n");

            Main2(args);

            Console.ReadLine();
        }

        static void Main2(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();
            var j = Json.FromAnonymous(new
            {
                abc = new
                {
                    hello = "123",
                    h1 = new JsonObjectArray()
                    {
                        false,
                        true,
                        "str",
                        12785681,
                        Json.FromAnonymous(new
                        {
                            id = 32,
                            name = "egor",
                            arr = new
                            {
                                clas = "base",
                                lol = new[]
                                {
                                    new
                                    {
                                        prikol = "rjaka",
                                        ind = 1
                                    },
                                    new
                                    {
                                        prikol = "rs",
                                        ind = 2
                                    }
                                },
                                hash = "dijnaogsndghin039j\r\nt3y8972gtrb3789r3fb"
                            }
                        })
                    },
                    h2 = new[]
                    {
                        "Hi",
                        "Hell"
                    }
                }
            });
            ((j[0].Value as Json)["h1"].Value as JsonObjectArray).Add(((j[0].Value as Json)["h1"].Value as JsonObjectArray).Clone());
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            Console.WriteLine(j.ToFormatString() + "\r\n\r\n");

            var sj = "[{" +
                "	'guild_experiments': [" +
                "		[3601750436, null, 1, [" +
                "				[-1, [{" +
                "					's': 1000," +
                "					'e': 10000" +
                "				}]]," +
                "				[1, [{" +
                "					's': 500," +
                "					'e': 1000" +
                "				}]]" +
                "			]," +
                "			[]," +
                "			[{" +
                "				'k': ['21683865395396608', '315263844207558671', '392832386079129630', '114560575232671745', '689502507277615175']," +
                "				'b': 1" +
                "			}]" +
                "		]," +
                "		[3567166443, null, 3, [" +
                "				[2, [{" +
                "					's': 0," +
                "					'e': 10000" +
                "				}]]" +
                "			]," +
                "			[" +
                "				[580727696, [" +
                "					[3399957344, 51326]," +
                "					[1238858341, null]" +
                "				]]" +
                "			]," +
                "			[]" +
                "		]" +
                "	]" +
                "}," +
                "{" +
                "	'resp': true," +
                "	'resp2': 'true'" +
                "}]";
            sj = sj.Replace('\'', '"');
            stopWatch = new Stopwatch();
            stopWatch.Start();
            var jj = new JsonObjectArray(sj);
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            Console.WriteLine(jj.ToFormatString() + "\r\n\r\n");

            Console.ReadLine();
        }
    }
}
