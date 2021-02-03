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
    public class JsonObject : ObjectBase
    {
        public string Key { get; set; }

        public JsonObject(string key, object value)
        {
            Key = key;
            Value = value;
        }

        #region Override
        public override JsonObject ElementByKey(string key)
        {
            if (GetValType() == typeof(Json))
                return Get<Json>().ElementByKey(key);
            return null;
        }

        public override int IndexByKey(string key)
        {
            if (GetValType() == typeof(Json))
                return Get<Json>().IndexByKey(key);
            return -1;
        }

        public override void RemoveByKey(string key)
        {
            if (GetValType() == typeof(Json))
            {
                Get<Json>().RemoveByKey(key);
                return;
            }
            throw new InvalidTypeException("Value type is not Json");
        }

        public override string ToString()
        {
            return $"\"{Key.EscapeString()}\": {Json.ValueToJsonString(Value)}";
        }

        public override string ToFormatString(int spacing = 4, int gen = 1)
        {
            return $"{new string(' ', spacing * (gen - 1))}\"{Key.EscapeString()}\": {Json.ValueToFormatJsonString(Value, spacing, gen)}";
        }

        public override object Clone()
        {
            return new JsonObject(this.Key, this.Value);
        }
        #endregion
    }
}
