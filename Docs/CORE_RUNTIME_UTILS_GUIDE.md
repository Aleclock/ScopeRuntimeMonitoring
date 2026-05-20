# Core Runtime Utilities — Step-by-step Guide

Purpose
-------
This guide explains small, high-value utilities to add to your minimal runtime between Step 1 and Step 2. Each utility is copy-paste friendly and keeps changes minimal and focused. Implement them one at a time and verify in Play Mode.

High-level checklist
--------------------
- [ ] Extend `MonitorAttribute` with optional metadata (Label, Format, Tags)
- [ ] Improve `IMonitorHandle` interface (ValueType, GetValueRaw, Enabled, Id)
- [ ] Add duplicate-registration guard and a target->handles map in `MonitoringRegistry`
- [ ] Implement cached delegate getters for fields/properties
- [ ] Add `ValueProcessorRegistry` and wire `ValueFormatter` to it
- [ ] Add `MonitoringLogger` utility (lightweight)
- [ ] Add a simple thread-safety guard (lock) around handle list modifications

Notes on scope
--------------
- Keep runtime behaviour unchanged for panels: they still consume `IMonitorHandle` and call `GetValueString()`.
- These utilities are internal improvements: they make future work easier (formatting, performance, removal, AOT). They are safe to add incrementally.

File-by-file steps
------------------
1) Extend `MonitorAttribute` (optional metadata)

Create or update `MonitorAttribute.cs`:

```csharp
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class MonitorAttribute : Attribute
{
    public string Label { get; }
    public string Format { get; set; }
    public string[] Tags { get; set; }
    public bool Enabled { get; set; } = true;

    public MonitorAttribute(string label = null)
    {
        Label = label;
    }
}
```

Why: a `Label` allows UIs to show friendly names. `Format` is a hint for formatters (e.g. "F1", "%"), `Tags` let you filter later. `Enabled` allows disabling individual monitors.

2) Improve `IMonitorHandle`

Update the interface to expose raw value and type information.

```csharp
public interface IMonitorHandle
{
    string Id { get; }
    string Name { get; }
    object Target { get; }
    Type ValueType { get; }
    object GetValueRaw();
    string GetValueString();
    bool Enabled { get; set; }
}
```

Why: `ValueType` and `GetValueRaw()` let the formatter make type-specific decisions. `Id` + `Enabled` improve management.

3) Duplicate-safety and target->handles map in `MonitoringRegistry`

Update `MonitoringRegistry` to track registered targets and their handles for fast unregister and to avoid duplicate registration.

```csharp
using System.Collections.Generic;
using System.Reflection;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();
    private readonly Dictionary<object, List<IMonitorHandle>> _targetHandles = new Dictionary<object, List<IMonitorHandle>>();
    private readonly object _sync = new object();

    public IReadOnlyList<IMonitorHandle> GetMonitorHandles()
    {
        lock (_sync)
        {
            // Return a snapshot to avoid external mutation/race issues.
            return _handles.ToArray();
        }
    }

    public void RegisterTarget(object target)
    {
        if (target == null)
        {
            return;
        }

        lock (_sync)
        {
            if (_targetHandles.ContainsKey(target))
            {
                return; // already registered
            }

            var handles = new List<IMonitorHandle>();

            DiscoverFields(target, handles);
            DiscoverProperties(target, handles);

            if (handles.Count > 0)
            {
                _targetHandles[target] = handles;
                _handles.AddRange(handles);
            }
            else
            {
                // Still track target to prevent repeated discovery attempts.
                _targetHandles[target] = handles;
            }
        }
    }

    private void DiscoverFields(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(MonitorAttribute), true))
            {
                continue;
            }

            destination.Add(new FieldMonitorHandle(target, field));
        }
    }

    private void DiscoverProperties(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            if (!property.IsDefined(typeof(MonitorAttribute), true) || !property.CanRead)
            {
                continue;
            }

            destination.Add(new PropertyMonitorHandle(target, property));
        }
    }

    public void UnregisterTarget(object target)
    {
        if (target == null)
        {
            return;
        }

        lock (_sync)
        {
            if (!_targetHandles.TryGetValue(target, out var handles))
            {
                return;
            }

            foreach (var h in handles)
            {
                _handles.Remove(h);
            }

            _targetHandles.Remove(target);
        }
    }
}
```

Why: prevents duplicates and makes `UnregisterTarget` faster and safer.

4) Cached delegate getters (performance)

Reflection `GetValue` every frame is acceptable for small counts but will slow when many handles exist. Build a cached delegate once in the handle's constructor.

Add a small helper to create `Func<object, object>` getters using expressions.

```csharp
using System;
using System.Linq.Expressions;
using System.Reflection;

public static class GetterFactory
{
    public static Func<object, object> CreateGetter(FieldInfo fieldInfo)
    {
        var instanceParam = Expression.Parameter(typeof(object), "target");
        var castInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
        var fieldAccess = Expression.Field(castInstance, fieldInfo);
        var convertResult = Expression.Convert(fieldAccess, typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
        return lambda.Compile();
    }

    public static Func<object, object> CreateGetter(PropertyInfo propertyInfo)
    {
        var instanceParam = Expression.Parameter(typeof(object), "target");
        var castInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType);
        var getterCall = Expression.Call(castInstance, propertyInfo.GetGetMethod(true));
        var convertResult = Expression.Convert(getterCall, typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
        return lambda.Compile();
    }
}
```

