using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScopeRuntimeMonitoring
{
    public class FieldMonitorHandle<TTarget, TValue> : IMonitorHandle
    {
        private readonly TTarget _targetTyped;
        private readonly Func<TTarget, TValue> _getter;
        private TValue _lastValue;

        public string Id { get; }
        public string Name { get; }
        public object Target { get; }
        public bool Enabled { get; set; } = true;
        public Type ValueType { get; }
        public MonitorWidgetMetadata Metadata { get; }

        public FieldMonitorHandle(object target, FieldInfo fieldInfo, MonitorAttribute attribute)
        {
            Target = target;
            _targetTyped = (TTarget)target;
            Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
                ? attribute.Label
                : fieldInfo.Name;
            ValueType = typeof(TValue);
            Metadata = MonitorWidgetMetadata.From(target, fieldInfo, attribute);
            Id = Metadata.Id;
            _getter = CreateTypedGetter(fieldInfo);
            _lastValue = _getter(_targetTyped);
        }

        private static Func<TTarget, TValue> CreateTypedGetter(FieldInfo fieldInfo)
        {
            var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(TTarget), "target");
            var fieldAccess = System.Linq.Expressions.Expression.Field(instanceParam, fieldInfo);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TTarget, TValue>>(fieldAccess, instanceParam);
            return lambda.Compile();
        }

        public object GetValueRaw()
        {
            if (!Enabled || Target == null)
                return null;

            return _getter(_targetTyped);
        }

        public string GetValueString()
        {
            return ValueFormatter.FormatValue(GetValueRaw());
        }

        public bool UpdateValueAndCheckIfChanged()
        {
            if (!Enabled || Target == null)
                return false;

            var currentValue = _getter(_targetTyped);
            if (EqualityComparer<TValue>.Default.Equals(_lastValue, currentValue))
                return false;

            _lastValue = currentValue;
            return true;
        }

        public bool GetValueBool()
        {
            if (_lastValue is bool b)
                return b;
            try
            {
                return Convert.ToBoolean(_lastValue);
            }
            catch
            {
                return false;
            }
        }

        public float GetValueFloat()
        {
            try
            {
                return Convert.ToSingle(_lastValue);
            }
            catch
            {
                return 0f;
            }
        }
    }
}