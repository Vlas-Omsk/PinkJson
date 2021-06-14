using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace PinkJson
{
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
