using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScopeRuntimeMonitoring
{
    public class PropertyMonitorHandle<TTarget, TValue> : IMonitorHandle
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

        public PropertyMonitorHandle(object target, PropertyInfo propertyInfo, MonitorAttribute attribute)
        {
            Target = target;
            _targetTyped = (TTarget)target;
            Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
                ? attribute.Label
                : propertyInfo.Name;
            ValueType = typeof(TValue);
            Metadata = MonitorWidgetMetadata.From(target, propertyInfo, attribute);
            Id = Metadata.Id;

            if (propertyInfo.CanRead)
            {
                _getter = CreateTypedGetter(propertyInfo);
                _lastValue = _getter(_targetTyped);
            }
            else
            {
                _getter = null;
                _lastValue = default;
            }
        }

        private static Func<TTarget, TValue> CreateTypedGetter(PropertyInfo propertyInfo)
        {
            var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(TTarget), "target");
            var getMethod = propertyInfo.GetGetMethod(true);
            var call = System.Linq.Expressions.Expression.Call(instanceParam, getMethod);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TTarget, TValue>>(call, instanceParam);
            return lambda.Compile();
        }

        public object GetValueRaw()
        {
            if (!Enabled || Target == null || _getter == null)
                return null;

            return _getter(_targetTyped);
        }

        public string GetValueString()
        {
            return ValueFormatter.FormatValue(GetValueRaw());
        }

        public bool UpdateValueAndCheckIfChanged()
        {
            if (!Enabled || Target == null || _getter == null)
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