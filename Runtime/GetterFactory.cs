using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ScopeRuntimeMonitoring
{
    public static class GetterFactory
    {
        public static Func<object, object> CreateGetter(FieldInfo fieldInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "target");
            var castInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
            var fieldAccess = Expression.Field(castInstance, fieldInfo);
            var convertResult = Expression.Convert(fieldAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }

        public static Func<object, object> CreateGetter(PropertyInfo propertyInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "target");
            var castInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType);
            var getterCall = Expression.Call(castInstance, propertyInfo.GetGetMethod(true));
            var convertResult = Expression.Convert(getterCall, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }
    }
}