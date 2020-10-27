using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson.Parser
{
    public class SyntaxHighlighting
    {
        public static Color BackgroundColor = new Color(System.Drawing.Color.White);

        public static Color BracketsColor = new Color(System.Drawing.Color.Black) { RtfTableIndex = 1 };
        public static Color CommaColor = new Color(System.Drawing.Color.Black) { RtfTableIndex = 2 };
        public static Color DoubleDotColor = new Color(System.Drawing.Color.Black) { RtfTableIndex = 3 };
        public static Color NullValueColor = new Color(24, 175, 138) { RtfTableIndex = 4 };
        public static Color BoolValueColor = new Color(24, 175, 138) { RtfTableIndex = 5 };
        public static Color NumberValueColor = new Color(255, 128, 0) { RtfTableIndex = 6 };
        public static Color StringValueColor = new Color(128, 0, 58) { RtfTableIndex = 7 };
        public static Color KeyColor = new Color(128, 0, 255) { RtfTableIndex = 8 };

        public static string ToAnsiWithEscapeSequences(object value, int spacing = 4, int gen = 1)
        {
            var result = BackgroundColor.ToAnsiBackgroundEscapeCode() + "\x1b[J";
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                {
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += "{}";
                }
                else
                {
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += "{\r\n";
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result += ToAnsiWithEscapeSequences(element, spacing, gen + 1);
                        if (i != valueAsJson.Count - 1)
                        {
                            result += CommaColor.ToAnsiForegroundEscapeCode();
                            result += ",\r\n";
                        }
                    }
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += $"\r\n{new string(' ', spacing * (gen - 1))}}}";
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result += KeyColor.ToAnsiForegroundEscapeCode();
                result += $"{new string(' ', spacing * (gen - 1))}\"{valueAsJsonObject.Key}\"";
                result += DoubleDotColor.ToAnsiForegroundEscapeCode();
                result += ": ";
                result += ToAnsiWithEscapeSequences(valueAsJsonObject.Value, spacing, gen);
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                {
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += "[]";
                }
                else
                {
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += "[\r\n";
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result += new string(' ', spacing * gen);
                        result += ToAnsiWithEscapeSequences(element, spacing, gen + 1);
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result += CommaColor.ToAnsiForegroundEscapeCode();
                            result += ",\r\n";
                        }
                    }
                    result += BracketsColor.ToAnsiForegroundEscapeCode();
                    result += $"\r\n{new string(' ', spacing * (gen - 1))}]";
                }
            }
            else if (value is null)
            {
                result += NullValueColor.ToAnsiForegroundEscapeCode();
                result += "null";
            }
            else if (value is bool)
            {
                result += BoolValueColor.ToAnsiForegroundEscapeCode();
                result += ((bool)value) ? "true" : "false";
            }
            else if (value is long)
            {
                result += NumberValueColor.ToAnsiForegroundEscapeCode();
                result += ((long)value).ToString();
            }
            else if (value is int)
            {
                result += NumberValueColor.ToAnsiForegroundEscapeCode();
                result += ((int)value).ToString();
            }
            else if (value is short)
            {
                result += NumberValueColor.ToAnsiForegroundEscapeCode();
                result += ((short)value).ToString();
            }
            else
            {
                result += StringValueColor.ToAnsiForegroundEscapeCode();
                result += $"\"{value.ToString().EscapeString()}\"";
            }

            return result;
        }

        public static string ToRtf(object value, Encoding encoding, int spacing = 4, int gen = 1)
        {
            var result = @"{\rtf1\" + encoding.WebName + @"\deff0 {\fonttbl {\f0 Courier New;}}";
            result += @"{\colortbl;";
            result += BracketsColor.ToRtfTableColor();
            result += CommaColor.ToRtfTableColor();
            result += DoubleDotColor.ToRtfTableColor();
            result += NullValueColor.ToRtfTableColor();
            result += BoolValueColor.ToRtfTableColor();
            result += NumberValueColor.ToRtfTableColor();
            result += StringValueColor.ToRtfTableColor();
            result += KeyColor.ToRtfTableColor();
            result += @"}";
            result += _ToRtf(value, spacing, gen);
            return result += @"}";
        }

        public static string ToHtml(object value, int spacing = 4, int gen = 1)
        {
            var result = @"<html><body>";
            result += _ToHtml(value, spacing, gen);
            return result += @"</body></html>";
        }


        private static string _ToHtml(object value, int spacing = 4, int gen = 1)
        {
            var result = "";
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                    result += BracketsColor.ToHtml("{}");
                else
                {
                    result += BracketsColor.ToHtml("{<br>");
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result += _ToHtml(element, spacing, gen + 1);
                        if (i != valueAsJson.Count - 1)
                        {
                            result += CommaColor.ToHtml(",<br>");
                        }
                    }
                    result += BracketsColor.ToHtml($"<br>{new_string("&nbsp;", spacing * (gen - 1))}}}");
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result += KeyColor.ToHtml($"{new_string("&nbsp;", spacing * (gen - 1))}\"{valueAsJsonObject.Key}\"");
                result += DoubleDotColor.ToHtml(": ");
                result += _ToHtml(valueAsJsonObject.Value, spacing, gen);
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                    result += BracketsColor.ToHtml("[]");
                else
                {
                    result += BracketsColor.ToHtml("[<br>");
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result += new_string("&nbsp;", spacing * gen);
                        result += _ToHtml(element, spacing, gen + 1);
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result += CommaColor.ToHtml(",<br>");
                        }
                    }
                    result += BracketsColor.ToHtml($"<br>{new_string("&nbsp;", spacing * (gen - 1))}]");
                }
            }
            else if (value is null)
            {
                result += NullValueColor.ToHtml("null");
            }
            else if (value is bool)
            {
                result += BoolValueColor.ToHtml(((bool)value) ? "true" : "false");
            }
            else if (value is long)
            {
                result += NumberValueColor.ToHtml(((long)value).ToString());
            }
            else if (value is int)
            {
                result += NumberValueColor.ToHtml(((int)value).ToString());
            }
            else if (value is short)
            {
                result += NumberValueColor.ToHtml(((short)value).ToString());
            }
            else
            {
                var tmp = value.ToString().EscapeString();
                var isUri = Uri.IsWellFormedUriString(tmp, UriKind.Absolute);
                result += isUri ? $"<a href=\"{tmp}\">\"{tmp}\"</a>" : StringValueColor.ToHtml($"\"{tmp}\"");
            }

            return result;
        }

        private static string _ToRtf(object value, int spacing = 4, int gen = 1)
        {
            var result = "";
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                {
                    result += BracketsColor.ToRtf();
                    result += @"\{\}";
                }
                else
                {
                    result += BracketsColor.ToRtf();
                    result += @"\{\line";
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result += _ToRtf(element, spacing, gen + 1);
                        if (i != valueAsJson.Count - 1)
                        {
                            result += CommaColor.ToRtf();
                            result += @",\line";
                        }
                    }
                    result += BracketsColor.ToRtf();
                    result += $"\\line{new string(' ', spacing * (gen - 1))}\\}}";
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result += KeyColor.ToRtf();
                result += $"{new string(' ', spacing * (gen - 1))}\"{valueAsJsonObject.Key}\"";
                result += DoubleDotColor.ToRtf();
                result += ": ";
                result += _ToRtf(valueAsJsonObject.Value, spacing, gen);
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                {
                    result += BracketsColor.ToRtf();
                    result += "[]";
                }
                else
                {
                    result += BracketsColor.ToRtf();
                    result += @"[\line";
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result += new string(' ', spacing * gen);
                        result += _ToRtf(element, spacing, gen + 1);
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result += CommaColor.ToRtf();
                            result += @",\line";
                        }
                    }
                    result += BracketsColor.ToRtf();
                    result += $"\\line{new string(' ', spacing * (gen - 1))}]";
                }
            }
            else if (value is null)
            {
                result += NullValueColor.ToRtf();
                result += "null";
            }
            else if (value is bool)
            {
                result += BoolValueColor.ToRtf();
                result += ((bool)value) ? "true" : "false";
            }
            else if (value is long)
            {
                result += NumberValueColor.ToRtf();
                result += ((long)value).ToString();
            }
            else if (value is int)
            {
                result += NumberValueColor.ToRtf();
                result += ((int)value).ToString();
            }
            else if (value is short)
            {
                result += NumberValueColor.ToRtf();
                result += ((short)value).ToString();
            }
            else
            {
                result += StringValueColor.ToRtf();
                result += $"\"{value.ToString().EscapeString().EscapeString()}\"";
            }

            return result;
        }

        private static string new_string(object value, int count)
        {
            var result = "";
            for (var i = 0; i < count; i++)
                result += value;
            return result;
        }


        //P\Invoke
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void EnableVirtualTerminalProcessing()
        {
            IntPtr hOut = GetStdHandle(-11);
            uint dwMode = 0;
            GetConsoleMode(hOut, out dwMode);
            dwMode |= 0x0004;
            SetConsoleMode(hOut, dwMode);
        }
    }

    public class Color
    {
        public byte R, G, B;
        public int RtfTableIndex;

        public Color(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }

        public Color(System.Drawing.Color color)
        {
            R = color.R; G = color.G; B = color.B;
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
    }
}
