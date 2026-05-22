# Step 18 — Create the UI on First `TargetRegistered`, Then Add Only New Boxes

Goal
----
Implement the flow you described:

- the first time `Monitor.TargetRegistered` fires, create the UIToolkit UI and the first box,
- every later time it fires, keep the UI alive and add only the new box,
- do not rebuild the full UI.

This is the best fit for a one-box-per-monitored-instance architecture.

Why this is the right model
---------------------------
You already decided that:
- each monitored script instance should get its own box,
- the box title should come from the instance,
- values should stay live,
- the UI should not be rebuilt every time a new object registers.

So the lifecycle should be:

1. First registered target arrives.
2. If the UI does not exist yet, create it.
3. Create the first box for that target.
4. Add the box to `columnContainer`.
5. For every later target, create only the new box and append it.

That is the cleanest and cheapest approach.

Files involved
--------------
- [Assets/Scope/RuntimeMonitoring/Monitor.cs](Assets/Scope/RuntimeMonitoring/Monitor.cs)
- [Assets/Scope/RuntimeMonitoring/MonitorUIService.cs](Assets/Scope/RuntimeMonitoring/MonitorUIService.cs)
- [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- [Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs](Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs)
- [Assets/Scope/MonitoredExample.cs](Assets/Scope/MonitoredExample.cs)

Important architecture idea
---------------------------
You need two separate checks:

- **UI exists?**
  - if no, create the `UIDocument` + `DynamicColumnLayout` object,
  - if yes, keep using it.

- **Box already exists for this target?**
  - if no, create one new box,
  - if yes, skip it.

That means the first event bootstraps the UI, and the later events append new boxes.

Step-by-step edits
------------------

1) Add a target-to-box lookup in `DynamicColumnLayout`

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs).

Add a dictionary field to track which target already has a box.

Use this exact field:

```csharp
    private readonly Dictionary<object, VisualElement> targetToBoxMap = new Dictionary<object, VisualElement>();
```

If you also want to avoid duplicates for destroyed/recreated targets, you can key by a more stable id later, but `object` is enough for the current version.

2) Keep `rowBindings` for live refresh

Keep the existing row binding list:

```csharp
    private readonly List<RowBinding> rowBindings = new List<RowBinding>();
```

This is still needed so `Update()` can refresh values.

3) Extract UI creation into a helper

Your UI creation should be split into a helper that only creates the UIToolkit root when needed.

Add a method like this:

```csharp
    private void EnsureUIExists()
    {
        if (uiDocument == null)
        {
            if (!TryGetComponent(out uiDocument))
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }
        }

        if (uiDocument.panelSettings == null && panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings.panelSettings;
        }
    }
```

If you already resolve panel settings through `MonitorPanelConfigResolver`, keep using that result. The important part is: do not rebuild the whole UI once it exists.

4) Add a helper that creates one box for one target

Create a helper that:
- checks if the target already has a box,
- if not, creates the box,
- stores it in `targetToBoxMap`,
- stores row bindings for live refresh.

Use this exact pattern:

```csharp
    private void AddBoxForTarget(object target)
    {
        if (target == null)
            return;

        if (targetToBoxMap.ContainsKey(target))
            return;

        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var handlesForTarget = allHandles.Where(handle => ReferenceEquals(handle.Target, target)).ToList();

        if (handlesForTarget.Count == 0)
            return;

        var title = GetDisplayNameForTarget(target);
        var boxElement = CreateUITKBoxForInstance(target, title, handlesForTarget);

        targetToBoxMap[target] = boxElement;
        columnContainer.Add(boxElement);
    }
```

This is the core of the behavior you described.

5) Change the first registration path to create the UI and the first box

Subscribe `DynamicColumnLayout` to `Monitor.TargetRegistered`.

Then make the event handler do this:
- if the UI does not exist yet, create it,
- if `columnContainer` does not exist yet, create it,
- add the first box for the target,
- on later events, only add the new box.

Use this event-handler pattern:

```csharp
    private void OnEnable()
    {
        Monitor.TargetRegistered += OnTargetRegistered;

        EnsureUIExists();
        BuildInitialLayoutFromExistingTargets();
    }

    private void OnDisable()
    {
        Monitor.TargetRegistered -= OnTargetRegistered;
    }

    private void OnTargetRegistered(object target)
    {
        EnsureUIExists();
        EnsureColumnContainer();
        AddBoxForTarget(target);
    }
```

6) Build the initial state from already-registered targets

When the UI first appears, there may already be monitored objects in the registry.

