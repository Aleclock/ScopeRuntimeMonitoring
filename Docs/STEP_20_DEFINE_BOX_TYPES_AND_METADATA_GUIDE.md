# Step 20 - Define box types and metadata

Goal: turn `MonitorAttribute` into a real metadata source so each monitored member can declare what kind of box it wants, whether it is editable, and how it should be identified later.

This step does not implement the visual editor controls yet. It only defines the box metadata contract so the next steps can build on it cleanly.

The model we want
- `BoxType` tells us what kind of box to render.
- `Id` gives the box a stable identifier.
- `Group` lets us group related boxes together.
- `Variant` lets us choose a style variant later.
- `Editable` tells us if the box can write back to the target.
- `Min`, `Max`, `Step` are used later for sliders and numeric inputs.
- `Enabled` keeps the current on/off behavior.

Why this matters
- The current system treats every box like the same visual container.
- This step creates the metadata layer needed for text boxes, sliders, toggles, and future custom renderers.
- It also gives us stable IDs for routing and filtering later.

## 1) Add `MonitorBoxType.cs`

Create a new file at `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/MonitorBoxType.cs` with this content:

```csharp
public enum MonitorBoxType
{
    Text,
    Toggle,
    Slider,
    InputText,
    Progress,
    Custom
}
```

What each value means:
- `Text`: read-only value display
- `Toggle`: boolean input
- `Slider`: numeric range input
- `InputText`: editable text field
- `Progress`: progress-style visualization
- `Custom`: special renderer for later

## 2) Add `MonitorBoxMetadata.cs`

Create a new file at `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/MonitorBoxMetadata.cs` with this content:

```csharp
using System;
using System.Reflection;

public readonly struct MonitorBoxMetadata
{
    public readonly string Id;
    public readonly string Label;
    public readonly string Group;
    public readonly string Variant;
    public readonly MonitorBoxType BoxType;
    public readonly bool Editable;
    public readonly float Min;
    public readonly float Max;
    public readonly float Step;
    public readonly bool Enabled;

    public MonitorBoxMetadata(
        string id,
        string label,
        string group,
        string variant,
        MonitorBoxType boxType,
        bool editable,
        float min,
        float max,
        float step,
        bool enabled)
    {
        Id = id;
        Label = label;
        Group = group;
        Variant = variant;
        BoxType = boxType;
        Editable = editable;
        Min = min;
        Max = max;
        Step = step;
        Enabled = enabled;
    }

    public static MonitorBoxMetadata From(object target, MemberInfo member, MonitorAttribute attribute)
    {
        var fallbackLabel = member?.Name ?? string.Empty;
        var label = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
            ? attribute.Label
            : fallbackLabel;

        var stableMemberName = member?.DeclaringType == null
            ? fallbackLabel
            : $"{member.DeclaringType.FullName}.{member.Name}";

        var id = attribute != null && !string.IsNullOrWhiteSpace(attribute.Id)
            ? attribute.Id
            : $"{target?.GetHashCode() ?? 0}::{stableMemberName}";

        var group = attribute != null && !string.IsNullOrWhiteSpace(attribute.Group)
            ? attribute.Group
            : member?.DeclaringType?.Name ?? string.Empty;

        var variant = attribute != null && !string.IsNullOrWhiteSpace(attribute.Variant)
            ? attribute.Variant
            : string.Empty;

        var boxType = attribute != null ? attribute.BoxType : MonitorBoxType.Text;
        var editable = attribute != null && attribute.Editable;
        var min = attribute != null ? attribute.Min : 0f;
        var max = attribute != null ? attribute.Max : 1f;
        var step = attribute != null ? attribute.Step : 0.1f;
        var enabled = attribute == null || attribute.Enabled;

        return new MonitorBoxMetadata(id, label, group, variant, boxType, editable, min, max, step, enabled);
    }
}
```

What this does:
- creates a stable metadata object from the attribute
- generates a fallback ID if the attribute did not specify one
- keeps the data close to the monitored member

## 3) Update `MonitorAttribute.cs`

Replace `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/MonitorAttribute.cs` with this version:

```csharp
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class MonitorAttribute : Attribute
{
    public string Label { get; }

    public string Id { get; set; }
    public string Group { get; set; }
    public string Variant { get; set; }
    public MonitorBoxType BoxType { get; set; } = MonitorBoxType.Text;
    public bool Editable { get; set; }
    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 1f;
    public float Step { get; set; } = 0.1f;
    public bool Enabled { get; set; } = true;

    public MonitorAttribute(string label)
    {
        Label = label;
    }
}
```

Notes:
- I recommend using named properties for metadata.
- Keep the constructor label for the display name.
- `Id` should be a constant string if you set it in code.

## 4) Update `IMonitorHandle.cs`

Replace `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/IMonitorHandle.cs` with this version:

