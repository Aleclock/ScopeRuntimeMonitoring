using System;
using System.Reflection;

public class FieldMonitorHandle : IMonitorHandle
{
    private readonly Func<object, object> _getter;
    
    public string Id { get; }
    public string Name { get; }
    public object Target { get; }
    public bool Enabled { get; set; } = true;
    public Type ValueType { get; }
    
    public FieldMonitorHandle(object target, FieldInfo fieldInfo)
    {
        Target = target;
        Id = $"{target.GetHashCode()}::{fieldInfo.DeclaringType?.FullName}.{fieldInfo.Name}";
        Name = fieldInfo.Name;
        ValueType = fieldInfo.FieldType;
        _getter = GetterFactory.CreateGetter(fieldInfo);
    }

    public object GetValueRaw()
    {
        if (!Enabled  || Target == null)
            return null;

        return _getter(Target);
    }

    public string GetValueString()
    {
        return ValueFormatter.FormatValue(GetValueRaw());
    }
}