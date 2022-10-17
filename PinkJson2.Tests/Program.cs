using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Tests
{
    //  ____  _       _       _                 
    // |  _ \(_)_ __ | | __  | |___  ___  _ __  
    // | |_) | | '_ \| |/ /  | / __|/ _ \| '_ \ 
    // |  __/| | | | |   < |_| \__ \ (_) | | | |
    // |_|   |_|_| |_|_|\_\___/|___/\___/|_| |_|

    public static class Program
    {
        private class Directory
        {
            public string Name { get; set; }
            public Directory Parent { get; set; }
            public File MetaFile { get; set; }
            [JsonProperty(DeserializeToType = typeof(File[]))]
            public IEnumerable<File> Files { get; set; }
        }

        private class File
        {
            public string Name { get; set; }
            public Directory Parent { get; set; }
        }

        public static void Main(string[] args)
        {
            //var obj = new
            //{
            //    test1 = "str",
            //    test2 = new
            //    {
            //        test4 = "str2",
            //        test5 = true
            //    },
            //    test4 = Enumerable.Range(1, 100),
            //    test3 = false
            //};

            //var obj = new object[] { 1, 2, new[] { 5, 6, 7 }, 3, 4 };

            //var json2 = new ObjectSerializer().Serialize(obj).ToJson();

            //var json = new ArgumentNullException("test_patam").Serialize().ToJson();

            //var root = new Directory { Name = "Root" };
            //var documents = new Directory { Name = "My Documents", Parent = root };
            //var file = new File { Name = "ImportantLegalDocument.docx", Parent = documents };

            //root.MetaFile = file;
            //root.Files = new List<File> { file, file };
            //documents.Files = root.Files;

            //ObjectSerializerOptions.Default.PreserveObjectsReferences = true;

            //var json = Json.Serialize(documents).ToJson();

            //Console.WriteLine(json.ToString(new PrettyFormatter()));

            //var obj = json.Deserialize<Directory>();

            //return;

            ShowLogo();

            Examples.Start();
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
    }
}
