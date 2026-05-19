# Step 1 Guide: Add A Property Monitor

This guide is the practical next exercise after your field-based panel works.

The goal is simple:

- keep your current `Scope` UI,
- add one property to a monitored class,
- create a handle for that property,
- show it in `MonitorsPanel`,
- understand every line you paste.

If you already have a field monitor working, this is the best next step.

## What You Are Building

You will add one new monitored property and make the runtime treat it like a field.

You are not building the whole package yet.

You are only proving one idea:

- a monitored member can be read through a common interface,
- the panel does not need to care whether the value came from a field or a property.

That separation is what makes the system scalable later.

## The Files You Will Create Or Update

You will work with these files:

- a sample `MonoBehaviour` with one monitored property,
- a property handle class,
- your registry or discovery code,
- your `MonitorsPanel`.

If you want to keep the changes focused, do them in this order:

1. add the sample property,
2. add the property handle,
3. connect it in the registry,
4. show it in the panel,
5. test it in Play Mode.

## Step 0: Choose One Small Property

Pick a property that is easy to understand.

Good examples:

- `Health`
- `Score`
- `Speed`
- `IsAlive`

Try to avoid complex computed properties for this step.

You want something that changes in Play Mode and is cheap to read.

### Example sample class

If you want a ready-made test object, use something like this.

```csharp
using UnityEngine;

public class MonitoredExample : MonoBehaviour
{
    public float currentHealth = 100f;

    [Monitor("Health")]
    public float Health => currentHealth;

    private void Update()
    {
        currentHealth = Mathf.PingPong(Time.time * 10f, 100f);
    }
}
```

What this does:

- `currentHealth` is the backing field,
- `Health` is the property you will monitor,
- `Update()` changes the value so you can see the panel refresh.

If you already have your own sample object, use that instead.

## Step 1: Make Sure Your Attribute Can Mark Properties

Your monitoring attribute should allow properties, not only fields.

If you already wrote an attribute like this, check that it includes properties in its target list.

### Copy-paste version

```csharp
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class MonitorAttribute : Attribute
{
    public string Label;

    public MonitorAttribute(string label = null)
    {
        Label = label;
    }
}
```

What to notice:

- `AttributeTargets.Property` is the important part for this step,
- `Label` is optional, but useful for a clean display name later,
- the attribute itself is intentionally small.

## Step 2: Define A Common Handle Interface

The panel should not know whether a value comes from a field or property.

For that reason, both kinds of monitored members should implement the same interface.

### Copy-paste version

```csharp
public interface IMonitorHandle
{
    string Name { get; }
    object Target { get; }
    string GetValueString();
}
```

What this means:

- `Name` is what the UI shows,
- `Target` lets the registry remove handles later,
- `GetValueString()` is the only thing the panel needs to refresh text.

This interface is the key boundary between runtime and UI.

## Step 3: Create A Property Handle

Now create a new class that reads a property through reflection.

This is the smallest piece that makes the step real.

### Copy-paste version

```csharp
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
        {
            return "null";
        }

        if (propertyInfo == null || !propertyInfo.CanRead)
        {
            return "unavailable";
        }

        var value = propertyInfo.GetValue(Target);
        return value != null ? value.ToString() : "null";
    }
}
```

Why each part exists:

- `PropertyInfo` is the reflection object for the property,
- `Target` is the runtime instance you read from,
- `Name` is the display label,
- `GetValueString()` converts the current property value into text.

This is the property equivalent of a field handle.

## Step 4: Teach The Registry To Discover Properties

Your registry already knows how to discover fields.

Now add a second discovery path for properties.

Keep both — the pattern is:

- inspect the target type,
- get its fields and check for `[Monitor]`,
- get its properties and check for `[Monitor]`,
- create appropriate handles (field or property),
- add them to the same handle list.

This is important: both fields and properties go to the same `IMonitorHandle` list.

### Copy-paste discovery shape

Use this shape to update your `RegisterTarget` method.

**Important**: Both fields **and** properties must check for `[Monitor]` attribute. Do not register all fields without checking.

