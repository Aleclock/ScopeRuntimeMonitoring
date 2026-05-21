# Step 8: Hybrid Package Defaults + Per-Panel Overrides

This guide shows how to turn the working UI Toolkit prototype into a reusable system with two layers:

1. **Package defaults**: one shared settings asset that stores the package's default `PanelSettings`, default `StyleSheet`, and common layout values.
2. **Per-panel overrides**: optional components on each monitor panel that can replace only the values that need to be different.

This is the best middle ground between:

- hard-wiring references into every scene object, and
- forcing every panel to use the exact same look.

Your current prototype in [Assets/RuntimeFlexLayout.cs](../Assets/RuntimeFlexLayout.cs) and [Assets/NewUSSFile.uss](../Assets/NewUSSFile.uss) already proves the layout works. This step makes it configurable and reusable.

---

## Why this approach

Use this architecture when you want:

- one shared default setup for the whole project,
- easy swapping of package themes and USS files,
- optional per-panel changes without duplicating the whole UI,
- a clean place to add editor tooling later,
- a setup that is still understandable when you come back to it later.

Do **not** put scene-only references directly into a Project Settings window. Project Settings should store shared defaults, not temporary scene objects.

---

## Final shape of the system

You will end up with these layers:

1. **`MonitorPanelSettings`**
   - A `ScriptableObject` asset.
   - Stores shared defaults.
   - References the `PanelSettings` asset and the default `StyleSheet`.

2. **`MonitorPanelOverrides`**
   - A `MonoBehaviour` placed on a specific panel GameObject.
   - Lets that panel override only the values it needs.

3. **`MonitorPanelConfigResolver`**
   - Merges defaults and overrides into one final runtime config.

4. **Your runtime builder**
   - Reads the resolved config.
   - Builds the UI exactly once.
   - Uses the same code path regardless of whether values came from defaults or overrides.

5. **Optional Project Settings page**
   - Lets you edit the shared settings asset from `Project Settings`.

---

## Step 1: Create the shared settings asset

Create a new runtime script named `MonitorPanelSettings.cs`.

Place it somewhere like `Assets/Scope/Settings/` or `Assets/Scope/UIFramework/Settings/`.

Copy-paste this file exactly:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Scope Runtime Monitoring/Monitor Panel Settings", fileName = "MonitorPanelSettings")]
public sealed class MonitorPanelSettings : ScriptableObject
{
    [Header("UI Toolkit Assets")]
    public PanelSettings panelSettings;
    public StyleSheet boxStyleSheet;

    [Header("Runtime Defaults")]
    public MonitorPanelAnchor defaultAnchor = MonitorPanelAnchor.TopLeft;
    [Min(1)] public int defaultNumberOfBoxes = 12;
    [Min(0.05f)] public float defaultUpdateInterval = 1f;
    [Min(0f)] public float defaultPadding = 15f;

    [Header("Optional Safety")]
    public bool logMissingReferences = true;
}

public enum MonitorPanelAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
```

### What this asset does

- `panelSettings` stores the UI Toolkit panel configuration, including the `ThemeStyleSheet`.
- `boxStyleSheet` stores the USS file for your boxes.
- `defaultAnchor` controls where the panel starts.
- `defaultNumberOfBoxes` controls the base content count.
- `defaultUpdateInterval` controls how often the values refresh.
- `defaultPadding` gives you one place to tune spacing.

### Why `PanelSettings` is stored here

`PanelSettings` is the Unity object that already knows about:

- scale mode,
- match mode,
- reference resolution,
- theme style sheet,
- and other UI Toolkit panel behavior.

So instead of rebuilding that every time in code, you store one configured asset and reuse it.

---

## Step 2: Create the per-panel override component

Create a second runtime script named `MonitorPanelOverrides.cs`.

This component goes on any panel object that needs custom behavior.

Copy-paste this file exactly:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MonitorPanelOverrides : MonoBehaviour
{
    [Header("Override Switches")]
    [SerializeField] private bool overridePanelSettings;
    [SerializeField] private bool overrideBoxStyleSheet;
    [SerializeField] private bool overrideAnchor;
    [SerializeField] private bool overrideNumberOfBoxes;
    [SerializeField] private bool overrideUpdateInterval;
    [SerializeField] private bool overridePadding;

    [Header("Per-Panel Values")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private StyleSheet boxStyleSheet;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;
    [SerializeField, Min(1)] private int numberOfBoxes = 12;
    [SerializeField, Min(0.05f)] private float updateInterval = 1f;
    [SerializeField, Min(0f)] private float padding = 15f;

    public bool OverridePanelSettings => overridePanelSettings;
    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public bool OverrideAnchor => overrideAnchor;
    public bool OverrideNumberOfBoxes => overrideNumberOfBoxes;
    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public bool OverridePadding => overridePadding;

    public PanelSettings PanelSettings => panelSettings;
    public StyleSheet BoxStyleSheet => boxStyleSheet;
    public MonitorPanelAnchor Anchor => anchor;
    public int NumberOfBoxes => numberOfBoxes;
    public float UpdateInterval => updateInterval;
    public float Padding => padding;
}
```

