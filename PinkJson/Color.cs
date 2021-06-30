using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PinkJson
{
    public class Color
    {
        public byte R, G, B;
        public int RtfTableIndex;

        public Color(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }

        public Color(int hex)
        {
            var sHex = hex.ToString("x");
            if (sHex.Length == 1)
                sHex = sHex[0].Repeat(6);
            else if (sHex.Length == 2)
                sHex = (sHex[0].ToString() + sHex[1]).Repeat(3);
            else if (sHex.Length == 3)
                sHex = sHex[0].Repeat(2) + sHex[1].Repeat(2) + sHex[2].Repeat(2);
            else if (sHex.Length == 4)
                sHex = sHex.Substring(0, 2) + sHex[2].Repeat(2) + sHex[3].Repeat(2);
            else if (sHex.Length == 5)
                sHex = sHex.Substring(0, 4) + sHex[4].Repeat(2);

            R = Convert.ToByte(sHex.Substring(0, 2), 16);
            G = Convert.ToByte(sHex.Substring(2, 2), 16);
            B = Convert.ToByte(sHex.Substring(4, 2), 16);
        }

        public string ToAnsiForegroundEscapeCode()
        {
            return $"\x1b[38;2;{R};{G};{B}m";
        }

        public string ToAnsiBackgroundEscapeCode()
        {
            return $"\x1b[48;2;{R};{G};{B}m";
        }

        public string ToRtfTableColor()
        {
            return $"\\red{R}\\green{G}\\blue{B};";
        }

        public string ToRtf()
        {
            return $"\\cf{RtfTableIndex} ";
        }

        public string ToHtml(string innerText)
        {
            return $"<font style=\"color: rgb({R}, {G}, {B})\">{innerText}</font>";
        }

        public static implicit operator Color(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B);
        }
    }
}
