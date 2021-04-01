using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections;

namespace PinkJson
{
    public class JsonConverter
    {
        public static List<JsonObject> ConvertFrom(object obj, bool usePrivateFields, string[] exclusion_fields = null)
        {
            if (TryGetAsArray(obj, out _))
                throw new Exception("Use JsonArray.FromArray(Array array).");
            //if (!IsStructureType(structure.GetType()))
            //    throw new Exception("Unknown Structure format.");
            if (obj is null)
                return null;

            var structType = obj.GetType();
            List<MemberInfo> fields;
            if (usePrivateFields)
            {
                fields = structType.GetRuntimeFields().ToList<MemberInfo>();
                fields.AddRange(structType.GetRuntimeProperties().ToList<MemberInfo>());
            }
            else
            {
                fields = structType.GetFields().ToList<MemberInfo>();
                fields.AddRange(structType.GetProperties().ToList<MemberInfo>());
            }

            fields = fields.Where(m => !m.IsStatic()).ToList();

            return fields.Select<MemberInfo, JsonObject>(field =>
            {
                string name = field.Name;
                if (!(exclusion_fields is null) && exclusion_fields.Contains(name))
                    return null;

                object value = field.GetValue(obj);

                if (TryGetAsArray(value, out Array arr))
                    value = JsonArray.FromArray(arr, usePrivateFields, exclusion_fields);
                else if (IsStructureOrSpecificClassType(field.GetFieldType()))
                    value = Json.FromObject(value, usePrivateFields, exclusion_fields);

                return new JsonObject(name, value);
            }).OfType<JsonObject>().ToList();
        }

        public static List<object> ConvertArrayFrom(Array array, bool usePrivateFields, string[] exclusion_fields = null)
        {
            List<object> list = new List<object>();
            if (array is null)
                return list;
            foreach (var elem in array)
            {
                var type = elem.GetType();
                if (TryGetAsArray(elem, out Array arr))
                    list.Add(JsonArray.FromArray(arr, usePrivateFields, exclusion_fields));
                else if (IsStructureOrSpecificClassType(type))
                    list.Add(Json.FromObject(elem, usePrivateFields, exclusion_fields));
                else
                    list.Add(elem);
            }

            return list;
        }

        public static T ConvertTo<T>(Json json)
        {
            return (T)ConvertTo(json, typeof(T));
        }

        public static T[] ConvertArrayTo<T>(JsonArray json)
        {
            return (T[])ConvertArrayTo(json, typeof(T));
        }

        private static object ConvertTo(Json json, Type structType)
        {
            if (!IsStructureOrSpecificClassType(structType))
                throw new Exception("Unknown Structure format.");

            var result = FormatterServices.GetUninitializedObject(structType);
            List<MemberInfo> fields;

            fields = structType.GetRuntimeFields().ToList<MemberInfo>();
            fields.AddRange(structType.GetRuntimeProperties().ToList<MemberInfo>());

            fields = fields.Where(m => !m.IsStatic()).ToList();

            fields.ForEach(field =>
            {
                if (json.IndexByKey(field.Name) == -1)
                    return;

                object value = json[field.Name].Value;

                if (value is Json)
                {
                    var fieldset = FormatterServices.GetUninitializedObject(field.GetFieldType());
                    value = ConvertTo(value as Json, field.GetFieldType());
                }
                else if (value is JsonArray)
                {
                    value = ConvertArrayTo(value as JsonArray, field.GetFieldType().GetElementType());
                }

                field.SetValue(result, value);
            });

            return result;
        }

        private static object ConvertArrayTo(JsonArray json, Type elemType)
        {
            Array list = Array.CreateInstance(elemType, json.Count);

            for (var i = 0; i < json.Count; i++)
            {
                var elem = json[i];
                if (elem.Value is Json)
                    list.SetValue(ConvertTo(elem.Get<Json>(), elemType), i);
                else
                    list.SetValue(elem.Value, i);
            }

            return list;
        }

        public static bool TryGetAsArray(object value, out Array array)
        {
            array = null;

            if (value is Array)
                array = value as Array;
            else if (value is IList)
                array = (value as IList).OfType<object>().ToArray();

            return array != null;
        }

        public static bool IsStructureOrSpecificClassType(Type type)
        {
            return IsStructureType(type) || IsSpecificClassType(type);
        }

        private static bool IsStructureType(Type type)
        {
            return type.Attributes.HasFlag(TypeAttributes.SequentialLayout)
                && type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }

        private static bool IsSpecificClassType(Type type)
        {
            return type.IsClass && !type.IsValueType && !type.IsGenericType && type != typeof(string);
        }
    }

    public static class MemberInfoExtension
    {
        internal static object GetValue(this MemberInfo member, object obj)
        {
            if (member is FieldInfo)
                return (member as FieldInfo).GetValue(obj);
            else if (member is PropertyInfo)
                return (member as PropertyInfo).GetValue(obj);
            else
                return null;
        }

        internal static void SetValue(this MemberInfo member, object obj, object value)
        {
            try
            {
                if (member is FieldInfo)
                    (member as FieldInfo).SetValue(obj, value);
                else if (member is PropertyInfo)
                    (member as PropertyInfo).SetValue(obj, value);
            }
            catch { }
        }

        internal static Type GetFieldType(this MemberInfo member)
        {
            if (member is FieldInfo)
                return (member as FieldInfo).FieldType;
            else if (member is PropertyInfo)
                return (member as PropertyInfo).PropertyType;
            else
                return null;
        }

        internal static bool IsStatic(this MemberInfo member)
        {
            if (member is FieldInfo)
                return (member as FieldInfo).Attributes.HasFlag(FieldAttributes.Static);
            else if (member is PropertyInfo)
                return false;
            else
                return false;
        }
    }
}
