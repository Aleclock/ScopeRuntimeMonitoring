using System.Reflection;
using System.Runtime.Serialization;

public class FieldMonitorHandle : IMonitorHandle
{
    private readonly FieldInfo _field;
    public object Target { get; }
    public string Name { get; }
    
    public FieldMonitorHandle(object target, FieldInfo field)
    {
        Target = target;
        _field = field;
        Name = field.Name;
    }

    public string GetValueString()
    {
        var val = _field.GetValue(Target);
        return ValueFormatter.FormatValue(val);
    }
}