using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace PinkJson2
{
    public sealed class JsonMetaObject : DynamicMetaObject
    {
        private static readonly MethodInfo _getValueMethodInfo;
        private static readonly MethodInfo _setValueMethodInfo;

        static JsonMetaObject()
        {
            var type = typeof(IDynamicJson);
            _getValueMethodInfo = type.GetMethod(nameof(IDynamicJson.DynamicGetValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            _setValueMethodInfo = type.GetMethod(nameof(IDynamicJson.DynamicSetValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        internal JsonMetaObject(Expression parameter, IDynamicJson value) : base(parameter, BindingRestrictions.Empty, value)
        {
        }

        public new IDynamicJson Value => (IDynamicJson)base.Value;

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var arguments = new Expression[]
            {
                Expression.Constant(this),
                Expression.Constant(binder.Name)
            };

            var objectExpression = Expression.Call(Expression.Convert(Expression, LimitType), _getValueMethodInfo, arguments);

            return new DynamicMetaObject(objectExpression, BindingRestrictions.GetTypeRestriction(Expression, RuntimeType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var arguments = new Expression[]
            {
                Expression.Constant(this),
                Expression.Constant(binder.Name),
                Expression.Constant(value.Value)
            };

            var objectExpression = Expression.Call(Expression.Convert(Expression, LimitType), _setValueMethodInfo, arguments);

            return new DynamicMetaObject(objectExpression, BindingRestrictions.GetTypeRestriction(Expression, RuntimeType));
        }

        internal object GetValue(string propertyName)
        {
            object member;
            if ((member = typeof(IJson).GetProperty(propertyName)) != null)
                return ((PropertyInfo)member).GetValue(Value);
            if ((member = typeof(IJson).GetField(propertyName)) != null)
                return ((FieldInfo)member).GetValue(Value);

            if (propertyName[0] == '_' && propertyName.Length > 1 && propertyName[1] != '_')
            {
                var index = int.Parse(propertyName.Substring(1));
                return Value[index];
            }
            return Value[propertyName];
        }

        internal object SetValue(string propertyName, object value)
        {
            object member;
            if ((member = typeof(IJson).GetProperty(propertyName)) != null)
            {
                ((PropertyInfo)member).SetValue(Value, value);
                return value;
            }
            if ((member = typeof(IJson).GetField(propertyName)) != null)
            {
                ((FieldInfo)member).SetValue(Value, value);
                return value;
            }

            if (!(value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            if (propertyName[0] == '_' && propertyName.Length > 1 && propertyName[1] != '_')
            {
                var index = int.Parse(propertyName.Substring(1));
                Value[index] = (IJson)value;
                return value;
            }
            Value[propertyName] = (IJson)value;
            return value;
        }
    }
}