### How to use it

- Add this component to a panel GameObject only when you want a custom look or behavior.
- Leave the switches off if the panel should use the shared defaults.
- Turn on only the overrides you need.

### Good examples

- One panel uses a different anchor because it should appear in the bottom-right corner.
- One panel uses a different USS file because it is a compact version.
- One panel refreshes values every 0.25 seconds instead of every second.

---

## Step 3: Create the resolver that merges defaults and overrides

Create a third runtime script named `MonitorPanelConfigResolver.cs`.

This file is small, but it is the important part. It prevents the runtime builder from being full of `if` statements.

Copy-paste this file exactly:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public readonly struct MonitorPanelConfig
{
    public readonly PanelSettings PanelSettings;
    public readonly StyleSheet BoxStyleSheet;
    public readonly MonitorPanelAnchor Anchor;
    public readonly int NumberOfBoxes;
    public readonly float UpdateInterval;
    public readonly float Padding;

    public MonitorPanelConfig(
        PanelSettings panelSettings,
        StyleSheet boxStyleSheet,
        MonitorPanelAnchor anchor,
        int numberOfBoxes,
        float updateInterval,
        float padding)
    {
        PanelSettings = panelSettings;
        BoxStyleSheet = boxStyleSheet;
        Anchor = anchor;
        NumberOfBoxes = numberOfBoxes;
        UpdateInterval = updateInterval;
        Padding = padding;
    }
}

public static class MonitorPanelConfigResolver
{
    public static MonitorPanelConfig Resolve(MonitorPanelSettings defaults, MonitorPanelOverrides overrides)
    {
        if (defaults == null)
        {
            Debug.LogError("MonitorPanelConfigResolver: missing MonitorPanelSettings asset.");
            return default;
        }

        PanelSettings panelSettings = defaults.panelSettings;
        StyleSheet boxStyleSheet = defaults.boxStyleSheet;
        MonitorPanelAnchor anchor = defaults.defaultAnchor;
        int numberOfBoxes = defaults.defaultNumberOfBoxes;
        float updateInterval = defaults.defaultUpdateInterval;
        float padding = defaults.defaultPadding;

        if (overrides != null)
        {
            if (overrides.OverridePanelSettings && overrides.PanelSettings != null)
            {
                panelSettings = overrides.PanelSettings;
            }

            if (overrides.OverrideBoxStyleSheet && overrides.BoxStyleSheet != null)
            {
                boxStyleSheet = overrides.BoxStyleSheet;
            }

            if (overrides.OverrideAnchor)
            {
                anchor = overrides.Anchor;
            }

            if (overrides.OverrideNumberOfBoxes)
            {
                numberOfBoxes = Mathf.Max(1, overrides.NumberOfBoxes);
            }

            if (overrides.OverrideUpdateInterval)
            {
                updateInterval = Mathf.Max(0.05f, overrides.UpdateInterval);
            }

            if (overrides.OverridePadding)
            {
                padding = Mathf.Max(0f, overrides.Padding);
            }
        }

        if (panelSettings == null)
        {
            Debug.LogError("MonitorPanelConfigResolver: panelSettings is null after resolution.");
        }

        if (boxStyleSheet == null)
        {
            Debug.LogError("MonitorPanelConfigResolver: boxStyleSheet is null after resolution.");
        }

        return new MonitorPanelConfig(panelSettings, boxStyleSheet, anchor, numberOfBoxes, updateInterval, padding);
    }
}
```

### Why this file matters

This is the piece that makes the system clean:

- default values stay in one place,
- overrides stay optional,
- the runtime code only cares about one resolved config object.

---

## Step 4: Update your runtime builder to consume the config

Your current script in [Assets/RuntimeFlexLayout.cs](../Assets/RuntimeFlexLayout.cs) is already the right kind of place for this logic.

The goal is not to rewrite the whole panel builder. The goal is to change the setup phase so it reads from the resolver.

### Replace the top-level fields with these

Use this pattern in your builder script:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DynamicColumnLayout : MonoBehaviour
{
    [Header("Shared Defaults")]
    [SerializeField] private MonitorPanelSettings panelSettings;

    [Header("Optional Per-Panel Overrides")]
    [SerializeField] private MonitorPanelOverrides overrides;

    private UIDocument uiDocument;
    private MonitorPanelConfig resolvedConfig;

    private VisualElement columnContainer;
    private readonly List<Label> dynamicValueLabels = new List<Label>();
    private float updateTimer = 0f;
    private float updateInterval = 1f;
    private int numberOfBoxes = 12;
```

