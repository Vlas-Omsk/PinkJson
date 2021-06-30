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
            if (obj is null)
                return null;
            if (TryGetAsArray(obj, out _))
                throw new Exception("Use JsonArray.FromArray(Array array).");
            //if (!IsStructureType(structure.GetType()))
            //    throw new Exception("Unknown Structure format.");

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

            return fields.Select(field =>
            {
                var jsonPropertyAttribute = field.GetCustomAttribute(typeof(JsonProperty), true) as JsonProperty;
                var name = jsonPropertyAttribute == null ? field.Name : jsonPropertyAttribute.PropertyName;

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
                var jsonPropertyAttribute = field.GetCustomAttribute(typeof(JsonProperty), true) as JsonProperty;
                var fieldName = jsonPropertyAttribute == null ? field.Name : jsonPropertyAttribute.PropertyName;

                if (json.IndexByKey(fieldName) == -1)
                    return;

                object value = json[fieldName].Value;
                var fieldType = jsonPropertyAttribute?.TargetType ?? field.GetFieldType();

                if (value is Json && fieldType != typeof(Json))
                {
                    //var fieldset = FormatterServices.GetUninitializedObject(fieldType);
                    value = ConvertTo(value as Json, fieldType);
                }
                else if (value is JsonArray && fieldType != typeof(JsonArray))
                {
                    var currentType = fieldType;
                    while (currentType != typeof(object))
                    {
                        if (currentType.IsGenericType)
                            break;
                        currentType = currentType.BaseType;
                    }

                    if (currentType.GetInterface("IList") != null)
                    {
                        if (currentType != fieldType)
                            value = ConvertListTo(value as JsonArray, fieldType);
                        else
                            value = ConvertArrayTo(value as JsonArray, currentType.GetGenericArguments().Single(), true);
                    }
                    else
                        value = ConvertArrayTo(value as JsonArray, fieldType.GetElementType());
                }

                field.SetValue(result, ConvertValue(value, fieldType));
            });

            return result;
        }

        private static object ConvertArrayTo(JsonArray json, Type elemType, bool asList = false)
        {
            Array list = Array.CreateInstance(elemType, json.Count);

            for (var i = 0; i < json.Count; i++)
            {
                var elem = json[i];
                if (elem.Value is Json)
                    list.SetValue(ConvertTo(elem.Get<Json>(), elemType), i);
                else
                    list.SetValue(ConvertValue(elem.Value, elemType), i);
            }

            if (asList)
            {
                Type genericListType = typeof(List<>);
                Type concreteListType = genericListType.MakeGenericType(elemType);

                return Activator.CreateInstance(concreteListType, new object[] { list });
            }

            return list;
        }

        private static object ConvertListTo(JsonArray json, Type type)
        {
            var list = (IList)Activator.CreateInstance(type);
            for (var i = 0; i < json.Count; i++)
            {
                var elem = json[i];
                list.Add(ConvertValue(elem.Value, type));
            }
            return list;
        }

        private static bool TryGetAsArray(object value, out Array array)
        {
            array = null;

            if (value is Array)
                array = value as Array;
            else if (value is IList)
                array = (value as IList).OfType<object>().ToArray();

            return array != null;
        }

        private static object ConvertValue(object value, Type type)
        {
            if (type == typeof(DateTime))
                return DateTime.Parse(value.ToString());
            else if (type == typeof(TimeSpan))
                return TimeSpan.Parse(value.ToString());
            else
                return value;
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
}
