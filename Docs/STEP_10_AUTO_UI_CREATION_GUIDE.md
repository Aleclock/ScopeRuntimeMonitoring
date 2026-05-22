# Step 10 — Auto-create Runtime UI on First Monitor (Detailed Guide)

Goal
----
Create the runtime UI (UIDocument + `DynamicColumnLayout`) automatically the first time any script calls `Monitor.StartMonitoring(...)`, with minimal scene setup and clear separation of responsibilities.

Summary of approach
-------------------
- Keep `Monitor` as the authoritative registry for monitored targets.
- On first successful `StartMonitoring(...)`, create a single `GameObject` named `RuntimeMonitorUI` and attach `DynamicColumnLayout` so the UI appears with zero scene setup.
- Provide a Resources fallback for `MonitorPanelSettings` so `DynamicColumnLayout` can find its configuration when instantiated at runtime.
- Keep `MonitoredExample` tiny: add lifecycle auto-registration (`OnEnable`/`OnDisable`).

Files referenced
----------------
- Global monitor: [Assets/Scope/RuntimeMonitoring/Monitor.cs](Assets/Scope/RuntimeMonitoring/Monitor.cs)
- UI builder: [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)
- Monitored example: [Assets/Scope/MonitoredExample.cs](Assets/Scope/MonitoredExample.cs)
- Settings asset: [Assets/Scope/Settings/MonitorPanelSettings.cs](Assets/Scope/Settings/MonitorPanelSettings.cs)

Why this pattern
-----------------
- Zero-config: designers can drop a `MonitoredExample` in a scene and the UI is created automatically when monitoring starts.
- Single builder: one `DynamicColumnLayout` instance controls all runtime UI.
- Minimal coupling: Monitor still only knows about creating the UI object, while `DynamicColumnLayout` owns presentation details and resolves its settings.

Exact edits to make (copy-paste)
--------------------------------

1) Add UI auto-creation to `Monitor.StartMonitoring` (static, zero-config approach)

Open [Assets/Scope/RuntimeMonitoring/Monitor.cs](Assets/Scope/RuntimeMonitoring/Monitor.cs) and replace or extend the `StartMonitoring` method with the following exact block. Leave other methods intact.

```csharp
public static void StartMonitoring(object target)
{
    Registry.RegisterTarget(target);

    // Auto-create a single runtime UI builder on first monitored target (zero-config)
    if (!s_uiCreated && Application.isPlaying)
    {
        s_uiCreated = true;

        // If a DynamicColumnLayout is already present, don't create another
        if (Object.FindObjectOfType<DynamicColumnLayout>() == null)
        {
            var go = new GameObject("RuntimeMonitorUI");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DynamicColumnLayout>();
        }
    }
}
```

Notes and safety checks:
- `s_uiCreated` should be a private static `bool` field on `Monitor` (add `private static bool s_uiCreated = false;` near the top of the class).
- `Application.isPlaying` ensures this only runs in Play mode.
- This approach creates the `GameObject` in the root of the scene and marks it `DontDestroyOnLoad` so the UI persists across scenes.

2) Make `DynamicColumnLayout` robust to being created at runtime (Resources fallback)

Because the new `RuntimeMonitorUI` may be created before you assign `panelSettings` in the inspector, add a small fallback in `DynamicColumnLayout.OnEnable()` to try loading a `MonitorPanelSettings` from `Resources` if the serialized `panelSettings` is `null`.

Add this near the top of `OnEnable()` (after the early `uidocument` logic and before resolving config):

```csharp
// Fallback: load MonitorPanelSettings from Resources if none assigned in inspector
if (panelSettings == null)
{
    var fallback = Resources.Load<MonitorPanelSettings>("MonitorPanelSettings");
    if (fallback != null)
        panelSettings = fallback;
}
```

Instructions to create the `Resources` asset:
- Move or duplicate your `MonitorPanelSettings` ScriptableObject asset into a folder named `Assets/Resources/` and name it `MonitorPanelSettings.asset` (Unity `Resources.Load` uses the filename without extension).

