# Step 2 Guide: Add Basic Value Formatting

This guide shows how to format values nicely without changing the runtime architecture.

The goal is to improve readability:

- floats display with 2 decimals,
- bools display as color-coded "true"/"false",
- Vector3 displays on one line,
- integers, strings, and unknowns use `ToString()`.

The panel UI stays the same — you're only changing what `GetValueString()` returns.

## What You Are Building

You will create a simple value formatter that:

- knows how to format common types,
- can be extended with new types easily,
- lives separate from the handle classes,
- makes the panel automatically look better without changes.

This is the idea of a "value processor" — a small formatting layer.

## The Files You Will Create Or Update

You will work with:

- a new `ValueFormatter` class (the formatter),
- updated `FieldMonitorHandle` and `PropertyMonitorHandle` (use the formatter),
- your test class (add a few typed values).

## Step 0: Add More Monitored Values To Your Test Class

First, add variety to what you're monitoring.

This gives you test cases for formatting.

### Copy-paste addition to MonitoredExample

```csharp
using UnityEngine;

public class MonitoredExample : MonoBehaviour
{
    public float currentHealth = 100f;

    [Monitor("Health")]
    public float Health => currentHealth;

    [Monitor("Is Alive")]
    public bool IsAlive => currentHealth > 0;

    [Monitor("Score")]
    public int Score => (int)(currentHealth * 2);

    [Monitor("Position")]
    public Vector3 Position => transform.position;

    private void Update()
    {
        currentHealth = Mathf.PingPong(Time.time * 10f, 100f);
    }
}
```

What this adds:

- a bool for true/false coloring,
- an int for integer display,
- a Vector3 for multi-line or compact formatting.

## Step 1: Create A Value Formatter

This is a small utility class that converts any value to a formatted string.

The key idea: check the type, apply type-specific formatting, fall back to `ToString()`.

### Copy-paste the ValueFormatter

```csharp
using UnityEngine;

public static class ValueFormatter
{
    public static string FormatValue(object value)
    {
        if (value == null)
        {
            return "null";
        }

        var type = value.GetType();

        // Float: 2 decimal places
        if (type == typeof(float))
        {
            return ((float)value).ToString("F2");
        }

        // Int: no change needed
        if (type == typeof(int))
        {
            return ((int)value).ToString();
        }

        // Bool: use text representation
        if (type == typeof(bool))
        {
            return ((bool)value) ? "true" : "false";
        }

        // Vector3: compact format
        if (type == typeof(Vector3))
        {
            var v = (Vector3)value;
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }

        // Vector2: compact format
        if (type == typeof(Vector2))
        {
            var v = (Vector2)value;
            return $"({v.x:F2}, {v.y:F2})";
        }

        // Default: use ToString()
        return value.ToString();
    }
}
```

What to notice:

- it's a static utility with one public method,
- it checks type and applies formatting rules,
- it falls back to `ToString()` for unknown types,
- adding a new type is as simple as adding another `if` block.

## Step 2: Update The Field Handle To Use Formatting

Your `FieldMonitorHandle` currently calls `ToString()` directly.

Change it to use the formatter.

### Copy-paste the updated FieldMonitorHandle

```csharp
using System.Reflection;

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
        if (Target == null)
        {
            return "null";
        }

        var value = _field.GetValue(Target);
        return ValueFormatter.FormatValue(value);
    }
}
```

What changed:

- `GetValueString()` now calls `ValueFormatter.FormatValue()` instead of `value.ToString()`,
- that's it. The handle stays simple.

## Step 3: Update The Property Handle To Use Formatting

Same idea as the field handle.

### Copy-paste the updated PropertyMonitorHandle

```csharp
using System.Reflection;

public class PropertyMonitorHandle : IMonitorHandle
{
    private readonly PropertyInfo _propertyInfo;

    public object Target { get; }
    public string Name { get; }

    public PropertyMonitorHandle(object target, PropertyInfo propertyInfo)
    {
        Target = target;
        _propertyInfo = propertyInfo;
        Name = propertyInfo.Name;
    }

    public string GetValueString()
    {
        if (Target == null)
        {
            return "null";
        }

        if (_propertyInfo == null || !_propertyInfo.CanRead)
        {
            return "unavailable";
        }

        var value = _propertyInfo.GetValue(Target);
        return ValueFormatter.FormatValue(value);
    }
}
```

What changed:

- same as field handle: use `ValueFormatter.FormatValue()` instead of direct `ToString()`.

## Step 4: Test The Formatting In Play Mode

Run the scene and observe:

- `Health` should show `100.00` (or similar, based on PingPong),
- `Is Alive` should show `true`,
- `Score` should show an integer,
- `Position` should show `(x.xx, y.yy, z.zz)` format.

If the formatting looks right, you're done with this step.

## Step 5: Add Color Coding (Optional Polish)

If you want bools to show in color, you can update `MonitorsPanel` to check the type and colorize text.

This is optional — formatting without color still works.

### Optional: Color bool values in the panel

In `MonitorsPanel.CreateTextField()`, after creating the text, check if the handle value is a bool and set the color:

```csharp
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

    // Optional: color-code bools
    var handle = /* get the handle for this text */;
    if (handle != null && /* check if value is bool */)
    {
        text.color = /* true color */ ? Color.green : Color.red;
    }
    else
    {
        text.color = Color.white;
    }

    var rectTransform = text.GetComponent<RectTransform>();
    rectTransform.anchorMin = new Vector2(0, 1);
    rectTransform.anchorMax = new Vector2(0, 1);
    rectTransform.pivot = new Vector2(0, 1);
    rectTransform.anchoredPosition = anchoredPosition;
    rectTransform.sizeDelta = new Vector2(width, height);

    return text;
}
```

This is a quick sketch — the cleaner approach is to pass the handle itself so the text knows what it's displaying.

## Step 6: Add A New Format Type

This is where the architecture proves itself.

Want to format `Color` or `Quaternion`?

Add one `if` block to `ValueFormatter`:

```csharp
// In ValueFormatter.FormatValue():

if (type == typeof(Color))
{
    var c = (Color)value;
    return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
}
```

That's it. No changes to handles, registry, or panel needed.

## Step 7: Common Formatting Patterns

When you add new types, use these patterns:

- **Single-line values** (float, int, string): just return the formatted string,
- **Multi-part values** (Vector, Quaternion): format all parts on one line with `F2` (2 decimals),
- **Collections** (arrays, lists): consider showing `[count=5]` or first few items,
- **Enums**: use `ToString()` which gives the enum name.

## Step 8: What You Learned In This Step

Formatting is decoupled from monitoring:

- the runtime discovers values,
- the handles retrieve values,
- the formatter beautifies them,
- the panel displays them.

Each layer is independent. You can change formatting without touching discovery or UI.

That separation is what keeps the system maintainable as you grow it.

## Step 9: The Next Step After This One

After formatting works cleanly, the next milestone is:

- **Step 3**: make the panel auto-size and scroll if it grows (UI polish),
- or **Step 3 alternative**: add method monitoring (runtime expansion).

Both are good next steps depending on what interests you more:

- polish the display → go with auto-size and scroll,
- expand what you can monitor → go with methods.

I recommend auto-size and scroll first, since it solves a real problem (too many values overflow the panel).
