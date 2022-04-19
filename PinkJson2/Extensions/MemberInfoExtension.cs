using System;
using System.Reflection;

namespace PinkJson2
{
    public static class MemberInfoExtension
    {
        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, out T jsonPropertyAttribute) where T : Attribute
        {
            jsonPropertyAttribute = memberInfo.GetCustomAttribute<T>();
            return jsonPropertyAttribute != null;
        }

        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, out T jsonPropertyAttribute, bool inherit) where T : Attribute
        {
            jsonPropertyAttribute = memberInfo.GetCustomAttribute<T>(inherit);
            return jsonPropertyAttribute != null;
        }
    }
}
