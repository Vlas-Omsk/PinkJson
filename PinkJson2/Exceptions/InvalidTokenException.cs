using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson2
{
    public class InvalidTokenException : LexerException
    {
        public InvalidTokenException(int position, StreamReader stream) : base("Invalid token", position, stream)
        {
        }
    }
}
