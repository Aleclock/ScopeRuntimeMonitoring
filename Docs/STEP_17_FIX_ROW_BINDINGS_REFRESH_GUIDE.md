# Step 17 — Fix `rowBindings` So UIToolkit Values Refresh Live

Goal
----
Fix the live-refresh path in `DynamicColumnLayout` so values update over time.

The current issue is simple:
- the box rows are created correctly,
- but the refresh list `rowBindings` is never populated,
- so `Update()` has nothing to refresh.

This guide shows the exact copy-paste changes to fix that.

Why the values are not updating
-------------------------------
In the current UIToolkit builder:
- `CreateUITKBoxForInstance(...)` creates the labels,
- `RefreshMonitoredValues()` loops over `rowBindings`,
- but nothing ever adds items to `rowBindings`.

That means this method runs on an empty list:

```csharp
private void RefreshMonitoredValues()
{
    foreach (var binding in rowBindings)
    {
        if (binding == null || binding.Handle == null || binding.ValueLabel == null)
            continue;

        binding.KeyLabel.text = binding.Handle.Name + ":";
        binding.ValueLabel.text = binding.Handle.GetValueString();
    }
}
```

So the UI looks correct at first, but the values stay frozen.

What this guide fixes
---------------------
You will make three changes:

1. Populate `rowBindings` while creating each row.
2. Clear `rowBindings` before rebuilding the UI.
3. Verify `Update()` keeps calling the refresh method.

Files involved
--------------
- [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- Optional reference: [Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs](Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs)

Step-by-step edits
------------------

1) Clear `rowBindings` before rebuilding the UI

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs).

Inside `OnEnable()`, before you start creating boxes, clear the list so you do not keep stale bindings from a previous enable/rebuild.

Add this line near the start of `OnEnable()` after you clear the root visual element:

```csharp
        rowBindings.Clear();
```

Recommended placement:

```csharp
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();
        rowBindings.Clear();
```

Why this matters:
- if the component is re-enabled,
- if the UI is rebuilt,
- or if the scene is reloaded,
- you want a fresh binding list.

2) Populate `rowBindings` inside `CreateUITKBoxForInstance(...)`

Still in [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs), find the `foreach (var handle in handles)` loop inside `CreateUITKBoxForInstance(...)`.

Right now it creates the row visuals, but it does not store them anywhere.

Replace that loop with this exact version:

```csharp
        foreach (var handle in handles)
        {
            var rowContainer = new VisualElement();
            rowContainer.AddToClassList("stat-row");

            var keyLabel = new Label(handle.Name + ":");
            keyLabel.AddToClassList("stat-label");

            var valueLabel = new Label(handle.GetValueString());
            valueLabel.AddToClassList("stat-value");

            rowContainer.Add(keyLabel);
            rowContainer.Add(valueLabel);
            statsContainer.Add(rowContainer);

            rowBindings.Add(new RowBinding
            {
                Handle = handle,
                KeyLabel = keyLabel,
                ValueLabel = valueLabel
            });
        }
```

What this does:
- `Handle` stores the live monitor handle,
- `KeyLabel` stores the name label,
- `ValueLabel` stores the value label.

That gives `RefreshMonitoredValues()` something real to update.

3) Keep `Update()` calling the refresh method

Your `Update()` method should continue to call `RefreshMonitoredValues()`.

Use this version:

```csharp
    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
        RefreshMonitoredValues();
    }
```

Do not remove the refresh call.
That call is what makes the values live.

4) Keep the refresh method simple

Your refresh method should stay as-is, or use this version if you want the copy-paste form:

```csharp
    private void RefreshMonitoredValues()
    {
        foreach (var binding in rowBindings)
        {
            if (binding == null || binding.Handle == null || binding.ValueLabel == null)
                continue;

            binding.KeyLabel.text = binding.Handle.Name + ":";
            binding.ValueLabel.text = binding.Handle.GetValueString();
        }
    }
```

This method is the bridge between the live registry and the UI.

5) Keep the nested `RowBinding` class

At the bottom of `DynamicColumnLayout`, keep the private nested class:

```csharp
    private sealed class RowBinding
    {
        public IMonitorHandle Handle;
        public Label ValueLabel;
        public Label KeyLabel;
    }
```

This is the small container that links a monitor handle to its labels.

Expected result
---------------
After these changes:
- every row stores its `IMonitorHandle`,
- `Update()` iterates over real bindings,
- `GetValueString()` gets called again and again,
- the displayed values change when the monitored values change.

Testing checklist
-----------------
1. Add the `rowBindings.Clear()` line in `OnEnable()`.
2. Add the `rowBindings.Add(new RowBinding { ... })` block inside the row creation loop.
3. Keep `RefreshMonitoredValues()` called from `Update()`.
4. Enter Play mode.
5. Change a monitored value, such as `currentHealth` in `MonitoredExample`.
6. Verify the UI value changes instead of staying frozen.

Common mistakes
---------------
- Clearing `rowBindings` after creating rows instead of before.
- Adding bindings only for one box instead of all rows.
- Creating labels but not storing them.
- Calling `RefreshMonitoredValues()` only once in `OnEnable()` instead of in `Update()`.

Important note
--------------
This fix only works if the rows are tied to the current live handles.
If a handle is reused or the monitored target is destroyed, the binding may become invalid. In that case, the refresh method will safely skip null entries.

If you want, the next guide can clean up the UIToolkit builder so it is easier to read after all these changes.
