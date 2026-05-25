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
    public MonitorWidgetMetadata Metadata { get; }

    public PropertyMonitorHandle(object target, PropertyInfo propertyInfo, MonitorAttribute attribute)
    {
        Target = target;
        _property = propertyInfo;
        Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
            ? attribute.Label
            : propertyInfo.Name;
        ValueType = propertyInfo.PropertyType;
        Metadata = MonitorWidgetMetadata.From(target, propertyInfo, attribute);
        Id = Metadata.Id;
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