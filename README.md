# Scope Runtime Monitoring

A lightweight, high-performance runtime debugging and monitoring UI Toolkit panel for Unity. Annotate your fields and properties to instantly view, modify, and format variables directly in a clean in-game panel.

---

## Features

- **Attribute-Based Registration**: Annotate fields/properties with `[Monitor]`, `[MonitorSlider]`, `[MonitorToggle]`, etc., and they will automatically show up in the UI.
- **Dynamic Sorting & Ordering**: Control the display order of items and subgroups inside card boxes.
- **Runtime Box/Group Customization**: Customize box and group names at runtime per-instance (e.g., using GameObject names) or globally.
- **Vertical List & Collection Displays**: Collections/lists are auto-formatted as bulleted vertical lists that resize and wrap layout text cleanly.
- **Extensible Custom Widgets**: Build your own custom widgets (like charts, visual representations, or interactive components) and map them to custom variants.

---

## Quick Start

### 1. Annotating Fields & Properties
Attach the `[Monitor]` attributes to fields or properties on any class:

```csharp
using UnityEngine;
using ScopeRuntimeMonitoring;

public class PlayerStats : MonoBehaviour
{
    [Monitor("Health", Group = "Combat", Order = 1)]
    public float currentHealth = 100f;

    [MonitorToggle("Shielded", Group = "Combat", Order = 0)]
    public bool isShielded = true;

    [MonitorSlider("Mana", 0f, 100f, Group = "Magic")]
    public float Mana { get; set; }

    private void OnEnable() => Monitor.StartMonitoring(this);
    private void OnDisable() => Monitor.StopMonitoring(this);
}
```

### 2. Auto-UI Display
Add the `MonitorPanelView` component to a GameObject in your scene containing a `UIDocument`. The monitoring boxes will construct and update automatically when game objects start/stop monitoring.

---

## Advanced Usage

### 1. Customizing Box Names at Runtime (Per-Instance)
By default, the card box name defaults to the declaring class type name (or the static `Group` attribute value). To customize names dynamically (e.g., to create a card box matching the GameObject name):

Implement `IMonitorGroupCustomizer` (or `IMonitorSubGroupCustomizer`) on your component:

```csharp
public class Enemy : MonoBehaviour, IMonitorGroupCustomizer
{
    [Monitor("HP")] 
    public float hp = 100f;

    public string GetMonitorGroup()
    {
        return $"Enemy: {gameObject.name}";
    }

    private void OnEnable() => Monitor.StartMonitoring(this);
    private void OnDisable() => Monitor.StopMonitoring(this);
}
```

### 2. Global Naming Customizer (Delegate)
You can intercept naming rules globally for classes you do not control:

```csharp
Monitor.GroupCustomizer = (target) =>
{
    if (target is UnityEngine.Object unityObj)
        return $"Dynamic - {unityObj.name}";
        
    return null; // Fallback to attribute configuration
};
```

---

## Collections & Lists
Lists, arrays, and other `IEnumerable` collections are automatically formatted vertically with bullet points:

```csharp
[Monitor("Active Quests", Group = "Log")]
public List<string> Quests => new List<string> { "Kill 10 Slimes", "Return to Town" };
```
The value UI text automatically wraps and expands in height, aligning to the top-left for clean readability.

---

## Custom Extensible Widgets
You can define arbitrarily complex UI Toolkit widgets (e.g., charts, progress graphs, action buttons) and hook them up:

1. Annotate your property with `WidgetType.Custom` and choose a unique `Variant` name:
```csharp
[Monitor("History", WidgetType = MonitorWidgetType.Custom, Variant = "my_graph")]
public float FPS => currentFps;
```

2. Register your custom widget creator at startup:
```csharp
Monitor.RegisterCustomWidget("my_graph", (IMonitorHandle handle, out System.Action<object> updateCallback) =>
{
    // Create any VisualElement structure
    var container = new VisualElement();
    var graphLabel = new Label("---");
    container.Add(graphLabel);

    // Provide the action to update your widget elements when values change
    updateCallback = (newValue) =>
    {
        graphLabel.text = $"Graph: {newValue}";
    };

    return container;
});
```