### Then replace the start of `OnEnable()` with this

```csharp
    private void OnEnable()
    {
        if (!TryGetComponent(out uiDocument))
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        resolvedConfig = MonitorPanelConfigResolver.Resolve(panelSettings, overrides);

        if (resolvedConfig.PanelSettings == null || resolvedConfig.BoxStyleSheet == null)
        {
            Debug.LogError("DynamicColumnLayout: missing resolved UI Toolkit references.", this);
            return;
        }

        uiDocument.panelSettings = resolvedConfig.PanelSettings;

        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();
        dynamicValueLabels.Clear();

        if (!root.styleSheets.Contains(resolvedConfig.BoxStyleSheet))
        {
            root.styleSheets.Add(resolvedConfig.BoxStyleSheet);
        }

        numberOfBoxes = resolvedConfig.NumberOfBoxes;
        updateInterval = resolvedConfig.UpdateInterval;

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

        // Continue with your existing box creation logic here.
    }
```

### Update your anchor method to use the shared enum

If your current script has its own nested `LayoutAnchor` enum, replace it with `MonitorPanelAnchor` so the whole project uses one shared type.

That means this method becomes:

```csharp
    private void ApplyLayoutAnchor(MonitorPanelAnchor anchor)
    {
        if (columnContainer == null)
        {
            return;
        }

        switch (anchor)
        {
            case MonitorPanelAnchor.TopLeft:
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.flexWrap = Wrap.Wrap;
                break;
            case MonitorPanelAnchor.TopRight:
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.flexWrap = Wrap.WrapReverse;
                break;
            case MonitorPanelAnchor.BottomLeft:
                columnContainer.style.flexDirection = FlexDirection.ColumnReverse;
                columnContainer.style.flexWrap = Wrap.Wrap;
                break;
            case MonitorPanelAnchor.BottomRight:
                columnContainer.style.flexDirection = FlexDirection.ColumnReverse;
                columnContainer.style.flexWrap = Wrap.WrapReverse;
                break;
        }
    }
```

### Update `Update()` to use the resolved interval

```csharp
    private void Update()
    {
        ApplyLayoutAnchor(resolvedConfig.Anchor);

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RandomizeFields();
        }
    }
```

### Why this works

Your runtime builder still owns the actual UI creation.

The settings asset only decides:

- which assets to use,
- where the panel should anchor,
- how often it updates,
- and whether a specific panel needs overrides.

That keeps responsibilities clean.

---

## Step 5: Add a Project Settings page (optional but recommended)

This step is optional.

If you want the project to feel polished, expose the shared settings asset under `Project Settings`.

This does **not** replace the asset. It simply gives you a nicer place to edit it.

Create a folder named `Assets/Editor/` if it does not already exist, then create `MonitorPanelProjectSettingsProvider.cs`.