```csharp
using System.Collections.Generic;
using System.Reflection;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();

    public IReadOnlyList<IMonitorHandle> GetMonitorHandles()
    {
        return _handles;
    }

    public void RegisterTarget(object target)
    {
        if (target == null)
        {
            return;
        }

        DiscoverFields(target);
        DiscoverProperties(target);
    }

    private void DiscoverFields(object target)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(MonitorAttribute), true))
            {
                continue;
            }

            _handles.Add(new FieldMonitorHandle(target, field));
        }
    }

    private void DiscoverProperties(object target)
    {
        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            if (!property.IsDefined(typeof(MonitorAttribute), true))
            {
                continue;
            }

            _handles.Add(new PropertyMonitorHandle(target, property));
        }
    }

    public void UnregisterTarget(object target)
    {
        _handles.RemoveAll(handle => handle.Target == target);
    }
}
```

What to notice:

- **both fields and properties check for `[Monitor]`** — only marked members are registered,
- field and property discovery are in separate methods for clarity,
- they create different handle types, but both implement `IMonitorHandle`,
- the panel still receives `IMonitorHandle` objects, so it doesn't care which kind,
- later you can add methods, static members, etc. using the same pattern.

## Step 5: Register Your Example Object

The registry can only discover something if the object is registered first.

So make sure your example object is passed into the monitoring system.

### Copy-paste bootstrap example

```csharp
using UnityEngine;

public class MonitoringBootstrap : MonoBehaviour
{
    private void Start()
    {
        var example = FindObjectOfType<MonitoredExample>();
        if (example != null)
        {
            Monitor.StartMonitoring(example);
        }
    }
}
```

What this does:

- waits until Play Mode starts,
- finds your sample object in the scene,
- registers it with the monitor system.

If you already have another way of registering objects, use that instead.

## Step 6: Show The Property In `MonitorsPanel`

At this point your panel should still be able to use the same handle interface.

That is the good part.

The UI should only ask for two things:

- the name,
- the current value string.

### Copy-paste panel shape

Use this as a reference if you want to update your panel.

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonitorsPanel : DebugPanelBase
{
    private readonly List<IMonitorHandle> handles = new List<IMonitorHandle>();
    private readonly List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    public override void Initialize()
    {
        CreatePanelBackground();

        handles.AddRange(Monitor.Registry.GetMonitorHandles());

        float y = -10f;
        foreach (var handle in handles)
        {
            var text = CreateTextField(
                handle.Name,
                new Vector2(10f, y),
                18,
                $"{handle.Name}: {handle.GetValueString()}",
                400f,
                24f);

            texts.Add(text);
            y -= 24f;
        }
    }

    private TextMeshProUGUI CreateTextField(
        string name,
        Vector2 anchoredPosition,
        int fontSize,
        string initialText,
        float width,
        float height)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(panelBackground.transform, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;

        var rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(width, height);

        return text;
    }

    private void Update()
    {
        for (int i = 0; i < handles.Count; i++)
        {
            texts[i].text = $"{handles[i].Name}: {handles[i].GetValueString()}";
        }
    }

    public override void InizializePanelData()
    {
    }

    public override void UpdatePanelData()
    {
    }
}
```

What to notice:

- `handles` is a list of `IMonitorHandle`, so fields and properties are treated the same,
- `CreateTextField()` makes one text object per handle,
- `Update()` refreshes every label every frame,
- the panel does not care whether the handle is backed by a field or a property.

## Step 7: What To Check If It Does Not Work

If the property does not appear, check these things in order:

1. Is the property public?
2. Does it have a getter?
3. Does it have `[Monitor]` on it?
4. Is the object actually registered?
5. Did the registry discover the property?
6. Did the panel pull handles from the same registry instance?

Those six checks usually find the problem quickly.

## Step 8: What You Learned In This Step

If it works, you have already learned the important architectural idea:

- discovery happens in the runtime,
- storage happens in the registry,
- rendering happens in the panel,
- the panel only depends on `IMonitorHandle`.

That is the structure you want to keep as you grow the system.

## Step 9: The Next Step After This One

Once the property works, the next easiest expansion is a method return monitor.

After that, add formatting.

A good order is:

1. field,
2. property,
3. method,
4. formatting,
5. nicer UI.

That keeps the project understandable and avoids mixing too many problems at once.

## Final Advice

Do not try to make one giant abstraction for every member type right away.

Keep it concrete:

- one handle for fields,
- one handle for properties,
- one panel that reads both.

That is enough to move forward without getting lost.
