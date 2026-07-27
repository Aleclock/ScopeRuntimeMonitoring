using System;
using System.Collections.Generic;

namespace ScopeRuntimeMonitoring
{
    public static class ValueProcessorRegistry
    {
        private static readonly Dictionary<Type, Func<object, string>> _processors = new Dictionary<Type, Func<object, string>>();

        static ValueProcessorRegistry()
        {
            // register defaults
            _processors[typeof(float)] = v => ((float)v).ToString("F2");
            _processors[typeof(int)] = v => ((int)v).ToString();
            _processors[typeof(bool)] = v => ((bool)v) ? "true" : "false";
            _processors[typeof(UnityEngine.Vector3)] = v => {
                var vec = (UnityEngine.Vector3)v;
                return $"({vec.x:F2}, {vec.y:F2}, {vec.z:F2})";
            };

            // IEnumerable processor (excluding string)
            _processors[typeof(System.Collections.IEnumerable)] = v =>
            {
                if (v is string s) return s;
                var list = new List<string>();
                foreach (var item in (System.Collections.IEnumerable)v)
                {
                    list.Add("• " + ValueFormatter.FormatValue(item));
                }
                return string.Join("\n", list);
            };

            // TODO Add Quaternion
            // TODO Add Color
            // TODO Add Enums
        }

        public static void AddProcessor(Type type, Func<object, string> processor)
        {
            _processors[type] = processor;
        }

        public static Func<object, string> GetProcessor(Type type)
        {
            if (type == null) return null;
            if (_processors.TryGetValue(type, out var p)) return p;

            // Check interfaces
            foreach (var iface in type.GetInterfaces())
            {
                if (_processors.TryGetValue(iface, out p)) return p;
            }

            // Fallback to base types
            var baseType = type.BaseType;
            while(baseType != null)
            {
                if (_processors.TryGetValue(baseType, out p)) return p;
                baseType = baseType.BaseType;
            }
            
            return null;
        }
    }
}