Copy-paste this file exactly:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class MonitorPanelProjectSettingsProvider
{
    private const string FolderPath = "Assets/ScopeRuntimeMonitoringSettings";
    private const string AssetPath = FolderPath + "/MonitorPanelSettings.asset";

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new SettingsProvider("Project/Scope Runtime Monitoring", SettingsScope.Project)
        {
            guiHandler = DrawSettings,
            keywords = new[] { "scope", "runtime", "monitor", "panel", "theme", "stylesheet", "ui toolkit" }
        };
    }

    private static void DrawSettings(string searchContext)
    {
        MonitorPanelSettings settings = GetOrCreateSettings();
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Could not create or load MonitorPanelSettings.asset.", MessageType.Error);
            return;
        }

        Editor editor = Editor.CreateEditor(settings);
        try
        {
            editor.OnInspectorGUI();
        }
        finally
        {
            Object.DestroyImmediate(editor);
        }
    }

    private static MonitorPanelSettings GetOrCreateSettings()
    {
        MonitorPanelSettings settings = AssetDatabase.LoadAssetAtPath<MonitorPanelSettings>(AssetPath);
        if (settings != null)
        {
            return settings;
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "ScopeRuntimeMonitoringSettings");
        }

        settings = ScriptableObject.CreateInstance<MonitorPanelSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
    }
}
#endif
```

### What this gives you

- A Project Settings entry called `Scope Runtime Monitoring`.
- A single shared asset that all panels can point to.
- A clean place to edit the package defaults without hunting through the scene.

### If you do not want this step

Skip it.

The runtime architecture still works with only the `ScriptableObject` asset and the override component.

---

## Step 6: Create the default assets

Now create the actual assets in the project.

### Create the `PanelSettings` asset

1. In the Project window, create a `PanelSettings` asset if you do not already have one.
2. Set its `scaleMode`, `referenceResolution`, `screenMatchMode`, and `match` values.
3. Assign the `ThemeStyleSheet` you want as the default package look.

### Create the USS file

You already have [Assets/NewUSSFile.uss](../Assets/NewUSSFile.uss).

If you want, rename it later to something more explicit like:

- `MonitorBoxStyles.uss`
- `ScopeMonitorStyles.uss`
- `RuntimeMonitorPanel.uss`

### Create the settings asset

1. Create a `MonitorPanelSettings` asset using the `CreateAssetMenu` entry.
2. Assign your `PanelSettings` asset.
3. Assign your `StyleSheet` asset.
4. Set the shared defaults:
   - anchor,
   - number of boxes,
   - update interval,
   - padding.

### Important rule

If the asset is meant to be the default for the package, it should work before any per-panel overrides are added.

That is the whole point of this design.

---

## Step 7: Use per-panel overrides only when needed

This is the practical rule that keeps the project maintainable.

Use the shared asset when:

- the panel should look like every other panel,
- the anchor is standard,
- the style is standard,
- the refresh rate is standard.

Use a per-panel override when:

- one panel needs to appear in a different corner,
- one panel needs compact spacing,
- one panel uses a different USS file,
- one panel should update faster or slower than the others.

Do not override values just because you can. Override only the values that must change.

---

## Step 8: Scene setup checklist

For each monitor panel GameObject:

1. Add or keep the `UIDocument`.
2. Add your runtime layout builder, such as `DynamicColumnLayout`.
3. Assign the shared `MonitorPanelSettings` asset.
4. Add `MonitorPanelOverrides` only if this panel needs local differences.
5. If you added overrides, enable only the switches that matter.
6. Press Play and verify that the panel uses the expected theme, USS file, anchor, and spacing.

If something does not show up, check these first:

- `panelSettings` is not null.
- `boxStyleSheet` is not null.
- the `ThemeStyleSheet` is assigned inside the `PanelSettings` asset.
- the panel has `UIDocument`.
- the panel builder script is enabled.

---

## Step 9: Keep the package defaults separate from the scene setup

This is the part that makes the whole system easier to maintain.

### Put these in the package or shared project area

- the default `PanelSettings` asset,
- the default USS files,
- the settings asset,
- the shared runtime scripts.

### Put these in the scene

- panel GameObjects,
- optional override components,
- any scene-specific layout choices.

That split keeps the reusable package code clean and keeps scene logic local.

---

## Step 10: Recommended workflow from here

If you want the best next implementation order, do it like this:

1. Create the shared `MonitorPanelSettings` asset.
2. Create the per-panel override component.
3. Add the resolver.
4. Update your runtime builder to read the resolved config.
5. Test the default setup first.
6. Add one override panel and verify that only the overridden values change.
7. Add the optional Project Settings page last.

That order keeps each step testable.

---

## Step 11: What to test

Test these three scenarios:

1. **Default only**
   - One shared settings asset.
   - No overrides.
   - All panels look the same.

2. **One custom panel**
   - One shared settings asset.
   - One panel override component.
   - Only that panel changes.

3. **Multiple custom panels**
   - Several panels use the same defaults.
   - Two or more panels override different values.
   - Each panel stays independent.

If those three cases work, the system is in good shape.

---

## Step 12: What I would ship first

If this were my implementation, I would ship it in this order:

1. Shared settings asset.
2. Per-panel override component.
3. Resolver.
4. Runtime builder integration.
5. Optional Project Settings page.

That gives you the reusable architecture quickly, without overbuilding the editor tooling too early.

---

## Short version

The hybrid approach is the right one:

- package defaults live in one shared settings asset,
- each panel can override only what it needs,
- the runtime builder reads one resolved config object,
- the Project Settings page is just a nicer editor front-end for the same asset.

If you follow the steps in this guide, you will have a setup that is:

- easier to reuse,
- easier to customize,
- easier to document,
- and easier to debug later.