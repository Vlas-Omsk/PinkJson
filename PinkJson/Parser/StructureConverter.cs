using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PinkJson.Parser
{
    public class StructureConverter
    {
        public static List<JsonObject> ConvertFrom(object structure, bool usePrivateFields, string[] exclusion_fields = null)
        {
            if (structure is Array)
                throw new Exception("Use JsonObjectArray.FromArray(Array array).");
            if (!IsStructureType(structure.GetType()))
                throw new Exception("Unknown Structure format.");

            var structType = structure.GetType();
            List<FieldInfo> fields;
            if (usePrivateFields)
                fields = structType.GetRuntimeFields().ToList();
            else
                fields = structType.GetFields().ToList();

            return fields.Select<FieldInfo, JsonObject>(field =>
            {
                string name = field.Name;
                if (!(exclusion_fields is null) && exclusion_fields.Contains(name))
                    return null;

                object value = field.GetValue(structure);

                if (value is Array)
                    value = JsonObjectArray.FromArray(value as Array, usePrivateFields, exclusion_fields);
                else if (IsStructureType(field.FieldType))
                    value = Json.FromStructure(value, usePrivateFields, exclusion_fields);

                return new JsonObject(name, value);
            }).OfType<JsonObject>().ToList();
        }

        public static List<object> ConvertArrayFrom(Array array, bool usePrivateFields, string[] exclusion_fields = null)
        {
            List<object> list = new List<object>();
            foreach (var elem in array)
            {
                var type = elem.GetType();
                if (IsStructureType(type))
                    list.Add(Json.FromStructure(elem, usePrivateFields, exclusion_fields));
                else
                    list.Add(elem);
            }

            return list;
        }

        public static T ConvertTo<T>(Json json)
        {
            return (T)ConvertTo(json, typeof(T));
        }

        public static T[] ConvertArrayTo<T>(JsonObjectArray json)
        {
            var test = ConvertArrayTo(json, typeof(T));
            return (T[])test;
        }

        private static object ConvertTo(Json json, Type structType)
        {
            if (!IsStructureType(structType))
                throw new Exception("Unknown Structure format.");

            var result = FormatterServices.GetUninitializedObject(structType);
            var fields = structType.GetRuntimeFields().ToList();

            fields.ForEach(field =>
            {
                if (json.IndexByKey(field.Name) == -1)
                    return;

                object value = json[field.Name].Value;

                if (value is Json)
                {
                    var fieldset = FormatterServices.GetUninitializedObject(field.FieldType);
                    ConvertTo(value as Json, field.FieldType);
                }
                else if (value is JsonObjectArray)
                {
                    value = ConvertArrayTo(value as JsonObjectArray, field.FieldType.GetElementType());
                }

                field.SetValue(result, value);
            });

            return result;
        }

        private static object ConvertArrayTo(JsonObjectArray json, Type elemType)
        {
            Array list = Array.CreateInstance(elemType, json.Count);

            for (var i = 0; i < json.Count; i++)
            {
                var elem = json[i];
                if (elem is Json)
                    list.SetValue(ConvertTo(elem as Json, elemType), i);
                else
                    list.SetValue(elem, i);
            }

            return list;
        }

        public static bool IsStructureType(Type type)
        {
            return type.Attributes.HasFlag(TypeAttributes.SequentialLayout)
                && type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }
    }
}
