using System;

namespace PinkJson2
{
    public static class TypeHelper
    {
        public static object ChangeType(Type type, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var valueType = value.GetType();

            if (valueType == type || type.IsAssignableFrom(valueType))
                return value;

            if (type == typeof(DateTime))
            {
                if (value is string @string)
                    return DateTime.Parse(@string);
                else if (value is long @long)
                    return DateTime.FromBinary(@long);
                else
                    throw new Exception($"Can't convert value of type {value.GetType()} to {type}");
            }
            if (type.IsEnum)
                return Enum.Parse(type, (string)value);

            return Convert.ChangeType(value, type);
        }
    }
}
