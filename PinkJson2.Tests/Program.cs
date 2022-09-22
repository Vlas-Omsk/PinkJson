using System;
using System.Collections;
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
        public static void Main(string[] args)
        {
            ShowLogo();

            //ShowTypeComparsion();

            ParserBenchmark.Start();
            Examples.Start();

            Console.ReadLine();
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

        private static void ShowTypeComparsion()
        {
            var table = new List<string[]>();

            table.Add(new string[6]
            {
                "Name",
                typeof(string).Name,
                typeof(int).Name,
                typeof(DictionaryEntry).Name,
                typeof(Dictionary<object, object>).Name,
                typeof(DateTime).Name,
            });
            table.Add(null);

            foreach (var prop in GetProps(typeof(string)))
            {
                var row = new string[table[0].Length];
                row[0] = prop.Name.ToString();
                row[1] = prop.Value.ToString();
                table.Add(row);
            }

            var i = 2;
            foreach (var prop in GetProps(typeof(int)))
            {
                var row = table[i++];
                row[2] = prop.Value.ToString();
            }

            i = 2;
            foreach (var prop in GetProps(typeof(DictionaryEntry)))
            {
                var row = table[i++];
                row[3] = prop.Value.ToString();
            }

            i = 2;
            foreach (var prop in GetProps(typeof(Dictionary<object, object>)))
            {
                var row = table[i++];
                row[4] = prop.Value.ToString();
            }

            i = 2;
            foreach (var prop in GetProps(typeof(DateTime)))
            {
                var row = table[i++];
                row[5] = prop.Value.ToString();
            }

            Console.WriteLine(TextTablePreview.ToString(table));
        }

        private static IEnumerable<(string Name, object Value)> GetProps(Type type)
        {
            foreach (var prop in type.GetType().GetProperties())
            {
                if (!prop.Name.StartsWith("Is"))
                    continue;

                yield return (prop.Name, prop.GetValue(type));
            }
        }
    }

    public static class TextTablePreview
    {
        //Values[row][column]

        public static string ToString(IEnumerable<IEnumerable<string>> values)
        {
            var rowsCount = values.Count();
            var columnsCount = 0;

            foreach (var row in values)
            {
                if (row == null)
                    continue;
                var count = row.Count();
                if (count > columnsCount)
                    columnsCount = count;
            }

            var columnLengths = new int[columnsCount];

            foreach (var row in values)
            {
                if (row == null)
                    continue;
                var i = 0;
                foreach (var column in row)
                {
                    var value = column ?? "";
                    if (value.Length > columnLengths[i])
                        columnLengths[i] = value.Length;
                    i++;
                }
            }

            return string.Join("\r\n", values.Select(row =>
            {
                string result;
                if (row == null)
                    result = "|" + string.Join("|", columnLengths.Select(i => new string('-', i + 2))) + "|";
                else
                    result = "| " + string.Join(" | ", row.Select((column, j) =>
                    {
                        var value = column ?? "";
                        return value + new string(' ', columnLengths[j] - value.Length);
                    })) + " |";
                return result;
            }));
        }
    }
}
