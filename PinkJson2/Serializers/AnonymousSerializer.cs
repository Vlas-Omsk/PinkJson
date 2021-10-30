using System;
using System.Collections;
using System.Linq;

namespace PinkJson2.Serializers
{
    [Obsolete("Use " + nameof(ObjectConverter) + " instead")]
    public sealed class AnonymousSerializer : ISerializer
    {
        public AnonymousSerializer()
        {
        }

        public IJson Serialize(object obj)
        {
            if (!HelperSerializer.IsAnonymousType(obj.GetType()))
                throw new InvalidObjectTypeException("AnonymousType");

            return SerializeObject(obj);
        }

        private object SerializeValue(object value, Type valueType)
        {
            if (value != null)
            {
                if (HelperSerializer.IsAnonymousType(valueType))
                    value = SerializeObject(value);
                else if (HelperSerializer.IsArray(valueType))
                    value = SerializeArray((IEnumerable)value);
            }

            return value;
        }

        private JsonObject SerializeObject(object obj)
        {
            var type = obj.GetType();
            var jsonObject = new JsonObject();

            if (HelperSerializer.IsEmptyAnonymousType(type))
                return jsonObject;

            var genericType = type.GetGenericTypeDefinition();
            var parameterTypes = genericType.GetConstructors()[0].GetParameters().Select(p => p.ParameterType).ToArray();
            var propertyNames = genericType.GetProperties().OrderBy(p => Array.IndexOf(parameterTypes, p.PropertyType)).Select(p => p.Name);

            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName);
                jsonObject.AddLast(
                    new JsonKeyValue(
                        propertyName,
                        SerializeValue(property.GetValue(obj), property.PropertyType)
                    )
                );
            }

            return jsonObject;
        }

        private JsonArray SerializeArray(IEnumerable enumerable)
        {
            var jsonArray = new JsonArray();

            foreach (var item in enumerable)
                jsonArray.AddLast(new JsonArrayValue(SerializeValue(item, item.GetType())));

            return jsonArray;
        }
    }
}
