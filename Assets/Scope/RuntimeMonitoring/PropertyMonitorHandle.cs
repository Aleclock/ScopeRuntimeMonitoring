using System.Reflection;

public class PropertyMonitorHandle : IMonitorHandle
{
    private readonly PropertyInfo propertyInfo;

    public object Target { get; }
    public string Name { get; }

    public PropertyMonitorHandle(object target, PropertyInfo propertyInfo)
    {
        Target = target;
        this.propertyInfo = propertyInfo;
        Name = propertyInfo.Name;
    }

    public string GetValueString()
    {
        if (Target == null)
            return "null";

        if (propertyInfo == null || !propertyInfo.CanRead)
            return "unavailable";
        
        var value = propertyInfo.GetValue(Target);
        return ValueFormatter.FormatValue(value);
    }
}