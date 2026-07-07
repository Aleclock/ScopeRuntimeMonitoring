using System;
using System.Reflection;

namespace ScopeRuntimeMonitoring
{
    public class FieldMonitorHandle : IMonitorHandle
    {
        private readonly Func<object, object> _getter;

        public string Id { get; }
        public string Name { get; }
        public object Target { get; }
        public bool Enabled { get; set; } = true;
        public Type ValueType { get; }
        public MonitorWidgetMetadata Metadata { get; }

        public FieldMonitorHandle(object target, FieldInfo fieldInfo, MonitorAttribute attribute)
        {
            Target = target;
            Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
                ? attribute.Label
                : fieldInfo.Name;
            ValueType = fieldInfo.FieldType;
            Metadata = MonitorWidgetMetadata.From(target, fieldInfo, attribute);
            Id = Metadata.Id;
            _getter = GetterFactory.CreateGetter(fieldInfo);
        }

        public object GetValueRaw()
        {
            if (!Enabled || Target == null)
                return null;

            return _getter(Target);
        }

        public string GetValueString()
        {
            return ValueFormatter.FormatValue(GetValueRaw());
        }
    }
}