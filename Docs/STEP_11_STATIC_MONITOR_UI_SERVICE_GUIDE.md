# Step 11 — Static (non-Mono) MonitorUIService (Exact Edits)

Goal
----
Provide a zero-scene-footprint, static `MonitorUIService` that auto-creates the runtime UI (a single `DynamicColumnLayout` instance) when the first monitored target with handles is registered — without requiring any Mono `Bootstrap` objects in the scene.

Overview
--------
This guide shows the minimal, testable code changes you should make. The flow will be:

1. `MonitoringRegistry.RegisterTarget` returns `bool` indicating whether the target contributed handles.
2. `Monitor.StartMonitoring` calls `RegisterTarget` and invokes a `TargetRegistered` event only when `RegisterTarget` returned `true`.
3. `MonitorUIService` is a static class that subscribes to `Monitor.TargetRegistered` and instantiates the UI on the first invocation.

Why this pattern
-----------------
- No scene objects required (static service).
- Monitor remains responsible for monitoring lifecycle and only emits an event when something useful was registered.
- UI creation logic is centralized and non-Mono; the instantiated `DynamicColumnLayout` remains a normal component.

Exact edits (copy-paste)
------------------------

1) Change `MonitoringRegistry.RegisterTarget` to return `bool`

Open `Assets/Scope/RuntimeMonitoring/MonitoringRegistry.cs` and change the method signature and return semantics. Replace the existing `RegisterTarget` method with this exact implementation:

```csharp
public bool RegisterTarget(object target)
{
    if (target == null) return false;

    lock (_sync)
    {
        if (_targetHandles.ContainsKey(target))
            return _targetHandles[target].Count > 0; // already registered, return whether it has handles
    }

    var handles = new List<IMonitorHandle>();

    DiscoverFields(target, handles);
    DiscoverProperties(target, handles);

    lock (_sync)
    {
        if (handles.Count > 0)
        {
            _targetHandles[target] = handles;
            _handles.AddRange(handles);
            return true;
        }
        else
        {
            // Track target to avoid repeated discovery attempts
            _targetHandles[target] = handles;
            return false;
        }
    }
}
```

Notes:
- Update any callers of `RegisterTarget` if present. We will call it from `Monitor.StartMonitoring`.

2) Add an event to `Monitor` and invoke it only when `RegisterTarget` returns true

Open `Assets/Scope/RuntimeMonitoring/Monitor.cs` and update it as follows. Add the event and change `StartMonitoring` to use the new `RegisterTarget` return value.

Add at the top of the class (inside `Monitor`):

```csharp
public static event System.Action<object> TargetRegistered;
```

Then replace the body of `StartMonitoring` with this exact code:

```csharp
public static void StartMonitoring(object target)
{
    // RegisterTarget returns true only when the target contributed one or more handles.
    bool added = Registry.RegisterTarget(target);
    if (added)
    {
        TargetRegistered?.Invoke(target);
    }
}
```

Keep `StopMonitoring` unchanged.

3) Add the static `MonitorUIService` (non-Mono)

Create a new file at `Assets/Scope/RuntimeMonitoring/MonitorUIService.cs` with the exact contents below.

```csharp
using UnityEngine;

// Non-Mono static UI service that instantiates the runtime UI on first registered target
public static class MonitorUIService
{
    private static bool s_initialized = false;
    private static bool s_uiCreated = false;

    // Static constructor subscribes to the Monitor event when this class is first referenced.
    static MonitorUIService()
    {
        EnsureSubscribed();
    }

    // Public method to ensure the type is initialized from other code (e.g. Monitor static constructor)
    public static void EnsureSubscribed()
    {
        if (s_initialized) return;
        s_initialized = true;
        Monitor.TargetRegistered += OnTargetRegistered;
    }

    private static void OnTargetRegistered(object target)
    {
        if (s_uiCreated) return;

        // Try to instantiate a prefab named "RuntimeMonitorUILayout" from Resources first
        var prefab = Resources.Load<GameObject>("RuntimeMonitorUILayout");
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(go);
            s_uiCreated = true;
            return;
        }

        // Fallback: create a GameObject and attach DynamicColumnLayout
        var runtimeGo = new GameObject("RuntimeMonitorUI");
        Object.DontDestroyOnLoad(runtimeGo);
        runtimeGo.AddComponent<DynamicColumnLayout>();
        s_uiCreated = true;
    }
}
```

Notes:
- The `EnsureSubscribed` call in the static constructor registers the handler, but to guarantee subscription you will call `MonitorUIService.EnsureSubscribed()` from `Monitor` static constructor or from `StartMonitoring` before invoking the event. This avoids relying on the runtime to touch the `MonitorUIService` type implicitly.
- The service first attempts to load a prefab named `RuntimeMonitorUILayout` from `Assets/Resources/RuntimeMonitorUILayout.prefab`. If you want custom layout prefabs, put one there.

4) Ensure `Monitor` triggers `MonitorUIService` subscription on startup

To guarantee the `MonitorUIService` subscription exists, add a single call to `MonitorUIService.EnsureSubscribed()` in `Monitor` static constructor or in `StartMonitoring` before invoking the event. Example — add this call to the `Monitor` static constructor (inside `static Monitor()`):

```csharp
static Monitor()
{
    Registry = new MonitoringRegistry();
    MonitorUIService.EnsureSubscribed();
}
```

This keeps the coupling minimal: `Monitor` ensures the service is listening but the service controls UI creation.

5) Optional: `DynamicColumnLayout` Resources fallback (already covered in STEP_10)

If your `DynamicColumnLayout` expects a `MonitorPanelSettings` reference, add the `Resources.Load` fallback described in STEP_10 to handle the case when the prefab-less `DynamicColumnLayout` is created by the service.

Testing checklist
-----------------
- Create `Assets/Resources/RuntimeMonitorUILayout.prefab` if you prefer a prefab-based UI (optional). Otherwise, the service will create a plain `GameObject` and attach `DynamicColumnLayout`.
- Put an object with `[Monitor]` members (e.g. `MonitoredExample`) in the scene and add `OnEnable` lifecycle calls to `StartMonitoring`/`StopMonitoring` as in STEP_10.
- Enter Play mode. Expected:
  - `MonitoredExample.OnEnable` calls `Monitor.StartMonitoring(this)`.
  - `MonitoringRegistry.RegisterTarget` returns `true` (it found handles).
  - `Monitor` invokes `TargetRegistered` and the static `MonitorUIService` creates the UI (prefab or fallback).

Rollback
--------
- To revert to explicit bootstrapping, remove the `MonitorUIService.EnsureSubscribed()` call and delete `MonitorUIService.cs`. Re-introduce explicit `Bootstrap` objects that call `Monitor.StartMonitoring(...)` and create UI.

Notes and rationale
-------------------
- This variant keeps the UI auto-creation logic out of `Monitor.StartMonitoring` itself and places it in a single static service. `Monitor` only signals "a target with handles was registered" and the UI code decides how to instantiate.
- It preserves zero-scene-footprint while keeping concerns separated and easily testable.

Would you like me to also produce a patch file with these exact modifications, or will you apply them yourself? If you want a patch, I can generate `apply_patch` edits for the three files: `MonitoringRegistry.cs`, `Monitor.cs`, and a new `MonitorUIService.cs`.