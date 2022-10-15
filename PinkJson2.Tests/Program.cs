using System;

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
