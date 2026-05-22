# Step 12 — Replace Placeholder Boxes with Monitored Boxes

Goal
----
Replace the current demo loop in `DynamicColumnLayout` that creates `Box #1`, `Box #2`, random rows, and `---` values with real boxes generated from the runtime monitor registry.

After this step, the UI builder will:
- read the registered monitored handles from `Monitor.Registry`,
- group them into box definitions,
- create a box only when that box has at least one monitored handle,
- populate each box with the real monitored names and values.

What this step assumes
----------------------
This guide assumes you already completed the earlier steps:
- automatic creation of the runtime UI object on first `Monitor.StartMonitoring(...)`,
- `DynamicColumnLayout` can resolve `MonitorPanelSettings`,
- `MonitoredExample` (or other monitored components) call `Monitor.StartMonitoring(this)` on enable.

The key idea
------------
Right now `DynamicColumnLayout` is behaving like a demo panel. It is still generating placeholder boxes with random values. We want to switch it to a data-driven builder.

Instead of this:
- `numberOfBoxes = 12`
- `for (int i = 0; i < numberOfBoxes; i++)`
- random rows
- `---` labels

we want this:
- get all handles from `Monitor.Registry.GetMonitorHandles()`
- use your box definitions to decide which handles belong together
- build a box only when the definition actually has data
- render the live values from each `IMonitorHandle`

Files involved
--------------
- Builder: [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- Registry: [Assets/Scope/RuntimeMonitoring/Monitor.cs](Assets/Scope/RuntimeMonitoring/Monitor.cs)
- Handle source: [Assets/Scope/RuntimeMonitoring/MonitoringRegistry.cs](Assets/Scope/RuntimeMonitoring/MonitoringRegistry.cs)
- Existing grouping reference: [Assets/Scope/UIFramework/MonitorPanelController.cs](Assets/Scope/UIFramework/MonitorPanelController.cs)
- Box definitions: your existing `DefaultBoxDefinitions` / `MonitorBoxDefinition` code

How the new flow should work
----------------------------
1. `DynamicColumnLayout.OnEnable()` resolves settings and creates the UI root.
2. It gets a snapshot of all handles from `Monitor.Registry.GetMonitorHandles()`.
3. It loads the list of box definitions you already use elsewhere in the project.
4. For each definition:
   - collect the handles that match that definition,
   - skip the box if no handles matched,
   - create a `VisualElement` box,
   - add one row per handle,
   - bind the row label to `handle.Name`,
   - bind the value label to `handle.GetValueString()`.
5. Add the finished box to `columnContainer`.

Step-by-step edits
------------------

1) Remove the placeholder box generation block

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs) and find the block that starts at the comment:

```csharp
// Generate boxes
for (int i = 0; i < numberOfBoxes; i++)
{
```

Delete that entire loop, including the random row generation and the placeholder `---` labels.

Also delete or stop using these fields if they are now only for the demo loop:

```csharp
private readonly List<Label> dynamicValueLabels = new List<Label>();
private int numberOfBoxes = 12;
```

If `dynamicValueLabels` is no longer needed for the real monitored-box version, remove it too.

2) Add the registry-driven box creation block

In the same `OnEnable()` method, replace the demo loop with the following exact block.

Insert this after `ApplyLayoutAnchor(resolvedConfig.Anchor);` and before `root.Add(columnContainer);`:

```csharp
        // Build monitored boxes from the live registry instead of demo placeholders
        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var definitions = DefaultBoxDefinitions.Create();

        foreach (var definition in definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            if (handlesForBox == null || handlesForBox.Count == 0)
                continue;

            var boxElement = CreateUITKBoxForDefinition(definition, handlesForBox);
            columnContainer.Add(boxElement);
        }
```

Important:
- `IMonitorHandle`, `DefaultBoxDefinitions`, and `MonitorBoxDefinition` must already exist in your project.
- If `DefaultBoxDefinitions.Create()` returns a different type in your codebase, adjust only that line; keep the rest of the pattern the same.

3) Add the helper that groups handles for a box

Below the class methods, add a helper that matches the same logic used by your existing `MonitorPanelController`.

Use this exact method:

```csharp
    private IReadOnlyList<IMonitorHandle> BuildHandleListForBox(List<IMonitorHandle> source, MonitorBoxDefinition definition)
    {
        var filtered = new List<IMonitorHandle>();

        foreach (var handle in source)
        {
            if (definition.FilterRule == null || definition.FilterRule.Include(handle))
            {
                filtered.Add(handle);
            }
        }

        if (definition.SortRule != null)
        {
            filtered.Sort(definition.SortRule.Compare);
        }

        return filtered;
    }
```

