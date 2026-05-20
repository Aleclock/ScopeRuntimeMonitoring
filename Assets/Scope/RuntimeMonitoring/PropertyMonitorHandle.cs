using System;
using System.Reflection;

public class PropertyMonitorHandle : IMonitorHandle
{
    private readonly Func<object, object> _getter;
    private readonly PropertyInfo _property;

    public string Id { get; }
    public string Name { get; }
    public object Target { get; }
    public bool Enabled { get; set; } = true;
    public Type ValueType { get; }

    public PropertyMonitorHandle(object target, PropertyInfo propertyInfo)
    {
        Target = target;
        _property = propertyInfo;
        Id = $"{target.GetHashCode()}::{propertyInfo.DeclaringType?.FullName}.{propertyInfo.Name}";
        Name = propertyInfo.Name;
        ValueType = propertyInfo.PropertyType;
        _getter = propertyInfo.CanRead ? GetterFactory.CreateGetter(propertyInfo) : null;
    }

    public object GetValueRaw()
    {
        if (!Enabled || Target == null || _getter == null)
            return null;
        
        return _getter(Target);
    }

    public string GetValueString()
    {
        return ValueFormatter.FormatValue(GetValueRaw());
    }
}