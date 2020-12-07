using PinkJson.Lexer;
using PinkJson.Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson.Parser
{
    public class Json : List<JsonObject>, ICloneable
    {
        private int currentposition = 0;
        private TokenCollection tokens;

        #region Json
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

        public static Json FromAnonymous(dynamic anonymous)
        {
            return new Json(AnonymousConverter.Convert(anonymous) as List<JsonObject>);
        }

        public static Json FromStructure(object structure, bool usePrivateFields, string[] exclusion_fields = null)
        {
            return new Json(StructureConverter.ConvertFrom(structure, usePrivateFields, exclusion_fields));
        }

        public static T ToStructure<T>(Json json)
        {
            return StructureConverter.ConvertTo<T>(json);
        }
        #endregion

        public Json(TokenCollection json)
        {
            tokens = json;

            if (tokens[0].Kind == SyntaxKind.OBA && tokens[tokens.Count - 1].Kind == SyntaxKind.CBA)
                throw new Exception("Use JsonObjectArray(string json).");
            else if (tokens[0].Kind == SyntaxKind.OB && tokens[tokens.Count - 1].Kind == SyntaxKind.CB)
            {
                tokens.RemoveAt(tokens.Count - 1);
                tokens.RemoveAt(0);
            } else
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
                    value = new JsonObjectArray(GetInBrackets());
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

        #region Static
        public static object ValueToFormatJsonString(object value, int spacing, int gen)
        {
            if (value is Json)
                value = (value as Json).ToFormatString(spacing, gen);
            else if (value is JsonObject)
                value = (value as JsonObject).ToFormatString(spacing, gen);
            else if (value is JsonObjectArray)
                value = (value as JsonObjectArray).ToFormatString(spacing, gen);
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
            else if (  value is sbyte
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
                return value.ToString();
            else if(value is Json)
                return (value as Json).ToString();
            else if (value is JsonObject)
                return (value as JsonObject).ToString();
            else if (value is JsonObjectArray)
                return (value as JsonObjectArray).ToString();
            else
                return $"\"{value.ToString().EscapeString()}\"";
        }
        #endregion

        public override string ToString()
        {
            return $"{{{string.Join(", ", this.Select(o => Json.ValueToJsonString(o)))}}}";
        }

        public string ToFormatString(int spacing = 4, int gen = 1)
        {
            if (Count == 0)
                return "{}";
            else
                return $"{{\r\n" +
                           $"{string.Join(",\r\n", this.Select(o => Json.ValueToFormatJsonString(o, spacing, gen+1)))}" +
                       $"\r\n{new string(' ', spacing * (gen-1))}}}";
        }

        #region List Methods
        public JsonObject ElementByKey(string key)
        {
            foreach (var jsonObject in this)
                if (jsonObject.Key == key)
                    return jsonObject;
            return null;
        }

        /// <summary>
        /// Gets the index of element at the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexByKey(string key)
        {
            for (var i = 0; i < Count; i++)
            {
                var jsonObject = this[i];
                if (jsonObject.Key == key)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Deletes an element by the specified key
        /// </summary>
        /// <param name="key"></param>
        public void RemoveByKey(string key)
        {
            RemoveAt(IndexByKey(key));
        }

        /// <summary>
        /// Gets or sets the element at the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public JsonObject this[string key]
        {
            get =>
                ElementByKey(key);
            set =>
                this[IndexByKey(key)] = value;
        }

        public object Clone()
        {
            return new Json(this);
        }
        #endregion
    }

    public class JsonObject : ICloneable
    {
        public string Key { get; set; }
        public object Value { get; set; }

        public JsonObject(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public Type GetValType()
        {
            return Value.GetType();
        }

        #region Value Methods
        public JsonObject this[string key]
        {
            get
            {
                if (GetValType() == typeof(Json))
                    return (Value as Json)[key];

                throw new Exception($"Cannot get value by key \"{key}\".");
            }
            set
            {
                if (GetValType() == typeof(Json))
                    (Value as Json)[key] = value;
            }
        }

        public object this[int index]
        {
            get
            {
                if (GetValType() == typeof(Json))
                    return (Value as Json)[index];
                else if (GetValType() == typeof(JsonObjectArray))
                    return (Value as JsonObjectArray)[index];

                throw new Exception($"Cannot get value by index \"{index}\".");
            }
            set
            {
                if (GetValType() == typeof(Json)) {
                    if (value.GetType() != typeof(JsonObject))
                        (Value as Json)[index] = value as JsonObject;
                    else
                        throw new Exception($"The value cannot be set by index \"{index}\" because this value is not a JsonObject.");
                } else if (GetValType() == typeof(JsonObjectArray))
                    (Value as JsonObjectArray)[index] = value;
                else
                    throw new Exception($"Cannot set value by index \"{index}\".");
            }
        }
        #endregion

        public override string ToString()
        {
            return $"\"{Key}\": {Json.ValueToJsonString(Value)}";
        }

        public string ToFormatString(int spacing = 4, int gen = 1)
        {
            return $"{new string(' ', spacing * (gen-1))}\"{Key}\": {Json.ValueToFormatJsonString(Value, spacing, gen)}";
        }

        public object Clone()
        {
            return new JsonObject(this.Key, this.Value);
        }
    }

    public class JsonObjectArray : List<object>, ICloneable
    {
        private int currentposition = 0;
        private TokenCollection tokens;

        #region JsonObjectArray
        public JsonObjectArray()
        {
        }

        public JsonObjectArray(string json) : this(new JsonLexer().Tokenize(json))
        {
        }

        public JsonObjectArray(IEnumerable<object> json) : base(json)
        {
        }

        public JsonObjectArray(List<object> json) : base(json)
        {
        }

        public JsonObjectArray(object[] json) : base(json)
        {
        }

        public static JsonObjectArray FromAnonymous(dynamic json)
        {
            return new JsonObjectArray(AnonymousConverter.ConvertArray(json) as List<object>);
        }

        public static JsonObjectArray FromArray(Array array, bool usePrivateFields, string[] exclusion_fields = null)
        {
            return new JsonObjectArray(StructureConverter.ConvertArrayFrom(array, usePrivateFields, exclusion_fields));
        }

        public static T[] ToArray<T>(JsonObjectArray array)
        {
            return StructureConverter.ConvertArrayTo<T>(array);
        }
        #endregion

        public JsonObjectArray(TokenCollection json)
        {
            tokens = json;

            if (tokens[0].Kind == SyntaxKind.OB && tokens[tokens.Count - 1].Kind == SyntaxKind.CB)
                throw new Exception("Use Json(string json).");
            else if (tokens[0].Kind == SyntaxKind.OBA && tokens[tokens.Count - 1].Kind == SyntaxKind.CBA)
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
                    case SyntaxKind.SEPARATOR:
                        break;
                    case SyntaxKind.OB:
                        Add(new Json(GetItem()));
                        break;
                    case SyntaxKind.OBA:
                        Add(new JsonObjectArray(GetItem()));
                        break;
                    default:
                        Add(elem.Value);
                        break;
                }
            }
        }

        private TokenCollection GetItem()
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

        public override string ToString()
        {
            return $"[{string.Join(", ", this.Select(o => Json.ValueToJsonString(o)))}]";
        }

        public string ToFormatString(int spacing = 4, int gen = 1)
        {
            if (Count == 0)
                return "[]";
            else
                return $"[" + "\r\n" +
                           $"{string.Join(", " + "\r\n", this.Select(o => new string(' ', spacing * gen) + Json.ValueToFormatJsonString(o, spacing, gen + 1)))}" +
                       "\r\n" + $"{new string(' ', spacing * (gen - 1))}]";
        }

        public object Clone()
        {
            return new JsonObjectArray(this);
        }
    }
}
