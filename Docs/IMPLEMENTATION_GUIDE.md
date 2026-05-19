# Implementation Guide — Minimal Runtime + UI Integration

Goal
----
Provide a clear, step-by-step guide to implement a minimal runtime monitoring system (attribute-driven) and hook it into your existing `Scope` UI (`DebugManager` / `DebugPanelBase`). The guide assumes you want to learn and implement incrementally.

Why this order
--------------
Start with a tiny, reliable runtime: it produces monitor handles (name + value) that any UI can render. Once the runtime is stable, you can iterate on UI appearance without redoing core logic.

Prerequisites
-------------
- Unity 2020.3+ (the code uses reflection and runtime initialization patterns available across modern Unity versions)
- TextMeshPro package (your panels already use `TextMeshProUGUI`)
- Basic familiarity with C# and Unity scripting

Key concepts (keep these in mind)
--------------------------------
- `MonitorAttribute`: marks a field/property/method to be monitored.
- `IMonitorHandle`: lightweight runtime object that exposes `Name`, `Target`, and a `GetValueString()` method.
- `Monitor` (static): constructs and exposes a `Registry` and basic API `StartMonitoring(target)` / `StopMonitoring(target)`.
- `MonitoringRegistry`: stores `IMonitorHandle`s, supports registering targets, and provides `GetMonitorHandles()` for UIs.
- `ValueProcessor`: optional small component that formats values into strings.

High-level plan (what you will implement)
-----------------------------------------
1. Add a `MonitorAttribute` definition.
2. Add `IMonitorHandle` interface.
3. Add a tiny `Monitor` static entry point with `Registry` and `Start/StopMonitoring`.
4. Implement a simple `MonitoringRegistry` that holds handles and supports registering target instances.
5. Implement a `FieldMonitorHandle` that uses reflection to read field values.
6. Create a small example `MonitoredExample` class with a value to observe.
7. Modify one panel (or add a new panel) to query `Monitor.Registry.GetMonitorHandles()` and render one or more handles.
8. Test, iterate, and then expand (properties, methods, static, formatting, filters).

Full step-by-step instructions
------------------------------

Step 0 — Workspace prep

- Create a new folder for these learning files: `Assets/RuntimeMonitoring/` (optional).
- Open your project in the Unity Editor and keep the `Scope` panels available.

Step 1 — Create the `MonitorAttribute`

- Create a new file `MonitorAttribute.cs` under `Assets/RuntimeMonitoring/`.
- Implementation (minimal):

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

Notes: later you can add parameters like `format`, `tags`, `processorName` etc.

Step 2 — Define `IMonitorHandle`

- Create `IMonitorHandle.cs`.
- Minimal interface:

```csharp
public interface IMonitorHandle
{
    string Name { get; }
    object Target { get; }
    string GetValueString();
}
```

Step 3 — Add `Monitor` static entry point

- Create `Monitor.cs` (a tiny simplified version of the package's `Monitor`):

```csharp
using System.Collections.Generic;

public static class Monitor
{
    public static MonitoringRegistry Registry { get; private set; }

    static Monitor()
    {
        Registry = new MonitoringRegistry();
    }

    public static void StartMonitoring(object target)
    {
        Registry.RegisterTarget(target);
    }

    public static void StopMonitoring(object target)
    {
        Registry.UnregisterTarget(target);
    }
}
```

Step 4 — Implement `MonitoringRegistry`

- Create `MonitoringRegistry.cs`.
- Minimal responsibilities:
  - Maintain a list of `IMonitorHandle`s (both static and instance)
  - Provide `RegisterTarget(object target)` that inspects the target's type for `[Monitor]` fields and creates handles
  - `GetMonitorHandles()` returns IReadOnlyList<IMonitorHandle>

Minimal implementation idea:

```csharp
using System.Collections.Generic;
using System.Reflection;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();

    public IReadOnlyList<IMonitorHandle> GetMonitorHandles() => _handles;

    public void RegisterTarget(object target)
    {
        if (target == null) return;
        var t = target.GetType();
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var f in fields)
        {
            if (f.IsDefined(typeof(MonitorAttribute), true))
            {
                var handle = new FieldMonitorHandle(target, f);
                _handles.Add(handle);
            }
        }
    }

    public void UnregisterTarget(object target)
    {
        _handles.RemoveAll(h => h.Target == target);
    }
}
```

Notes: This is intentionally minimal. You can later add duplicate checks, static profiles, and caching.

Step 5 — Implement `FieldMonitorHandle`

- Create `FieldMonitorHandle.cs`.

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
        var val = _field.GetValue(Target);
        return val != null ? val.ToString() : "null";
    }
}
```

Step 6 — Create a test monitored class

- Create `MonitoredExample.cs`:

```csharp
using UnityEngine;

public class MonitoredExample : MonoBehaviour
{
    [Monitor("Player Health")]
    public float playerHealth = 100f;