If your actual filter/sort API has slightly different method names, keep the structure and substitute the correct members. The important part is:
- filter the handles,
- sort them if needed,
- return only the handles that belong in that box.

4) Add the helper that builds one UIToolkit box

Still below the class methods, add a helper that creates the `VisualElement` box using the real monitored data.

Use this exact method:

```csharp
    private VisualElement CreateUITKBoxForDefinition(MonitorBoxDefinition definition, IReadOnlyList<IMonitorHandle> handles)
    {
        var box = new VisualElement();
        box.AddToClassList("custom-box");
        box.pickingMode = PickingMode.Position;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("box-header-row");

        var titleLabel = new Label(definition.Title);
        titleLabel.AddToClassList("box-header");
        headerRow.Add(titleLabel);

        var toggleButton = new Button() { text = "−" };
        toggleButton.AddToClassList("collapse-btn");
        headerRow.Add(toggleButton);

        box.Add(headerRow);

        var statsContainer = new VisualElement();
        statsContainer.AddToClassList("stats-content-holder");
        statsContainer.pickingMode = PickingMode.Ignore;

        foreach (var handle in handles)
        {
            var rowContainer = new VisualElement();
            rowContainer.AddToClassList("stat-row");

            var keyLabel = new Label(handle.Name + ":");
            keyLabel.AddToClassList("stat-label");

            var valueLabel = new Label(handle.GetValueString());
            valueLabel.AddToClassList("stat-value");
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            rowContainer.Add(keyLabel);
            rowContainer.Add(valueLabel);
            statsContainer.Add(rowContainer);
        }

        toggleButton.clicked += () =>
        {
            statsContainer.ToggleInClassList("stats-content-holder--collapsed");
            toggleButton.text = statsContainer.ClassListContains("stats-content-holder--collapsed") ? "+" : "−";
        };

        box.Add(statsContainer);
        return box;
    }
```

This is the main change that turns the builder from demo mode into real monitored-data mode.

5) Keep the update loop, but remove the random demo behavior

Your current `Update()` method can stay, but it should no longer call `RandomizeFields()` if there are no placeholder labels.

Replace the current `Update()` method with this simplified version:

```csharp
    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
    }
```

If later you want live-refresh behaviour for values that change every frame, add a method that walks the existing value labels and calls `handle.GetValueString()` again. For now, this step only replaces the placeholder box creation with real monitored box creation.

6) Remove the old random-value helper

Delete `RandomizeFields()` completely if it is only used by the demo loop.

Also remove any fields that only supported the placeholder demo, such as:

```csharp
private readonly List<Label> dynamicValueLabels = new List<Label>();
private float updateTimer = 0f;
private float updateInterval = 1f;
private int numberOfBoxes = 12;
```

If you still need `updateInterval` for later refresh logic, keep it. If not, remove it now to keep the file clean.

Expected result
---------------
After these edits:
- if `MonitoredExample` is registered, its `[Monitor]` members show up in the registry,
- `DynamicColumnLayout` reads those handles and creates real boxes,
- no more `Box #1`, `Box #2`, or random placeholder values,
- the UI only shows actual monitored content.

Testing checklist
-----------------
1. Confirm `MonitoredExample` is registering itself with `Monitor.StartMonitoring(this)` in `OnEnable()`.
2. Enter Play mode.
3. Verify the UI appears.
4. Verify the box titles and rows match your real monitored definitions, not the placeholder demo boxes.
5. Verify changing `currentHealth` updates the displayed value if your refresh logic re-queries handle values.

Troubleshooting
---------------
- If the UI still looks like a demo panel, you probably missed the placeholder loop and the `RandomizeFields()` path is still running.
- If no boxes appear, check whether `Monitor.Registry.GetMonitorHandles()` is empty. That usually means the target was not registered or the `[Monitor]` members were not discovered.
- If box titles appear but row values are empty, verify that `handle.GetValueString()` is implemented correctly in your handle classes.

Optional next step
------------------
After this step, a useful follow-up is to make values refresh on a timer or on demand so the UI stays live instead of only showing the initial snapshot. If you want that, the next guide should add a small value refresh loop that re-reads `GetValueString()` for each existing row.
