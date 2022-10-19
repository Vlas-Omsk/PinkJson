using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PinkJson2.Runtime
{
    internal sealed class MemberAccessor
    {
        private Func<object, object> _getter;
        private Action<object, object> _setter;

        public MemberAccessor(MemberInfo memberInfo)
        {
            MemberInfo = memberInfo;
        }

        public MemberInfo MemberInfo { get; }

        public object GetValue(object obj)
        {
            if (_getter == null)
                CompileGetter();

            return _getter.Invoke(obj);
        }

        public void SetValue(object obj, object value)
        {
            if (_setter == null)
                CompileSetter();

            _setter.Invoke(obj, value);
        }

        private void CompileGetter()
        {
            var targetType = MemberInfo.DeclaringType;

            var exInstance = Expression.Parameter(typeof(object), "instance");

            var exConvertedInstance = Expression.Convert(exInstance, targetType);
            var exMemberAccess = Expression.MakeMemberAccess(exConvertedInstance, MemberInfo);
            var exConvertedResult = Expression.Convert(exMemberAccess, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(exConvertedResult, exInstance);

            _getter = lambda.Compile();
        }

        private void CompileSetter()
        {
            var targetType = MemberInfo.DeclaringType;

            var exInstance = Expression.Parameter(typeof(object), "instance");
            var exValue = Expression.Parameter(typeof(object), "value");

            var exConvertedInstance = Expression.Convert(exInstance, targetType);
            var exMemberAccess = Expression.MakeMemberAccess(exConvertedInstance, MemberInfo);
            var exConvertedValue = Expression.Convert(exValue, GetUnderlyingType(MemberInfo));
            var exAssign = Expression.Assign(exMemberAccess, exConvertedValue);

            var lambda = Expression.Lambda<Action<object, object>>(exAssign, exInstance, exValue);
            
            _setter = lambda.Compile();
        }

        private static Type GetUnderlyingType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException("Input MemberInfo must be of type EventInfo, FieldInfo, MethodInfo or PropertyInfo");
            }
        }
    }
}