    private void Update()
    {
        // change value so you can observe it
        playerHealth = Mathf.PingPong(Time.time * 10f, 100f);
    }
}
```

- Attach this component to a GameObject in a test scene.

Step 7 — Wire into `Scope` UI (read-only changes recommended by you)

You said you don't want code touched — the guide below explains how to change UI. Do these steps locally when ready.

Option A — Minimal quick integration (recommended for learning)

- Create a new debug panel `MonitorsPanel` that inherits `DebugPanelBase`.
- In `Initialize()` of `MonitorsPanel`, query `Monitor.Registry.GetMonitorHandles()` and create a `TextMeshProUGUI` for each handle. In `Update()` refresh the `text` with `GetValueString()`.

Pseudo-implementation sketch (inside `MonitorsPanel`):

```csharp
public class MonitorsPanel : DebugPanelBase
{
    private List<IMonitorHandle> handles = new List<IMonitorHandle>();
    private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    public override void Initialize()
    {
        CreatePanelBackground();
        handles.AddRange(Monitor.Registry.GetMonitorHandles());
        foreach (var h in handles)
        {
            var t = CreateTextField(h.Name, new Vector2(10, -10), 18, h.GetValueString(), 400, 24);
            texts.Add(t);
        }
    }

    private void Update()
    {
        for (int i = 0; i < handles.Count; i++)
        {
            texts[i].text = $"{handles[i].Name}: {handles[i].GetValueString()}";
        }
    }

    // Implement the abstract stubs
}
```

- Create the panel via `DebugManager.Instance.CreateDebugPanel<MonitorsPanel>()` (e.g., add a small initializer MonoBehaviour to the scene).

Option B — Incremental approach (safer; no UI changes at first)

1. Instead of changing `DebugPanelBase`, create a small `MonitorsPanel` file that coexists with current panels.
2. It will query `Monitor.Registry` and create its own `TextMeshProUGUI`s as children of the existing `DebugManager`'s canvas (similar to how `CreateDebugPanel<T>()` works).
3. This avoids touching `DebugPanelBase` at all and keeps your existing code safe.

Step 8 — Testing sequence (how to verify)

1. Add `MonitoredExample` to the scene on a GameObject.
2. Add a small `Bootstrap` MonoBehaviour in the scene which does:

```csharp
void Start()
{
    var example = FindObjectOfType<MonitoredExample>();
    Monitor.StartMonitoring(example);
    DebugManager.Instance.CreateDebugPanel<MonitorsPanel>().Initialize();
}
```

3. Enter Play mode — the `MonitorsPanel` should display `playerHealth` continuously changing.

Step 9 — Next improvements (after you confirm the minimal system works)

- Add property and method handles (mirror `FieldMonitorHandle` but use `PropertyInfo` or `MethodInfo.Invoke`).
- Add static member support (use `BindingFlags.Static` when profiling).
- Add a `ValueProcessor` pipeline: run values through type-specific formatter functions (floats with 1 decimal, Vector3 formatted columns, lists summarized). This is just a `Func<object,string>` map keyed by type.
- Add validation filters (show/hide) and tags in `MonitorAttribute`.
- Improve discovery: add a `MonitoringProfiler` that scans assemblies for `[Monitor]` at start and pre-creates profiles (similar to `MonitoringProfiler.cs` in the package). For now, manual registration via `StartMonitoring` is simplest.
- Replace the panel UI with UI Toolkit (for richer visuals) or style the existing panel (rounded boxes, icons, color-coded types).

Troubleshooting & tips
----------------------
- Reflection and IL2CPP: if you plan to build for IL2CPP/AOT, you’ll need to either use a code-gen step to produce concrete generic types or list types to preserve via `link.xml` or generator script. For learning and Editor testing this is not needed.
- Null references: ensure `Monitor.StartMonitoring` is called after `Monitor` static ctor ran. Using a `Bootstrap` MonoBehaviour's `Start()` is a reliable way.
- Performance: avoid reflection calls every frame. For `FieldMonitorHandle`, cache a delegate (e.g. compiled lambda) that reads the field faster — or call reflection only once per frame for all handles.
- Threading: keep everything on main thread for simplicity. The package’s profiler does async work — you can ignore async profiling for first iteration.

Files in this project to study (references)
------------------------------------------
- Package runtime: [Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Monitor.cs](Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Monitor.cs)
- Profiling: [Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringProfiler.cs](Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringProfiler.cs)
- Registry: [Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringRegistry.cs](Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringRegistry.cs)
- UI entry: [Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringDisplay.cs](Library/PackageCache/com.baracuda.runtime-monitoring@6d24d331f3/Runtime/Scripts/Core/Systems/MonitoringDisplay.cs)

Your existing Scope UI (use these as starting points):
- [Assets/Scope/DebugManager.cs](Assets/Scope/DebugManager.cs)
- [Assets/Scope/DebugPanelBase.cs](Assets/Scope/DebugPanelBase.cs)
- [Assets/Scope/FPSDebugPanel.cs](Assets/Scope/FPSDebugPanel.cs)

Learning checkpoints (what to verify)
-------------------------------------
- Checkpoint 1: `MonitoredExample` value appears in `MonitorsPanel` and updates.
- Checkpoint 2: Add one property monitor and verify value shows.
- Checkpoint 3: Add a `ValueProcessor` for floats (formatting) and verify formatted output.
- Checkpoint 4: Add toggles to the panel to hide/show groups or filter by name.

If you want a guided walk-through
--------------------------------
I can:
- Walk you through implementing Step 1–5 interactively (I'll show code snippets and explain each line). OR
- Provide small exercises and tests for each checkpoint so you learn incrementally.

Final notes
-----------
Start small: manually register one object and display one handle. When that’s comfortable, generalize to assembly profiling and UI polish.

Enjoy building — tell me which step you'd like to implement first and I will guide you interactively.
