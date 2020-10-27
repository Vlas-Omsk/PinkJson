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
            var result = new StringBuilder(BackgroundColor.ToAnsiBackgroundEscapeCode() + "\x1b[J");
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                {
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append("{}");
                }
                else
                {
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append("{\r\n");
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result.Append(ToAnsiWithEscapeSequences(element, spacing, gen + 1));
                        if (i != valueAsJson.Count - 1)
                        {
                            result.Append(CommaColor.ToAnsiForegroundEscapeCode());
                            result.Append(",\r\n");
                        }
                    }
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append($"\r\n{new string(' ', spacing * (gen - 1))}}}");
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result.Append(KeyColor.ToAnsiForegroundEscapeCode());
                result.Append($"{new string(' ', spacing * (gen - 1))}\"{valueAsJsonObject.Key}\"");
                result.Append(DoubleDotColor.ToAnsiForegroundEscapeCode());
                result.Append(": ");
                result.Append(ToAnsiWithEscapeSequences(valueAsJsonObject.Value, spacing, gen));
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                {
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append("[]");
                }
                else
                {
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append("[\r\n");
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result.Append(new string(' ', spacing * gen));
                        result.Append(ToAnsiWithEscapeSequences(element, spacing, gen + 1));
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result.Append(CommaColor.ToAnsiForegroundEscapeCode());
                            result.Append(",\r\n");
                        }
                    }
                    result.Append(BracketsColor.ToAnsiForegroundEscapeCode());
                    result.Append($"\r\n{new string(' ', spacing * (gen - 1))}]");
                }
            }
            else if (value is null)
            {
                result.Append(NullValueColor.ToAnsiForegroundEscapeCode());
                result.Append("null");
            }
            else if (value is bool)
            {
                result.Append(BoolValueColor.ToAnsiForegroundEscapeCode());
                result.Append(((bool)value) ? "true" : "false");
            }
            else if (value is long)
            {
                result.Append(NumberValueColor.ToAnsiForegroundEscapeCode());
                result.Append(((long)value).ToString());
            }
            else if (value is int)
            {
                result.Append(NumberValueColor.ToAnsiForegroundEscapeCode());
                result.Append(((int)value).ToString());
            }
            else if (value is short)
            {
                result.Append(NumberValueColor.ToAnsiForegroundEscapeCode());
                result.Append(((short)value).ToString());
            }
            else
            {
                result.Append(StringValueColor.ToAnsiForegroundEscapeCode());
                result.Append($"\"{value.ToString().EscapeString()}\"");
            }

            return result.ToString();
        }

        public static string ToRtf(object value, Encoding encoding, int spacing = 4, int gen = 1)
        {
            var result = new StringBuilder(@"{\rtf1\" + encoding.WebName + @"\deff0 {\fonttbl {\f0 Courier New;}}");
            result.Append(@"{\colortbl;");
            result.Append(BracketsColor.ToRtfTableColor());
            result.Append(CommaColor.ToRtfTableColor());
            result.Append(DoubleDotColor.ToRtfTableColor());
            result.Append(NullValueColor.ToRtfTableColor());
            result.Append(BoolValueColor.ToRtfTableColor());
            result.Append(NumberValueColor.ToRtfTableColor());
            result.Append(StringValueColor.ToRtfTableColor());
            result.Append(KeyColor.ToRtfTableColor());
            result.Append(@"}");
            result.Append(_ToRtf(value, spacing, gen));
            return result.Append(@"}").ToString();
        }

        public static string ToHtml(object value, int spacing = 4, int gen = 1)
        {
            var result = new StringBuilder(@"<html><body>");
            result.Append(_ToHtml(value, spacing, gen));
            return result.Append(@"</body></html>").ToString();
        }


        private static string _ToHtml(object value, int spacing = 4, int gen = 1)
        {
            var result = new StringBuilder("");
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                    result.Append(BracketsColor.ToHtml("{}"));
                else
                {
                    result.Append(BracketsColor.ToHtml("{<br>"));
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result.Append(_ToHtml(element, spacing, gen + 1));
                        if (i != valueAsJson.Count - 1)
                        {
                            result.Append(CommaColor.ToHtml(",<br>"));
                        }
                    }
                    result.Append(BracketsColor.ToHtml($"<br>{new_string("&nbsp;", spacing * (gen - 1))}}}"));
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result.Append(KeyColor.ToHtml($"{new_string("&nbsp;", spacing * (gen - 1))}\"{valueAsJsonObject.Key}\""));
                result.Append(DoubleDotColor.ToHtml(": "));
                result.Append(_ToHtml(valueAsJsonObject.Value, spacing, gen));
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                    result.Append(BracketsColor.ToHtml("[]"));
                else
                {
                    result.Append(BracketsColor.ToHtml("[<br>"));
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result.Append(new_string("&nbsp;", spacing * gen));
                        result.Append(_ToHtml(element, spacing, gen + 1));
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result.Append(CommaColor.ToHtml(",<br>"));
                        }
                    }
                    result.Append(BracketsColor.ToHtml($"<br>{new_string("&nbsp;", spacing * (gen - 1))}]"));
                }
            }
            else if (value is null)
            {
                result.Append(NullValueColor.ToHtml("null"));
            }
            else if (value is bool)
            {
                result.Append(BoolValueColor.ToHtml(((bool)value) ? "true" : "false"));
            }
            else if (value is long)
            {
                result.Append(NumberValueColor.ToHtml(((long)value).ToString()));
            }
            else if (value is int)
            {
                result.Append(NumberValueColor.ToHtml(((int)value).ToString()));
            }
            else if (value is short)
            {
                result.Append(NumberValueColor.ToHtml(((short)value).ToString()));
            }
            else
            {
                var tmp = value.ToString().EscapeString();
                var isUri = Uri.IsWellFormedUriString(tmp, UriKind.Absolute);
                result.Append(isUri ? $"<a href=\"{tmp}\">\"{tmp}\"</a>" : StringValueColor.ToHtml($"\"{tmp}\""));
            }

            return result.ToString();
        }

        private static string _ToRtf(object value, int spacing = 4, int gen = 1)
        {
            var result = new StringBuilder("");
            if (value is Json)
            {
                var valueAsJson = value as Json;
                if (valueAsJson.Count == 0)
                {
                    result.Append(BracketsColor.ToRtf());
                    result.Append(@"\{\}");
                }
                else
                {
                    result.Append(BracketsColor.ToRtf());
                    result.Append(@"\{\line");
                    for (var i = 0; i < valueAsJson.Count; i++)
                    {
                        var element = valueAsJson[i];
                        result.Append(_ToRtf(element, spacing, gen + 1));
                        if (i != valueAsJson.Count - 1)
                        {
                            result.Append(CommaColor.ToRtf());
                            result.Append(@",\line");
                        }
                    }
                    result.Append(BracketsColor.ToRtf());
                    result.Append($"\\line{new string(' ', spacing * (gen - 1))}\\}}");
                }
            }
            else if (value is JsonObject)
            {
                var valueAsJsonObject = value as JsonObject;
                result.Append(KeyColor.ToRtf());
                result.Append($"{new string(' ', spacing * (gen - 1))}\"{valueAsJsonObject.Key}\"");
                result.Append(DoubleDotColor.ToRtf());
                result.Append(": ");
                result.Append(_ToRtf(valueAsJsonObject.Value, spacing, gen));
            }
            else if (value is JsonObjectArray)
            {
                var valueAsJsonObjectArray = value as JsonObjectArray;
                if (valueAsJsonObjectArray.Count == 0)
                {
                    result.Append(BracketsColor.ToRtf());
                    result.Append("[]");
                }
                else
                {
                    result.Append(BracketsColor.ToRtf());
                    result.Append(@"[\line");
                    for (var i = 0; i < valueAsJsonObjectArray.Count; i++)
                    {
                        var element = valueAsJsonObjectArray[i];
                        result.Append(new string(' ', spacing * gen));
                        result.Append(_ToRtf(element, spacing, gen + 1));
                        if (i != valueAsJsonObjectArray.Count - 1)
                        {
                            result.Append(CommaColor.ToRtf());
                            result.Append(@",\line");
                        }
                    }
                    result.Append(BracketsColor.ToRtf());
                    result.Append($"\\line{new string(' ', spacing * (gen - 1))}]");
                }
            }
            else if (value is null)
            {
                result.Append(NullValueColor.ToRtf());
                result.Append("null");
            }
            else if (value is bool)
            {
                result.Append(BoolValueColor.ToRtf());
                result.Append(((bool)value) ? "true" : "false");
            }
            else if (value is long)
            {
                result.Append(NumberValueColor.ToRtf());
                result.Append(((long)value).ToString());
            }
            else if (value is int)
            {
                result.Append(NumberValueColor.ToRtf());
                result.Append(((int)value).ToString());
            }
            else if (value is short)
            {
                result.Append(NumberValueColor.ToRtf());
                result.Append(((short)value).ToString());
            }
            else
            {
                result.Append(StringValueColor.ToRtf());
                result.Append($"\"{value.ToString().EscapeString().EscapeString()}\"");
            }

            return result.ToString();
        }

        private static string new_string(object value, int count)
        {
            var result = new StringBuilder("");
            for (var i = 0; i < count; i++)
                result.Append(value);
            return result.ToString();
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