Usage in a handle (full example):

```csharp
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

    public FieldMonitorHandle(object target, FieldInfo field)
    {
        Target = target;
        Id = $"{target.GetHashCode()}::{field.DeclaringType?.FullName}.{field.Name}";
        Name = field.Name;
        ValueType = field.FieldType;
        _getter = GetterFactory.CreateGetter(field);
    }

    public object GetValueRaw()
    {
        if (!Enabled || Target == null)
        {
            return null;
        }

        return _getter(Target);
    }

    public string GetValueString()
    {
        return ValueFormatter.FormatValue(GetValueRaw());
    }
}
```

And the matching property handle:

```csharp
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

    public PropertyMonitorHandle(object target, PropertyInfo property)
    {
        Target = target;
        _property = property;
        Id = $"{target.GetHashCode()}::{property.DeclaringType?.FullName}.{property.Name}";
        Name = property.Name;
        ValueType = property.PropertyType;
        _getter = property.CanRead ? GetterFactory.CreateGetter(property) : null;
    }

    public object GetValueRaw()
    {
        if (!Enabled || Target == null || _getter == null)
        {
            return null;
        }

        return _getter(Target);
    }

    // If your interface currently uses GetvalueString (lowercase v),
    // rename this method accordingly or fix the interface casing.
    public string GetValueString()
    {
        return ValueFormatter.FormatValue(GetValueRaw());
    }
}
```

Why: compiled access is much faster than reflection and the expression handles boxing/unboxing correctly.

5) `ValueProcessorRegistry` and existing `ValueFormatter`

You already have `ValueFormatter.FormatValue(...)` in your project. Keep using it.

If you want a slightly more extensible setup later, you can add a small `ValueProcessorRegistry` that feeds into `ValueFormatter`, but that is optional.

For now, the practical change is: make sure handles call `ValueFormatter.FormatValue(value)` and keep the formatter itself as the single place that decides how common types are displayed.

If you do want the optional registry later, this is the shape of it:

```csharp
using System;
using System.Collections.Generic;

public static class ValueProcessorRegistry
{
    private static readonly Dictionary<Type, Func<object,string>> _processors = new Dictionary<Type, Func<object,string>>();

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
    }

    public static void AddProcessor(Type type, Func<object,string> processor)
    {
        _processors[type] = processor;
    }

    public static Func<object,string> GetProcessor(Type type)
    {
        if (type == null) return null;
        if (_processors.TryGetValue(type, out var p)) return p;
        // fallback to base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (_processors.TryGetValue(baseType, out p)) return p;
            baseType = baseType.BaseType;
        }
        return null;
    }
}

public static class ValueFormatter
{
    public static string FormatValue(object value)
    {
        if (value == null) return "null";
        var t = value.GetType();
        var processor = ValueProcessorRegistry.GetProcessor(t);
        if (processor != null) return processor(value);
        return value.ToString();
    }
}
```

Why: centralised formatting registration allows swapping formatting rules without changing handles. But if your current `ValueFormatter` already handles floats, bools, Vector3, etc., you do not need to add a second wrapper class yet.

6) Lightweight `MonitoringLogger`

Add a tiny logger to centralize debug messages.

```csharp
using UnityEngine;

public static class MonitoringLogger
{
    public static void Log(string message)
    {
        Debug.Log($"[Monitoring] {message}");
    }

    public static void Warn(string message)
    {
        Debug.LogWarning($"[Monitoring] {message}");
    }

    public static void Error(string message)
    {
        Debug.LogError($"[Monitoring] {message}");
    }
}
```

Why: makes it easier to find monitoring-specific logs.

7) Thread-safety guard

You already added a `_sync` lock in the registry above. Keep using `lock(_sync)` around list/dictionary mutations.

Extra small improvements (optional)
----------------------------------
- Expose a `Monitor.StartMonitoring(target)` overload that accepts a `bool overwrite` or `bool force` flag.
- Add a `HandlePriority` or `Category` field to `IMonitorHandle` so the UI can group entries.
- Add a small `Tests` scene that generates 1000 dummy monitored objects to see how performance scales.

Verification checklist
----------------------
- [ ] Field and property still show correctly in `MonitorsPanel`.
- [ ] No duplicate entries when `Monitor.StartMonitoring()` is called twice for the same target.
- [ ] Formatted values appear as expected for float/int/bool/Vector3.
- [ ] No significant per-frame reflection overhead observed (compare before/after using a stopwatch or profiler).

Next steps after utilities
-------------------------
- Implement `ValueProcessorRegistry` extensions (format strings, culture, unit suffixes).
- Add method-handles and static members.
- Replace `MonitorsPanel` rendering with UI Toolkit.

---

Implement these utilities incrementally. Tell me which one you'd like to add first and I can produce the exact copy-paste edits for those files (or apply them for you if you'd like code changes).