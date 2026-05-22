# Step 9 — Implement Lazy Box Creation (Guided Code Changes)

Goal
----
Make panels create boxes only when there are actual monitors to show. Add a runtime toggle in the shared settings so monitoring can be turned on/off for the project.

High-level approach
-------------------
- Add `enableRuntimeMonitoring` flag to `MonitorPanelSettings` (project-wide toggle).
- Ensure per-panel overrides can express `0` boxes (0 = auto).
- Add `NumberOfBoxes` to the resolved `MonitorPanelConfig` so the builder can decide.
- In `DynamicColumnLayout.OnEnable()`:
  - If runtime monitoring is disabled -> early exit (do nothing).
  - Query `Monitor.Registry.GetMonitorHandles()` and the existing `DefaultBoxDefinitions.Create()` set.
  - For each definition, build the handle list (reuse `MonitorPanelController` logic) and only create a UIToolkit `VisualElement` box for definitions that have at least one handle.
  - If no boxes are required, don't add any children (container can remain empty) and optionally disable the script.

Files to read before editing
---------------------------
- `Assets/Scope/Settings/MonitorPanelSettings.cs` ([open file](Assets/Scope/Settings/MonitorPanelSettings.cs#L1))
- `Assets/Scope/Settings/MonitorPanelOverrides.cs` ([open file](Assets/Scope/Settings/MonitorPanelOverrides.cs#L1))
- `Assets/Scope/Settings/MonitorPanelConfigResolver.cs` ([open file](Assets/Scope/Settings/MonitorPanelConfigResolver.cs#L1))
- `Assets/Scope/DynamicColumnLayout.cs` ([open file](Assets/Scope/DynamicColumnLayout.cs#L1))
- `Assets/Scope/UIFramework/MonitorPanelController.cs` ([open file](Assets/Scope/UIFramework/MonitorPanelController.cs#L1)) — used as reference for grouping/filtering logic

Step-by-step edits (copy-paste)
--------------------------------

1) Add project toggle: `MonitorPanelSettings`

Open `Assets/Scope/Settings/MonitorPanelSettings.cs` and add the `enableRuntimeMonitoring` boolean under the `Runtime Defaults` header.

Replace the `Runtime Defaults` block with this snippet (exact):

```csharp
    [Header("Runtime Defaults")]
    public MonitorPanelAnchor defaultAnchor = MonitorPanelAnchor.TopLeft;
    [Min(0.05f)] public float defaultUpdateInterval = 0.5f;
    [Min(0f)] public float defaultPadding = 15f;
    [Header("Runtime Control")]
    public bool enableRuntimeMonitoring = true; // toggle runtime monitoring for the project
```

2) Add per-panel enable and specific-box filter

Instead of a manual box count, add two per-panel options so each panel can:

- explicitly opt-in/out of runtime monitoring (`EnableRuntimePanel`), and
- optionally restrict which box definitions it instantiates (`SpecificBoxIds`).

Open `Assets/Scope/Settings/MonitorPanelOverrides.cs` and add these fields under the `Override Switches` and `Per-Panel Values` sections. Replace or add the following exact declarations:

```csharp
    [Header("Override Switches")]
    [SerializeField] private bool enableRuntimePanel = true; // per-panel on/off
    [SerializeField] private bool overridePanelSettings;
    [SerializeField] private bool overrideBoxStyleSheet;
    [SerializeField] private bool overrideAnchor;
    [SerializeField] private bool overrideUpdateInterval;
    [SerializeField] private bool overridePadding;
    [SerializeField] private bool overrideSpecificBoxFilter;

    [Header("Per-Panel Values")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private StyleSheet boxStyleSheet;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;
    [SerializeField, Min(0.05f)] private float updateInterval = 1f;
    [SerializeField, Min(0f)] private float padding = 15f;
    [SerializeField] private bool useSpecificBoxFilter = false;
    [SerializeField] private string[] specificBoxIds = new string[0];

    // Add public accessors for the new fields
    public bool EnableRuntimePanel => enableRuntimePanel;
    public bool UseSpecificBoxFilter => useSpecificBoxFilter;
    public string[] SpecificBoxIds => specificBoxIds;
```

This keeps runtime behavior fully automatic while letting you opt individual panels out or restrict them to named box definitions.

3) Add per-box overrides: `MonitorBoxOverrides` (new)

To support overrides at the single-box level (what you called "panel" but is actually a single box instance), add a small `MonitorBoxOverrides` MonoBehaviour you can attach to a box prefab or scene GameObject. This lets you tune one box's behaviour (enable/disable, update interval, style) without modifying global or panel defaults.

Create `Assets/Scope/Settings/MonitorBoxOverrides.cs` with this exact content (already provided in this repo as a reference implementation):

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MonitorBoxOverrides : MonoBehaviour
{
    [Header("Box Overrides")]
    [SerializeField] private bool enableRuntimeBox = true;

    [Header("Optional Overrides")]
    [SerializeField] private bool overrideUpdateInterval = false;
    [SerializeField, Min(0.01f)] private float updateInterval = 1f;

    [SerializeField] private bool overrideBoxStyleSheet = false;
    [SerializeField] private StyleSheet boxStyleSheet;

    [Header("Identification")]
    [SerializeField] private string boxId = ""; // optional identifier for this box instance

    public bool EnableRuntimeBox => enableRuntimeBox;
    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public float UpdateInterval => updateInterval;
    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public StyleSheet BoxStyleSheet => boxStyleSheet;
    public string BoxId => boxId;
}
```

How to use it in the builder
-----------------------------
Attach `MonitorBoxOverrides` to a box prefab or an empty GameObject in the scene and set `BoxId` to match the `MonitorBoxDefinition.Id` you want to target. At runtime the UIToolkit builder can look up box overrides and apply them when instantiating a VisualElement box.

Example snippet to apply overrides inside the box creation loop (merge this into the `OnEnable()` snippet above):

```csharp
    // cache overrides once
    var boxOverrides = Object.FindObjectsOfType<MonitorBoxOverrides>();

    foreach (var definition in definitions)
    {
        var handlesForBox = BuildHandleListForBox(allHandles, definition);
        if (handlesForBox == null || handlesForBox.Count == 0)
            continue;

        // find a matching per-box override by BoxId
        var overrideComp = boxOverrides.FirstOrDefault(b => !string.IsNullOrEmpty(b.BoxId) && b.BoxId == definition.Id);
        if (overrideComp != null && !overrideComp.EnableRuntimeBox)
            continue; // explicitly disabled

        var boxElement = CreateUITKBoxForDefinition(definition, handlesForBox);

        // apply style override if requested
        if (overrideComp != null && overrideComp.OverrideBoxStyleSheet && overrideComp.BoxStyleSheet != null)
        {
            boxElement.styleSheets.Add(overrideComp.BoxStyleSheet);
        }

        // optionally change update interval on the instantiated box's update logic
        if (overrideComp != null && overrideComp.OverrideUpdateInterval)
        {
            // apply the interval to whatever updater is used (example only)
            // boxUpdater.SetInterval(overrideComp.UpdateInterval);
        }

        columnContainer.Add(boxElement);
    }
```

This approach gives you three layers of configuration:

- **Global**: `MonitorPanelSettings` — project defaults and master runtime toggle.
- **Panel**: `MonitorPanelOverrides` — per-container overrides and filters.
- **Box**: `MonitorBoxOverrides` — per-single-box override attached to a prefab or GameObject.

Use panel-level filters for coarse selection and `MonitorBoxOverrides` for per-box tweaks.

4) Update `DynamicColumnLayout` to auto-build definitions from monitors

Open `Assets/Scope/DynamicColumnLayout.cs` and replace the current box generation section in `OnEnable()` with the following logic. Keep the top-of-file fields and config resolution as-is, but after you assign `resolvedConfig` and `uiDocument.panelSettings` add the new block.

Replace the old "Generate boxes" loop with this exact code snippet (copy-paste):

```csharp
        // Determine desired box definitions from runtime monitors
        if (!panelSettings.enableRuntimeMonitoring)
        {
            // Monitoring globally disabled
            if (panelSettings.logMissingReferences)
                Debug.Log("DynamicColumnLayout: runtime monitoring disabled in settings.", this);
            return;
        }

        // Get all monitor handles from the registry
        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());

        // Get default definitions and compute which ones have handles
        var definitions = DefaultBoxDefinitions.Create();
        var placementManager = new BoxPlacementManager();

        var handlesByBoxId = new Dictionary<string, IReadOnlyList<IMonitorHandle>>();

        foreach (var definition in definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            handlesByBoxId[definition.Id] = handlesForBox;
            // Only create a box if there are any handles to display
            if (handlesForBox != null && handlesForBox.Count > 0)
            {
                // Create a UIToolkit box element for this definition
                var boxElement = CreateUITKBoxForDefinition(definition, handlesForBox);
                columnContainer.Add(boxElement);
            }
        }

        // If nothing was added, we can safely disable this component (optional)
        if (columnContainer.childCount == 0)
        {
            enabled = false; // no monitors — turn off builder
            return;
        }
```

Below the class, add these helper methods (exact copy):

```csharp
    // Reuse MonitorPanelController matching logic
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

    // Build a simple VisualElement that mirrors the boxed layout (title + collapsible content)
    private VisualElement CreateUITKBoxForDefinition(MonitorBoxDefinition definition, IReadOnlyList<IMonitorHandle> handles)
    {
        var box = new VisualElement();
        box.AddToClassList("custom-box");

        var header = new VisualElement();
        header.AddToClassList("box-header-row");

        var title = new Label(definition.Title);
        title.AddToClassList("box-header");
        header.Add(title);

        var toggle = new Button() { text = "−" };
        toggle.AddToClassList("collapse-btn");
        header.Add(toggle);

        box.Add(header);

        var stats = new VisualElement();
        stats.AddToClassList("stats-content-holder");
        stats.pickingMode = PickingMode.Ignore;

        foreach (var h in handles)
        {
            var row = new VisualElement();
            row.AddToClassList("stat-row");

            var key = new Label(h.Name + ":");
            key.AddToClassList("stat-label");

            var value = new Label(h.GetValueString());
            value.AddToClassList("stat-value");

            row.Add(key);
            row.Add(value);
            stats.Add(row);
        }

        toggle.clicked += () =>
        {
            stats.ToggleInClassList("stats-content-holder--collapsed");
            toggle.text = stats.ClassListContains("stats-content-holder--collapsed") ? "+" : "−";
        };

        box.Add(stats);
        return box;
    }
```

Notes on styling and behavior
----------------------------
- The `CreateUITKBoxForDefinition` function creates a simple layout compatible with your existing USS (`NewUSSFile.uss`). You may want to add more properties (width/height) based on `placementManager.CalculatePanelSize(...)` if you need strict sizing.
- The code above uses `Monitor.Registry` as the single source of truth for monitor handles; that matches the existing runtime registry.

Testing
-------
1. In the Project window, open your `MonitorPanelSettings` asset and ensure `enableRuntimeMonitoring` is `true`.
2. Add one or more `MonitoredExample` or other `[Monitor]` targets in the scene and call `Monitor.StartMonitoring(this)` in `OnEnable()` for each (existing pattern in your repo).
3. Add a GameObject with `DynamicColumnLayout` attached and assign the `MonitorPanelSettings` asset in the inspector.
4. Enter Play mode — boxes should appear only for definitions that have at least one handle.

Optional: enable providers
-------------------------
If you prefer explicit scene providers (for non-reflection sources), create an `IPanelBoxProvider` interface and have scene components implement it. Then in the box-determination step prefer provider counts before falling back to `Monitor.Registry`.

Wrap up
------
This change makes your UI Toolkit builder create boxes only when there are actual monitors to show, keeps the project toggle to disable monitoring entirely, and reuses your existing grouping/filtering rules from `MonitorPanelController`. Follow the code snippets exactly where indicated, and test the three-step workflow described above.

If you want, I can now either (A) generate the exact patch files for you to apply, or (B) walk you line-by-line as you edit. Which do you prefer?