3) Auto-register monitored objects: `MonitoredExample` lifecycle calls

Open [Assets/Scope/MonitoredExample.cs](Assets/Scope/MonitoredExample.cs) and add `OnEnable`/`OnDisable` so the object registers itself when active. Replace the class body with (or insert these methods):

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

Why add this to each monitored object?
- The `[Monitor]` attribute marks members to be included by the registry, but `Monitor.StartMonitoring(target)` must be called to run discovery and add handles to `MonitoringRegistry`.
- Adding the two lifecycle methods keeps `MonitoredExample` very small and pushes the responsibility to the component itself.

4) Optional: only create UI when the registered target contributed handles

If you prefer to create the UI only when `RegisterTarget` actually added handles (i.e., the target had `[Monitor]` members), you can change `MonitoringRegistry.RegisterTarget` to return a `bool` indicating whether any handles were discovered. Then, in `Monitor.StartMonitoring`, only create the UI when `RegisterTarget` returns `true`.

- Edit `MonitoringRegistry.RegisterTarget` signature to `public bool RegisterTarget(object target)` and return `true` when `handles.Count > 0`, otherwise `false`.
- In `Monitor.StartMonitoring`, use the return value:

```csharp
if (Registry.RegisterTarget(target))
{
    // create UI
}
```

This is safer if you often call `StartMonitoring` on components that may not have `[Monitor]` members.

5) Clean-up guidance
--------------------
- If you previously had a `Bootstrap` or other scene object that calls `Monitor.StartMonitoring(...)`, remove that logic or ensure it doesn't double-register the same targets.
- If you prefer to keep explicit bootstrap control, skip step (1) and instead implement a small static `MonitorUIService` that subscribes to a `Monitor.TargetRegistered` event (not covered here since you requested the static approach).

Testing checklist
-----------------
- Create or ensure a `MonitorPanelSettings` asset exists in `Assets/Resources/MonitorPanelSettings.asset`.
- Place a `MonitoredExample` in an empty scene.
- Enter Play mode. Expected behavior:
  - `MonitoredExample.OnEnable` calls `Monitor.StartMonitoring(this)`.
  - `Monitor` auto-creates `RuntimeMonitorUI` with `DynamicColumnLayout` attached.
  - `DynamicColumnLayout` resolves `panelSettings` (via inspector or `Resources`) and builds the UI.

Debugging tips
--------------
- If nothing appears, inspect Console for messages from `MonitorPanelConfigResolver` (it logs missing references).
- Use `FindObjectOfType<DynamicColumnLayout>()` in the Editor console (runtime) to check whether the UI instance exists.
- If UI appears but boxes are placeholders, follow the steps in `Docs/STEP_9_LAZY_CREATION_GUIDE.md` to make the builder query `Monitor.Registry` for real handles and create boxes lazily.

Rollback steps
--------------
If you prefer to revert these changes later:
- Remove the `s_uiCreated` logic and the `GameObject` creation block from `Monitor.StartMonitoring`.
- Delete the `RuntimeMonitorUI` `GameObject` instances from scenes or stop them being created.
- Remove the `Resources` fallback if you added a `MonitorPanelSettings` into `Assets/Resources/`.

Notes on responsibilities
-------------------------
- This guide uses a pragmatic static approach for zero-config convenience. It trades a small amount of coupling (Monitor creates a UI GameObject) for simplicity. For larger projects, migrating to an event-driven UI service (Monitor raises an event; UI service listens and instantiates the prefab) preserves separation-of-concerns and is recommended.

If you want, I can also create a matching `STEP_11_MONITOR_EVENT_BOOTSTRAP_GUIDE.md` that shows the event-driven alternative with exact edits to `Monitor.cs`, `MonitoringRegistry.cs`, and a static `MonitorUIService` implementation. Tell me if you'd like that next.
