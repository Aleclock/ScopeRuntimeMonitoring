using System;
using System.Reflection;
#if UNITY_EDITOR || !ENABLE_IL2CPP
using System.Linq.Expressions;
#endif

public static class GetterFactory
{
    public static Func<object, object> CreateGetter(FieldInfo fieldInfo)
    {
#if UNITY_EDITOR || !ENABLE_IL2CPP
        try
        {
            var instanceParam = Expression.Parameter(typeof(object), "target");
            var castInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
            var fieldAccess = Expression.Field(castInstance, fieldInfo);
            var convertResult = Expression.Convert(fieldAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }
        catch
        {
            // Fall back to reflection if compilation fails
            return target => fieldInfo.GetValue(target);
        }
#else
        return target => fieldInfo.GetValue(target);
#endif
    }

    public static Func<object, object> CreateGetter(PropertyInfo propertyInfo)
    {
#if UNITY_EDITOR || !ENABLE_IL2CPP
        try
        {
            var instanceParam = Expression.Parameter(typeof(object), "target");
            var castInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType);
            var getterCall = Expression.Call(castInstance, propertyInfo.GetGetMethod(true));
            var convertResult = Expression.Convert(getterCall, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }
        catch
        {
            // Fall back to reflection if compilation fails
            return target => propertyInfo.GetValue(target);
        }
#else
        return target => propertyInfo.GetValue(target);
#endif
    }
}