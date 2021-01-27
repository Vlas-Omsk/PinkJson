﻿using PinkJson.Lexer;
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
    public class JsonArray : JsonBase<JsonArrayObject>
    {
        private int currentposition = 0;
        private TokenCollection tokens;

        public override object Value { get => new JsonArray(Collection); set => Collection = ((JsonArray)value).Collection; }

        #region Constructors
        public JsonArray()
        {
        }

        public JsonArray(string json) : this(new JsonLexer().Tokenize(json))
        {
        }

        public JsonArray(IEnumerable<object> json) : base(json?.Select(obj => new JsonArrayObject(obj)))
        {
        }

        public JsonArray(List<object> json) : this(json as IEnumerable<object>)
        {
        }

        public JsonArray(object[] json) : this(json as IEnumerable<object>)
        {
        }

        public static JsonArray FromAnonymous(dynamic json)
        {
            return new JsonArray(AnonymousConverter.ConvertArray(json) as List<object>);
        }

        public static JsonArray FromArray(Array array, bool usePrivateFields, string[] exclusion_fields = null)
        {
            return new JsonArray(StructureConverter.ConvertArrayFrom(array, usePrivateFields, exclusion_fields));
        }

        public static T[] ToArray<T>(JsonArray array)
        {
            return StructureConverter.ConvertArrayTo<T>(array);
        }
        #endregion

        public void Add(object item)
        {
            base.Add(new JsonArrayObject(item));
        }

        #region Parser
        public JsonArray(TokenCollection json)
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
                        Add(new JsonArray(GetItem()));
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
        #endregion

        #region Override
        public override string ToString()
        {
            return $"[{string.Join(", ", this.Select(o => Json.ValueToJsonString(o)))}]";
        }

        public override string ToFormatString(int spacing = 4, int gen = 1)
        {
            if (Count == 0)
                return "[]";
            else
                return $"[" + "\r\n" +
                           $"{string.Join(", " + "\r\n", this.Select(o => new string(' ', spacing * gen) + Json.ValueToFormatJsonString(o, spacing, gen + 1)))}" +
                       "\r\n" + $"{new string(' ', spacing * (gen - 1))}]";
        }

        public override object Clone()
        {
            return new JsonArray(this);
        }
        #endregion
    }
}
