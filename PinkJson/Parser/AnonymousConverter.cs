using PinkJson.Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace PinkJson.Parser
{
    public class AnonymousConverter
    {
        public static List<JsonObject> Convert(dynamic json)
        {
            if (json is Array)
                throw new Exception("Use JsonObjectArray(dynamic json).");
            if (!IsAnonymousType(json.GetType()))
                throw new Exception("Unknown Anonymous format.");

            if (IsEmptyAnonymousType(json.GetType()))
                return new List<JsonObject>();

            Type type = json.GetType();
            var genericType = type.GetGenericTypeDefinition();
            var parameterTypes = genericType.GetConstructors()[0]
                                            .GetParameters()
                                            .Select(p => p.ParameterType)
                                            .ToList();
            var propertyNames = genericType.GetProperties()
                                           .OrderBy(p => parameterTypes.IndexOf(p.PropertyType))
                                           .Select(p => p.Name);

            return propertyNames.Select<string, JsonObject>(name =>
            {
                dynamic value = type.GetProperty(name).GetValue(json, null);
                if (IsAnonymousType(value.GetType()))
                    value = Json.FromAnonymous(value);
                else if (value is Array)
                    value = JsonObjectArray.FromAnonymous(value);

                return new JsonObject(name, value);
            }).OfType<JsonObject>().ToList();
        }

        public static List<object> ConvertArray(dynamic json)
        {
            if (IsAnonymousType(json.GetType()))
                throw new Exception("Use Json(dynamic json).");
            if (!(json is Array))
                throw new Exception("Unknown Anonymous format.");

            List<object> list = new List<object>();
            foreach (var elem in json)
                if (IsAnonymousType(elem.GetType()))
                    list.Add(Json.FromAnonymous(elem));
                else
                    list.Add(elem);

            return list;
        }

        public static bool IsAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && (type.IsGenericType || IsEmptyAnonymousType(type)) && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsEmptyAnonymousType(Type type)
        {
            var name = type.Name;
            while (char.IsDigit(name[name.Length - 1]))
                name = name.Substring(0, name.Length - 1);

            return name == "<>f__AnonymousType";
        }
    }
}