```csharp
using System;

public interface IMonitorHandle
{
    string Id { get; }
    string Name { get; }
    object Target { get; }
    Type ValueType { get; }
    MonitorBoxMetadata Metadata { get; }
    object GetValueRaw();
    string GetValueString();
    bool Enabled { get; set; }
}
```

Why this helps:
- every UI layer can ask the handle for its box metadata
- later, the renderer does not need to know how metadata was built

## 5) Update `FieldMonitorHandle.cs`

Replace `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/FieldMonitorHandle.cs` with this version:

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
    public MonitorBoxMetadata Metadata { get; }

    public FieldMonitorHandle(object target, FieldInfo fieldInfo, MonitorAttribute attribute)
    {
        Target = target;
        Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
            ? attribute.Label
            : fieldInfo.Name;
        ValueType = fieldInfo.FieldType;
        Metadata = MonitorBoxMetadata.From(target, fieldInfo, attribute);
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
```

## 6) Update `PropertyMonitorHandle.cs`

Replace `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/PropertyMonitorHandle.cs` with this version:

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
    public MonitorBoxMetadata Metadata { get; }

    public PropertyMonitorHandle(object target, PropertyInfo propertyInfo, MonitorAttribute attribute)
    {
        Target = target;
        _property = propertyInfo;
        Name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
            ? attribute.Label
            : propertyInfo.Name;
        ValueType = propertyInfo.PropertyType;
        Metadata = MonitorBoxMetadata.From(target, propertyInfo, attribute);
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
```

## 7) Update `MonitoringRegistry.cs`

Replace `Assets/com.acproject.scoperuntimemonitoring/Runtime/RuntimeMonitoring/MonitoringRegistry.cs` with this version:

```csharp
using System;
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
            return _handles.ToArray();
        }
    }

    public bool RegisterTarget(object target)
    {
        if (target == null) return false;

        lock (_sync)
        {
            if (_targetHandles.ContainsKey(target))
                return _targetHandles[target].Count > 0;
        }

        var handles = new List<IMonitorHandle>();

        DiscoverFields(target, handles);
        DiscoverProperties(target, handles);

        if (handles.Count > 0)
        {
            _targetHandles[target] = handles;
            _handles.AddRange(handles);
            return true;
        }

        _targetHandles[target] = handles;
        return false;
    }

    private void DiscoverFields(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<MonitorAttribute>(true);
            if (attribute == null || !attribute.Enabled)
                continue;

            destination.Add(new FieldMonitorHandle(target, field, attribute));
        }
    }

    private void DiscoverProperties(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<MonitorAttribute>(true);
            if (attribute == null || !attribute.Enabled)
                continue;

            destination.Add(new PropertyMonitorHandle(target, property, attribute));
        }
    }

    public void UnregisterTarget(object target)
    {
        if (target == null) return;

        lock (_sync)
        {
            if (!_targetHandles.TryGetValue(target, out var handles))
                return;

            foreach (var handle in handles)
                _handles.Remove(handle);

            _targetHandles.Remove(target);
        }
    }
}
```

What changed in the registry:
- it now reads the `MonitorAttribute` instance instead of only checking for its presence
- it passes the attribute into the handle constructor
- it ignores disabled attributes at discovery time

## 8) Example usage

After this step, your monitored code can look like this:

```csharp
public class PlayerStats : MonoBehaviour
{
    [Monitor("Health", Id = "player.health", Group = "Player", Variant = "compact", BoxType = MonitorBoxType.Slider, Editable = true, Min = 0, Max = 100, Step = 1)]
    public float Health => currentHealth;

    [Monitor("Is Alive", Id = "player.isAlive", Group = "Player", BoxType = MonitorBoxType.Toggle)]
    public bool IsAlive => currentHealth > 0;

    [Monitor("Score", Id = "player.score", Group = "Player", BoxType = MonitorBoxType.Text)]
    public int Score => (int)(currentHealth * 2);

    private float currentHealth = 100f;
}
```

What this gives you:
- a stable box ID
- a box type for renderer selection later
- grouping info for panel organization
- a variant name for later styling

## 9) What this step does not do yet

This step does not yet:
- create slider/input/toggle UI controls
- write values back to the target
- render mixed visualizations
- route boxes to panels by metadata

Those are the next steps.

## 10) Validation checklist

After applying the files above:
- monitored fields and properties should still register
- disabled attributes should be ignored
- `IMonitorHandle.Metadata` should be populated
- the registry should still return handles normally

If you want a quick sanity check, use these kinds of attributes in one monitored class:
- one `Text` box
- one `Slider` box
- one `Toggle` box

Then verify that the registry still detects all three handles and that their metadata is present.

## 11) Recommended order after this step

1. wire the box renderer to read `BoxType`
2. add the editable UI for `Toggle`, `Slider`, and `InputText`
3. keep `Text` as the default fallback
4. later, add style variants based on `Variant`
