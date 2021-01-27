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
                            
                        }
                    }
                },
                new Phone() {
                    Name = "XIAOMI REDMI NOTE 7",
                    
                    dynamicHuinya = true
                }
            }
        };

        static void Main(string[] args)
        {
            SyntaxHighlighting.EnableVirtualTerminalProcessing();

            var j = Json.FromAnonymous(new
            {
                abc = new
                {
                    hello = "123",
                    h1 = new JsonArray()
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
                                ___dsad = 2,
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

            var clone = (((j["abc"][1].Value as JsonArray)[4].Value as Json)["arr"]["lol"][1].Value as Json).Clone() as Json; //Было
            (((j["abc"][1].Value as JsonArray)[4].Value as Json)["arr"]["lol"].Value as JsonArray).Add(clone);                //Было
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            clone = j["abc"][1][4]["arr"]["lol"][1].Get<Json>().Clone() as Json;                                              //Стало v1
            j["abc"][1][4]["arr"]["lol"].Get<JsonArray>().Add(clone);                                                         //Стало v1
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            clone = j.Dyn.abc._1._4.arr.lol._1.Get<Json>().Clone();                                                           //Стало v2
            j.Dyn.abc._1._4.arr.lol.Get<JsonArray>().Add(clone);                                                              //Стало v2

            j.Dyn.abc._1._4.arr.___dsad.Key = "s\ntewrwe";

            Console.WriteLine(new Json(j["abc"]).ToFormatString());

            //Console.WriteLine(SyntaxHighlighting.ToAnsiWithEscapeSequences(Json.FromStructure(test, true)));

            //Console.WriteLine(j.Dyn._ToFormatString);

            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();
            //var j = new Json(File.ReadAllText("in.json"));
            //File.WriteAllText(@"test.html", SyntaxHighlighting.ToHtml(j));
            //File.WriteAllText(@"test.json", j.ToFormatString(), Encoding.UTF8);
            //foreach (Json json in j["response"]["items"].Value as JsonObjectArray)
            //{
            //    Console.WriteLine("\r\n" + json["conversation_message_id"].Value + ((int)json["from_id"].Value == 272611387 ? " Egor" : " Vlas"));
            //    Console.WriteLine(SyntaxHighlighting.ToAnsiWithEscapeSequences(json["text"].Value));
            //    if (json.IndexByKey("fwd_messages") != -1)
            //        foreach (Json jsonfwd in json["fwd_messages"].Value as JsonObjectArray)
            //            Console.WriteLine("\t" + SyntaxHighlighting.ToAnsiWithEscapeSequences(jsonfwd["text"].Value));
            //}
            //var f = File.OpenWrite("testst.txt");
            //for (var i = 0; i < 200000; i++)
            //{
            //    var bytes = Encoding.UTF8.GetBytes($"{i}: &#{i};\r\n");
            //    f.Write(bytes, 0, bytes.Length);
            //}
            //f.Close();
            //Console.WriteLine(SyntaxHighlighting.ToAnsiWithEscapeSequences(j));
            //stopWatch.Stop();
            //Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            //Thread.Sleep(0);
            //Process.Start("cmd", "/c start test.html");
            //Process.Start("cmd", "/c start test.rtf");

            //Console.WriteLine(@"\u0072\u0079\u0020y\n performa\\nce   TEST".UnescapeString().EscapeString().ToUnicodeString());

            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();
            //var j = new JsonObjectArray(File.ReadAllText(@"detectable.json"));
            //var s = Json.FromStructure(test, true);
            //stopWatch.Stop();
            //Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            //File.WriteAllText("test.json", s.ToFormatString());

            //stopWatch.Start();
            //var file = new Json(File.ReadAllText("test.json"));
            //var testout = Json.ToStructure<Test>(new Json(file));
            //stopWatch.Stop();
            //Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");

            //var ss = Json.FromStructure(testout, true);
            //Console.WriteLine(ss.ToFormatString() + "\r\n\r\n");

            //Main2(args);

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
                    h1 = new JsonArray()
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
            var jj = new JsonArray(sj);
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
            Console.WriteLine(jj.ToFormatString() + "\r\n\r\n");

            Console.ReadLine();
        }
    }
}
