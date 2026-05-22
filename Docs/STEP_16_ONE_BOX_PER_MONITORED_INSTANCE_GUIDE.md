# Step 16 — Create One Box Per Monitored Script Instance

Goal
----
Change the runtime UI so each monitored script instance gets its own box.

This is the model you described:
- one `MonitoredExample` instance = one box,
- another `MonitoredExample` instance = another box,
- the box title should come from the instance, not from a hardcoded static definition.

Why this is needed
------------------
Right now the runtime UI is still using a static box definition model.
That means:
- one fixed definition like `Gameplay` is created,
- every monitored instance contributes rows into that same box,
- multiple script instances do **not** automatically become separate boxes.

That works for a demo, but it is not the model you want.

What should drive the box identity
----------------------------------
For instance-based UI, the runtime box identity should come from the monitored object itself, for example:
- the component type name,
- the GameObject name,
- a custom display name,
- or a combination of the above.

A good default is:
- box `Id` = instance id or stable unique key,
- box `Title` = GameObject name + component type, or just GameObject name if that is enough.

Example titles:
- `Player`
- `Enemy_01`
- `Enemy_02`
- `Boss`

What changes conceptually
------------------------
Instead of this:
- `DefaultBoxDefinitions.Create()` returns a single static `Gameplay` box,
- `DynamicColumnLayout` creates that box once,
- the box shows all matching handles.

You want this:
- find each registered monitored target,
- create one box for that target,
- use that target's name as the title,
- render only the handles that belong to that specific target.

Files involved
--------------
- [Assets/Scope/RuntimeMonitoring/MonitoringRegistry.cs](Assets/Scope/RuntimeMonitoring/MonitoringRegistry.cs)
- [Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs](Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs)
- [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- [Assets/Scope/MonitoredExample.cs](Assets/Scope/MonitoredExample.cs)
- Optional: [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](Assets/Scope/UIFramework/DefaultBoxDefinitions.cs)

Step-by-step edits
------------------

1) Give each handle a way to identify its source instance

Open [Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs](Assets/Scope/RuntimeMonitoring/IMonitorHandle.cs).

The interface already exposes `Target`, which is good. That means each handle already knows which object it belongs to.

Use that to group rows by monitored instance.

If needed, the handle `Target` can be used to derive:
- instance display name,
- stable grouping key,
- title for the box.

2) Build boxes from monitored targets instead of static definitions

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs).

Change the creation logic so it no longer asks a static `DefaultBoxDefinitions.Create()` list for the main box identity.

Instead:
- get all handles from `Monitor.Registry.GetMonitorHandles()`,
- group the handles by `handle.Target`,
- create one box per unique target,
- use the target to build the box title.

The algorithm should be conceptually like this:

```csharp
var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
var handlesByTarget = allHandles
    .GroupBy(handle => handle.Target)
    .ToList();

foreach (var group in handlesByTarget)
{
    var target = group.Key;
    var handlesForTarget = group.ToList();
    var boxTitle = GetDisplayNameForTarget(target);

    var boxElement = CreateUITKBoxForInstance(target, boxTitle, handlesForTarget);
    columnContainer.Add(boxElement);
}
```

3) Add a helper that creates a title from the monitored instance

Add a helper method like this to `DynamicColumnLayout`:

```csharp
    private string GetDisplayNameForTarget(object target)
    {
        if (target == null)
            return "Unknown";

        if (target is UnityEngine.Component component && component.gameObject != null)
            return component.gameObject.name;

        return target.GetType().Name;
    }
```

This gives you a useful default title:
- if the monitored object is a Unity component, use the GameObject name,
- otherwise fall back to the type name.

4) Add a box factory for one monitored instance

Replace the static-definition box factory with an instance-based version.

Use a method like this:

```csharp
    private VisualElement CreateUITKBoxForInstance(object target, string title, IReadOnlyList<IMonitorHandle> handles)
    {
        var box = new VisualElement();
        box.AddToClassList("custom-box");
        box.pickingMode = PickingMode.Position;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("box-header-row");

        var titleLabel = new Label(title);
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

5) Remove the static `Gameplay` box path from the builder

If `DynamicColumnLayout` still reads `DefaultBoxDefinitions.Create()` for the main runtime boxes, remove that dependency from the box creation path.

`DefaultBoxDefinitions` can either:
- be deleted later,
- or be kept only as a template/reference for older uGUI code.

But for the one-box-per-instance model, it should no longer be the thing that decides the actual runtime box identity.

6) Keep the row values live

If you already added the live-refresh guide from STEP 14, reuse that same approach here.

That means each row should still store:
- the `IMonitorHandle`,
- the `Label` showing the value.

Then the refresh loop should re-read `handle.GetValueString()` for each row, so each instance box stays live.

7) Auto-register each monitored script instance

Open [Assets/Scope/MonitoredExample.cs](Assets/Scope/MonitoredExample.cs).

Make sure each instance registers itself, for example:

```csharp
    private void OnEnable()
    {
        Monitor.StartMonitoring(this);
    }

    private void OnDisable()
    {
        Monitor.StopMonitoring(this);
    }
```

This is what makes each instance show up as its own runtime box source.

How this fits together
----------------------
- `MonitoredExample` tells the registry: "I exist; inspect my `[Monitor]` members."
- `MonitoringRegistry` stores handles for each target instance.
- `DynamicColumnLayout` groups handles by target instance.
- One target instance becomes one box.
- The box title comes from the target, not from a static definition.

Important distinction
---------------------
This is different from grouping by name category.

- Category grouping means many objects can contribute to one box.
- Instance grouping means each object gets its own box.

You want instance grouping.

Testing checklist
-----------------
1. Add two `MonitoredExample` components to the scene.
2. Make sure both call `Monitor.StartMonitoring(this)` on enable.
3. Enter Play mode.
4. Verify you get two separate boxes.
5. Verify the titles are based on the instance, not hardcoded as `Gameplay`.
6. Verify each box shows only the values from its own instance.

If you still see one shared box
-------------------------------
That means the builder is still using the static definition path somewhere.
Check for:
- `DefaultBoxDefinitions.Create()` still being used in `DynamicColumnLayout`,
- a box factory that creates boxes from a hardcoded list,
- grouping logic that ignores `handle.Target`.

Recommended next step
---------------------
After this change, the next useful cleanup is to remove or downgrade `DefaultBoxDefinitions` so it is no longer confusing future work. The real runtime identity should come from monitored instances, not from a hardcoded `Gameplay` template.
