# Step 14 — Add Live Refresh to the UIToolkit Builder

Goal
----
Make the UIToolkit runtime panel update values over time instead of showing a one-time snapshot.

Why the values look static now
------------------------------
The UIToolkit builder currently creates each label using `handle.GetValueString()` once, at the moment the box is built.

That means:
- the box appears correctly,
- the initial values are visible,
- but the labels do not update unless you explicitly refresh them.

This guide shows the exact way to store the label references so you can re-read the monitor values on a timer.

Files to edit
-------------
- [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- Optional reference: [Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs](Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs)

The core idea
-------------
Instead of building a label once and forgetting it, you keep a small record for each row:
- the `IMonitorHandle` it belongs to,
- the `Label` that displays the value.

Then, in `Update()`, you loop through those records and assign:

```csharp
label.text = $"{handle.Name}: {handle.GetValueString()}";
```

That is the live refresh.

Step-by-step edits
------------------

1) Add a small row binding type

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs) and add this private nested class near the top of `DynamicColumnLayout`:

```csharp
    private sealed class RowBinding
    {
        public IMonitorHandle Handle;
        public Label ValueLabel;
        public Label KeyLabel;
    }
```

This is just a small holder for the runtime row state.

2) Replace the old static label list with row bindings

If your current class contains something like this:

```csharp
private readonly List<Label> dynamicValueLabels = new List<Label>();
```

replace it with:

```csharp
private readonly List<RowBinding> rowBindings = new List<RowBinding>();
```

If you also have demo-only fields like `numberOfBoxes` or `RandomizeFields()`, those should be removed as part of the monitored-box conversion from STEP 12.

3) Update the box creation helper to store bindings

In the helper that creates the UIToolkit box, when you create each row, store the `IMonitorHandle` and the `Label` reference.

Replace the row-creation portion with this pattern:

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

Important:
- `rowBindings` must be a field on the class, not a local variable, so `Update()` can use it later.
- If you create several boxes, the binding list should include rows from all of them.

4) Add a refresh method

Add this method to `DynamicColumnLayout`:

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

This method re-reads the current value from the registry handle and updates the label text.

5) Call the refresh method from `Update()`

Replace your current `Update()` method with this version:

```csharp
    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
        RefreshMonitoredValues();
    }
```

This will refresh every frame.

If every-frame refresh is too expensive, you can later add a timer and only call `RefreshMonitoredValues()` every `0.1f`, `0.25f`, or `0.5f` seconds.

6) Keep `GetValueString()` as the source of truth

Do not cache the string values yourself unless you have a performance reason.
Let the handle calculate the current value each refresh.

That means the live update source stays simple:
- `FieldMonitorHandle.GetValueString()`
- `PropertyMonitorHandle.GetValueString()`
- `ValueFormatter.FormatValue(...)`

7) Optional timer version

If you want a timer instead of every-frame updates, use this pattern:

```csharp
    [SerializeField] private float refreshInterval = 0.25f;
    private float refreshTimer = 0f;

    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshMonitoredValues();
        }
    }
```

This is usually a better default than refreshing every single frame.

What live refresh means in practice
-----------------------------------
After this change:
- the box contents are still created from the registry,
- the labels now re-read the current values,
- changing `currentHealth` in `MonitoredExample` will update the displayed value in the UI.

Testing checklist
-----------------
1. Confirm `DynamicColumnLayout` is creating rows from `IMonitorHandle` objects.
2. Add the row-binding storage.
3. Enter Play mode.
4. Change a monitored value, for example `currentHealth`.
5. Verify the displayed value changes in the UI.

Common mistakes
---------------
- Only setting the label once in the box creation method and never refreshing it.
- Storing only `string` values instead of `IMonitorHandle` references.
- Refreshing a local list that is not accessible from `Update()`.

Simple mental model
-------------------
Think of it like this:
- creation step = build the row and keep a pointer to the data source,
- refresh step = ask the data source for the current value again.

If you want, the next guide can show the exact `DynamicColumnLayout` shape after the monitored-box conversion and live refresh are both applied together, so you can copy one complete version instead of assembling the pieces manually.
