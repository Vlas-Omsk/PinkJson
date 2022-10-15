using System;
using System.Reflection;

namespace PinkJson2
{
    public static class MemberInfoExtension
    {
        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, out T attribute) where T : Attribute
        {
            attribute = memberInfo.GetCustomAttribute<T>();
            return attribute != null;
        }

        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, out T attribute, bool inherit) where T : Attribute
        {
            attribute = memberInfo.GetCustomAttribute<T>(inherit);
            return attribute != null;
        }
    }
}