So add a helper that builds boxes for all currently registered targets once:

```csharp
    private void BuildInitialLayoutFromExistingTargets()
    {
        EnsureColumnContainer();

        var allHandles = Monitor.Registry.GetMonitorHandles();
        var existingTargets = allHandles
            .Select(handle => handle.Target)
            .Where(target => target != null)
            .Distinct()
            .ToList();

        foreach (var target in existingTargets)
        {
            AddBoxForTarget(target);
        }
    }
```

This gives you the startup boxes.

7) Add a helper to ensure `columnContainer` exists only once

Use a helper like this:

```csharp
    private void EnsureColumnContainer()
    {
        if (columnContainer != null)
            return;

        columnContainer = new VisualElement();
        columnContainer.style.height = Length.Percent(100);
        columnContainer.style.width = Length.Percent(100);
        columnContainer.style.backgroundColor = Color.clear;
        columnContainer.style.paddingTop = resolvedConfig.Padding;
        columnContainer.style.paddingBottom = resolvedConfig.Padding;
        columnContainer.style.paddingLeft = resolvedConfig.Padding;
        columnContainer.style.paddingRight = resolvedConfig.Padding;
        columnContainer.pickingMode = PickingMode.Ignore;

        ApplyLayoutAnchor(resolvedConfig.Anchor);

        var root = uiDocument.rootVisualElement;
        if (!root.Contains(columnContainer))
            root.Add(columnContainer);
    }
```

Important:
- create `columnContainer` once,
- add it to the root only once,
- keep appending boxes to it later.

8) Keep the box factory instance-based

Keep your existing `CreateUITKBoxForInstance(...)` helper, but make sure it also registers each row binding while creating rows.

The row creation block should still include this pattern:

```csharp
        rowBindings.Add(new RowBinding
        {
            Handle = handle,
            KeyLabel = keyLabel,
            ValueLabel = valueLabel
        });
```

That is what allows the live refresh loop to keep working.

9) Keep `Update()` only for live refresh and layout maintenance

`Update()` should not rebuild boxes.
It should only:
- refresh monitored values,
- optionally re-apply anchor/layout if needed.

Use this shape:

```csharp
    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
        RefreshMonitoredValues();
    }
```

10) Make sure `Monitor` only signals registrations

In [Assets/Scope/RuntimeMonitoring/Monitor.cs](Assets/Scope/RuntimeMonitoring/Monitor.cs), keep the `TargetRegistered` event.

The important part is:
- `Monitor` notifies that something new was registered,
- `DynamicColumnLayout` decides whether to add a box,
- `DynamicColumnLayout` does not rebuild everything.

That separation keeps the code clean.

Example flow at runtime
-----------------------
First target registers:
- `Monitor.StartMonitoring(target)` is called,
- `Monitor.TargetRegistered` fires,
- `DynamicColumnLayout` creates the UI if needed,
- `DynamicColumnLayout` creates the first box,
- box gets added to `columnContainer`.

Second target registers:
- `Monitor.StartMonitoring(target2)` is called,
- `Monitor.TargetRegistered` fires again,
- UI already exists,
- `DynamicColumnLayout` only creates the new box,
- the new box is appended to `columnContainer`.

What not to do
--------------
Do not rebuild the whole UIToolkit panel every time a target registers.
That will work, but it is wasteful and makes the state management harder.

Do not forget the duplicate check.
If you skip `targetToBoxMap.ContainsKey(target)`, the same target can create multiple boxes.

Do not forget row bindings.
If you create the box but do not add the `RowBinding` objects, live refresh will stop working.

Testing checklist
-----------------
1. Start with two monitored objects already in the scene.
2. Enter Play mode.
3. Verify the first object creates the UI and the first box.
4. Verify the second object adds a second box instead of rebuilding everything.
5. Add or enable a third monitored object at runtime if possible.
6. Verify it appends only one new box.
7. Change a monitored value and verify the label updates.

If you still see only one box
-----------------------------
Check these first:
- `DynamicColumnLayout` is subscribed to `Monitor.TargetRegistered`.
- `targetToBoxMap` is being populated.
- `AddBoxForTarget(target)` is called for every target.
- `Monitor.Registry.GetMonitorHandles()` contains handles for both targets.

If `Monitor.Registry` contains both targets but the UI still shows one box, the problem is almost always in the target-to-box lookup or the event subscription.

Next step
---------
After this change, the next cleanup guide should simplify the builder further so the box-adding path is easier to read and debug.
