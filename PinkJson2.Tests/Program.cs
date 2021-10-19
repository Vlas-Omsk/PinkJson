using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson2.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
			var lexer = new Lexer((@"{
                'octal': 0o52,
				'decimal': " + long.MaxValue + @",
				'hex': 0x2AF,
				'binary': 0b00101010,
				'string': '\02\ufeff'
            }").Replace('\'', '"'));

			foreach (var current in lexer)
			{
				Console.Write("Type: " + current.Type + " ");
				if (current.Type == TokenType.String)
					Console.Write("Value: " + current.Value);
				else if (current.Type == TokenType.Number)
					Console.Write("Value: " + current.Value);
				Console.WriteLine();
			}

			Console.ReadLine();
		}
    }
}
