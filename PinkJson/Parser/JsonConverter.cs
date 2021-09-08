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

            fields = fields.Where(m => !m.IsStatic() && m.Name != "Item").ToList();

            return fields.Select(field =>
            {
                var name = field.Name;
                if (field.GetCustomAttribute(typeof(JsonProperty), true) is JsonProperty jsonPropertyAttribute)
                {
                    if (jsonPropertyAttribute.Ignore)
                        return null;
                    if (!string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
                        name = jsonPropertyAttribute.PropertyName;
                }

                if (!(exclusion_fields is null) && exclusion_fields.Contains(name))
                    return null;

                object value = field.GetValue(obj);
                var valtype = field.GetFieldType();
                if (value != null && valtype == typeof(object))
                    valtype = value.GetType();

                if (TryGetAsArray(value, out Array arr))
                    value = JsonArray.FromArray(arr, usePrivateFields, exclusion_fields);
                else if (IsStructureOrSpecificClassType(valtype))
                    value = Json.FromObject(value, usePrivateFields, exclusion_fields);

                return new JsonObject(name, value);
            }).OfType<JsonObject>().ToList();
        }

        public static List<object> ConvertArrayFrom(IEnumerable array, bool usePrivateFields, string[] exclusion_fields = null)
        {
            var list = new List<object>();
            if (array is null)
                return list;
            foreach (var elem in array)
            {
                var value = elem;
                if (value != null)
                {
                    var type = value.GetType();
                    if (TryGetAsArray(value, out Array arr))
                        value = JsonArray.FromArray(arr, usePrivateFields, exclusion_fields);
                    else if (IsStructureOrSpecificClassType(type))
                        value = Json.FromObject(value, usePrivateFields, exclusion_fields);
                }
                list.Add(value);
            }

            return list;
        }

        public static T ConvertTo<T>(Json json, Func<T> initializator = null)
        {
            Func<object> initializator2 = null;
            if (initializator != null)
                initializator2 = () => initializator.Invoke();
            return (T)ConvertTo(json, typeof(T), initializator2);
        }

        public static T[] ConvertArrayTo<T>(JsonArray json)
        {
            return (T[])ConvertArrayTo(json, typeof(T));
        }

        public static object ConvertTo(Json json, Type type, Func<object> initializator = null)
        {
            if (!IsStructureOrSpecificClassType(type))
                throw new Exception("Unknown Structure format.");

            //var result = FormatterServices.GetUninitializedObject(structType);
            //var cctors = structType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //var result = cctors[0].Invoke(null);
            object result = CreateInstance(type, initializator);
            
            if (type.GetInterface(nameof(IJsonConvertTo)) != null)
                ((IJsonConvertTo)result).ConvertTo(json);
            else
            {
                List<MemberInfo> fields;

                fields = type.GetRuntimeFields().ToList<MemberInfo>();
                fields.AddRange(type.GetRuntimeProperties().ToList<MemberInfo>());

                fields = fields.Where(m => !m.IsStatic()).ToList();

                fields.ForEach(field =>
                {
                    var jsonPropertyAttribute = field.GetCustomAttribute(typeof(JsonProperty), true) as JsonProperty;
                    var fieldName = field.Name;
                    if (jsonPropertyAttribute != null)
                    {
                        if (jsonPropertyAttribute.Ignore)
                            return;
                        if (!string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
                            fieldName = jsonPropertyAttribute.PropertyName;
                    }

                    if (json.IndexByKey(fieldName) == -1)
                        return;

                    object value = null;
                    var fieldType = jsonPropertyAttribute?.TargetType ?? field.GetFieldType();
                    value = json[fieldName].Value;

                    var currentType = fieldType;
                    var isJsonObjectBaseFieldType = false;
                    while (currentType != typeof(object))
                    {
                        if (currentType == typeof(ObjectBase))
                        {
                            isJsonObjectBaseFieldType = true;
                            break;
                        }
                        currentType = currentType.BaseType;
                    }
                    if (!isJsonObjectBaseFieldType)
                    {
                        if (value is Json)
                        {
                            //var fieldset = FormatterServices.GetUninitializedObject(fieldType);
                            value = ConvertTo(value as Json, fieldType);
                        }
                        else if (value is JsonArray)
                        {
                            currentType = fieldType;
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
                        value = ConvertValue(value, fieldType);
                    }
                    field.SetValue(result, value);
                });
            }

            return result;
        }

        public static object ConvertArrayTo(JsonArray json, Type elemType, bool asList = false)
        {
            Array list = Array.CreateInstance(elemType, json.Count);

            for (var i = 0; i < json.Count; i++)
            {
                var elem = json[i];
                object value;
                if (elem.Value is Json)
                    value = ConvertTo(elem.Get<Json>(), elemType);
                else if (elemType.GetInterface(nameof(IJsonConvertTo)) != null)
                {
                    value = CreateInstance(elemType);
                    ((IJsonConvertTo)value).ConvertTo(elem);
                }
                else
                    value = ConvertValue(elem.Value, elemType);
                list.SetValue(value, i);
            }

            if (asList)
            {
                Type genericListType = typeof(List<>);
                Type concreteListType = genericListType.MakeGenericType(elemType);

                return Activator.CreateInstance(concreteListType, new object[] { list });
            }

            return list;
        }

        private static object CreateInstance(Type type, Func<object> initializator = null)
        {
            object result;
            if (initializator == null)
            {
                var cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (cctor == null)
                    result = FormatterServices.GetUninitializedObject(type);
                else
                    result = cctor.Invoke(null);
            }
            else
                result = initializator();
            return result;
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
            else if (value != null && value.GetType() != type)
                return Convert.ChangeType(value, type);
            else
                return value;
        }

        public static bool IsStructureOrSpecificClassType(Type type)
        {
            return IsStructureType(type) || IsSpecificClassType(type);
        }

        public static bool IsStructureType(Type type)
        {
            return type.Attributes.HasFlag(TypeAttributes.SequentialLayout)
                && type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }

        public static bool IsSpecificClassType(Type type)
        {
            return type.IsClass && !type.IsValueType && /*!type.IsGenericType && */type != typeof(string);
        }
    }
}
