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
    public abstract class ObjectBase : DynamicObject, ICloneable
    {
        public object Value { get; set; }
        
        public dynamic GetDynamic() =>
            this;

        #region Dynamic override
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            //result = null;

            //var name = binder.Name;
            //if (name[0] == '_' && name[1] == '_')
            //    name = name.Substring(1);
            //else if (name[0] == '_' && name[1] != '_')
            //{
            //    name = name.Substring(1);
            //    var parts = name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            //    switch (parts[0])
            //    {
            //        case "Get":
            //            if (parts.Length < 2)
            //                return false;
            //            var typestring = string.Join(".", parts, 1, parts.Length - 1);
            //            var type = Type.GetType(typestring);
            //            if (type is null)
            //            {
            //                type = Type.GetType("PinkJson.Parser." + typestring);
            //                if (type is null)
            //                {
            //                    type = Type.GetType("System." + typestring);
            //                    if (type is null)
            //                        return false;
            //                }
            //            }
            //            result = Convert.ChangeType(Value, type);
            //            break;
            //        case "Key":
            //            result = Get<JsonObject>().Key;
            //            break;
            //        case "Value":
            //            result = Value;
            //            break;
            //        case "ToString":
            //            result = Value.ToString();
            //            break;
            //        case "ToFormatString":
            //            result = Get<ObjectBase>().ToFormatString();
            //            break;
            //        default:
            //            if (parts.Length == 1 && int.TryParse(parts[0], out int ind))
            //                result = this[ind];
            //            else
            //                return false;
            //            break;
            //    }
            //}
            //else

            var name = binder.Name;
            //if (name[0] == '_' && name[1] == '_')
                //name = name.Substring(1);
            if (name[0] == '_' && char.IsDigit(name[1]))//&& name[1] != '_')
            {
                var parts = name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1 && int.TryParse(parts[0], out int ind))
                {
                    result = this[ind];
                    return true;
                }
            }

            if ((result = this[name]) != null)
                return true;
            else
            {
                var field = typeof(GetMemberBinder).GetRuntimeFields().Single(fi => fi.Name == "_name");
                field.SetValue(binder, name);
                return base.TryGetMember(binder, out result);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name;
            //if (name[0] == '_' && name[1] == '_')
                //name = name.Substring(1);
            if (name[0] == '_' && char.IsDigit(name[1]))//&& name[1] != '_')
            {
                var parts = name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1 && int.TryParse(parts[0], out int ind))
                {
                    this[ind] = (ObjectBase)value;
                    return true;
                }
            }

            if (this[name] != null)
            {
                this[name] = (JsonObject)value;
                return true;
            }
            else
            {
                var field = typeof(GetMemberBinder).GetRuntimeFields().Single(fi => fi.Name == "_name");
                field.SetValue(binder, name);
                return base.TrySetMember(binder, value);
            }
        }
        #endregion

        #region Abstract
        public abstract JsonObject ElementByKey(string key);

        /// <summary>
        /// Gets the index of element at the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int IndexByKey(string key);

        /// <summary>
        /// Deletes an element by the specified key
        /// </summary>
        /// <param name="key"></param>
        public abstract void RemoveByKey(string key);

        public override abstract string ToString();

        public abstract string ToFormatString(int spacing = 4, int gen = 1);

        public abstract object Clone();
        #endregion

        #region Value Methods
        public T Get<T>()
        {
            if (Value is null)
                return default;
            return (T)Value;
        }

        public T Get<T>(int index)
        {
            return (T)this[index].Value;
        }

        public Type GetValType()
        {
            return Value.GetType();
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

        public ObjectBase this[int index]
        {
            get
            {
                if (GetValType() == typeof(Json))
                    return (Value as Json)[index];
                else if (GetValType() == typeof(JsonArray))
                    return (Value as JsonArray)[index];

                return null;
                //throw new Exception($"Cannot get value by index \"{index}\".");
            }
            set
            {
                if (GetValType() == typeof(Json))
                {
                    if (value.GetType() == typeof(JsonObject))
                        (Value as Json)[index] = value as JsonObject;
                    else
                        throw new Exception($"The value cannot be set by index \"{index}\" because this value is not a JsonObject.");
                }
                else if (GetValType() == typeof(JsonArray))
                    (Value as JsonArray)[index] = new JsonArrayObject(value);
                else
                    throw new Exception($"Cannot set value by index \"{index}\".");
            }
        }
        #endregion
    }
}
