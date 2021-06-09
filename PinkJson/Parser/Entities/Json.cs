using PinkJson.Lexer;
using PinkJson.Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Dynamic;
using System.Reflection;

namespace PinkJson
{
    public class Json : JsonBase<JsonObject>
    {
        private int currentposition = 0;
        private TokenCollection tokens;

        #region Constructors
        public Json()
        {
        }

        public Json(string json) : this(new JsonLexer().Tokenize(json))
        {
        }

        public Json(IEnumerable<JsonObject> json) : base(json)
        {
        }

        public Json(List<JsonObject> json) : base(json)
        {
        }

        public Json(JsonObject[] json) : base(json)
        {
        }

        public Json(JsonObject jsonobject) : base(new JsonObject[] { jsonobject })
        {
        }

        public static Json FromAnonymous(dynamic anonymous)
        {
            return new Json(AnonymousConverter.Convert(anonymous) as List<JsonObject>);
        }

        public static Json FromObject(object structure, bool usePrivateFields, string[] exclusion_fields = null)
        {
            return new Json(JsonConverter.ConvertFrom(structure, usePrivateFields, exclusion_fields));
        }

        public static T ToObject<T>(Json json)
        {
            return JsonConverter.ConvertTo<T>(json);
        }
        #endregion

        #region Parser
        public Json(TokenCollection json)
        {
            tokens = json;

            if (tokens[0].Kind == SyntaxKind.OBA && tokens[tokens.Count - 1].Kind == SyntaxKind.CBA)
                throw new Exception("Use JsonArray(string json).");
            else if (tokens[0].Kind == SyntaxKind.OB && tokens[tokens.Count - 1].Kind == SyntaxKind.CB)
            {
                tokens.RemoveAt(tokens.Count - 1);
                tokens.RemoveAt(0);
            }
            else
                throw new Exception("Unknown Json format.");

            Parse();
        }

        private void Parse()
        {
            for (currentposition = 0; currentposition < tokens.Count; currentposition++)
            {
                var elem = tokens[currentposition];

                switch (elem.Kind)
                {
                    case SyntaxKind.EQSEPARATOR:
                        AddJsonObject();
                        break;
                }
            }
        }

        private void AddJsonObject()
        {
            var key = tokens[currentposition - 1].Value.ToString();
            object value;

            switch (tokens[currentposition + 1].Kind)
            {
                case SyntaxKind.OB:
                    currentposition++;
                    value = new Json(GetInBrackets());
                    break;
                case SyntaxKind.OBA:
                    currentposition++;
                    value = new JsonArray(GetInBrackets());
                    break;
                default:
                    value = tokens[currentposition + 1].Value;
                    break;
            }

            Add(new JsonObject(key, value));

            currentposition++;
        }

        private TokenCollection GetInBrackets()
        {
            TokenCollection result = new TokenCollection();

            var gen = 0;
            do
            {
                var elem = tokens[currentposition];

                switch (elem.Kind)
                {
                    case SyntaxKind.OB:
                    case SyntaxKind.OBA:
                        gen++;
                        break;
                    case SyntaxKind.CB:
                    case SyntaxKind.CBA:
                        gen--;
                        break;
                }

                result.Add(elem);

                currentposition++;
            }
            while (gen != 0);

            return result;
        }
        #endregion

        #region Static
        public static object ValueToFormatJsonString(object value, int spacing, int gen)
        {
            if (value is ObjectBase)
                value = (value as ObjectBase).ToFormatString(spacing, gen);
            //else if (value is JsonObject)
            //    value = (value as JsonObject).ToFormatString(spacing, gen);
            //else if (value is JsonArray)
            //    value = (value as JsonArray).ToFormatString(spacing, gen);
            //else if (value is JsonArrayObject)
            //    value = (value as JsonArrayObject).ToFormatString(spacing, gen);
            else
                value = ValueToJsonString(value);
            return value;
        }

        public static object ValueToJsonString(object value)
        {
            if (value is null)
                return "null";
            else if (value is bool)
                return ((bool)value) ? "true" : "false";
            else if (value is DateTime)
                return '\"' + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + '\"';
            else if (value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal)
                return value.ToString().Replace(',', '.');
            else if (value is ObjectBase)
                return (value as ObjectBase).ToString();
            //else if (value is JsonObject)
            //    return (value as JsonObject).ToString();
            //else if (value is JsonArray)
            //    return (value as JsonArray).ToString();
            //else if (value is JsonArrayObject)
            //    return (value as JsonArrayObject).ToString();
            else
                return $"\"{value.ToString().EscapeString()}\"";
        }
        #endregion

        #region Override
        public override JsonObject ElementByKey(string key)
        {
            foreach (var jsonObject in this)
                if (jsonObject.Key == key)
                    return jsonObject;

            return null;
        }

        public override int IndexByKey(string key)
        {
            for (var i = 0; i < Count; i++)
            {
                var jsonObject = this[i];
                if (jsonObject.Key == key)
                    return i;
            }

            return -1;
        }

        public override void RemoveByKey(string key)
        {
            RemoveAt(IndexByKey(key));
        }

        public override string ToString()
        {
            return $"{{{string.Join(", ", this.Select(o => Json.ValueToJsonString(o)))}}}";
        }

        public override string ToFormatString(int spacing = 4, int gen = 1)
        {
            if (Count == 0)
                return "{}";
            else
                return $"{{\r\n" +
                           $"{string.Join(",\r\n", this.Select(o => Json.ValueToFormatJsonString(o, spacing, gen + 1)))}" +
                       $"\r\n{new string(' ', spacing * (gen - 1))}}}";
        }

        public override object Clone()
        {
            return new Json(this);
        }
        #endregion
    }
}
