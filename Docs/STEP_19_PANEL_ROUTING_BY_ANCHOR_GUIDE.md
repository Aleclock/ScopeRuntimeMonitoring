# Step 19 - Panel routing by anchor and per-box overrides

Goal: let the package decide which monitored boxes belong to which runtime panel by using anchors as the routing key.

This is the model we want:
- global defaults live in `MonitorPanelSettings`
- each runtime panel instance can override those defaults with `MonitorPanelOverrides`
- each monitored object can optionally declare a box anchor with `MonitorBoxOverrides`
- the panel only shows boxes whose anchor matches the panel anchor

Default routing rule:
- `TopLeft` box -> `TopLeft` panel
- `TopRight` box -> `TopRight` panel
- `BottomLeft` box -> `BottomLeft` panel
- `BottomRight` box -> `BottomRight` panel

If a box does not override its anchor, it inherits the default anchor from `MonitorPanelSettings`.

Why this model works
- It is simple.
- It matches the existing `MonitorPanelAnchor` enum.
- It avoids hardcoding screen coordinates.
- It keeps the layout system in charge of placement, while the box settings control routing.

What you will change
1. extend `MonitorBoxOverrides` with an optional anchor override
2. make `MonitorPanelView` filter boxes by anchor
3. keep the existing panel override system as the source of panel anchor and layout defaults
4. test by assigning different anchors to different monitored objects

## 1) Update `MonitorBoxOverrides`

Open [Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorBoxOverrides.cs](../Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorBoxOverrides.cs) and replace the file with this version:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MonitorBoxOverrides : MonoBehaviour
{
    [Header("Box Overrides")]
    [SerializeField] private bool enableRuntimeBox = true;

    [Header("Routing")]
    [SerializeField] private bool overrideAnchor = false;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;

    [Header("Optional Overrides")]
    [SerializeField] private bool overrideUpdateInterval = false;
    [SerializeField, Min(0.01f)] private float updateInterval = 1f;

    [SerializeField] private bool overrideBoxStyleSheet = false;
    [SerializeField] private StyleSheet boxStyleSheet;

    [Header("Layout Overrides")]
    [SerializeField] private bool overridePanelWidth = false;
    [SerializeField, Min(0f)] private float panelWidth = 430f;

    [SerializeField] private bool overridePanelHeight = false;
    [SerializeField, Min(0f)] private float panelHeight = 280f;

    [SerializeField] private bool overrideFontSize = false;
    [SerializeField, Min(0f)] private float fontSize = 17f;

    [SerializeField] private bool overrideRowHeight = false;
    [SerializeField, Min(0f)] private float rowHeight = 26f;

    [SerializeField] private bool overrideRowSpacing = false;
    [SerializeField, Min(0f)] private float rowSpacing = 4f;

    [Header("Identification")]
    [SerializeField] private string boxId = "";

    public bool EnableRuntimeBox => enableRuntimeBox;

    public bool OverrideAnchor => overrideAnchor;
    public MonitorPanelAnchor Anchor => anchor;

    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public float UpdateInterval => updateInterval;

    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public StyleSheet BoxStyleSheet => boxStyleSheet;

    public bool OverridePanelWidth => overridePanelWidth;
    public float PanelWidth => panelWidth;

    public bool OverridePanelHeight => overridePanelHeight;
    public float PanelHeight => panelHeight;

    public bool OverrideFontSize => overrideFontSize;
    public float FontSize => fontSize;

    public bool OverrideRowHeight => overrideRowHeight;
    public float RowHeight => rowHeight;

    public bool OverrideRowSpacing => overrideRowSpacing;
    public float RowSpacing => rowSpacing;

    public string BoxId => boxId;
}
```

What this adds:
- `overrideAnchor` decides whether the box should route to a custom panel anchor
- `anchor` is the routing key when the override is enabled
- if the override is disabled, the box uses the global default anchor

## 2) Update `MonitorPanelView` so it only shows matching boxes

Open [Assets/com.acproject.scoperuntimemonitoring/Runtime/MonitorPanelView.cs](../Assets/com.acproject.scoperuntimemonitoring/Runtime/MonitorPanelView.cs) and replace it with this version:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MonitorPanelView : MonoBehaviour
{
    [Header("Shared Defaults")]
    [SerializeField] private MonitorPanelSettings panelSettings;

    [Header("Optional Per-Panel Overrides")]
    [SerializeField] private MonitorPanelOverrides overrides;

    private readonly List<RowBinding> rowBindings = new List<RowBinding>();
    private readonly Dictionary<object, VisualElement> targetToBoxMap = new Dictionary<object, VisualElement>();

    private UIDocument uiDocument;
    private MonitorPanelConfig resolvedConfig;

    private VisualElement columnContainer;

    private void OnEnable()
    {
        panelSettings ??= LoadDefaultPanelSettings();

        rowBindings.Clear();
        targetToBoxMap.Clear();

        resolvedConfig = MonitorPanelConfigResolver.Resolve(panelSettings, overrides);

        Monitor.TargetRegistered += OnTargetRegistered;

        EnsureUIExists();
        ApplyPanelStyleSheet();
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

    private string GetDisplayNameForTarget(object target)
    {
        if (target == null)
            return "Unknown";

        if (target is UnityEngine.Component component && component.gameObject != null)
            return component.gameObject.name;

        return target.GetType().Name;
    }

    private void AddBoxForTarget(object target)
    {
        if (target == null)
            return;

        if (targetToBoxMap.ContainsKey(target))
            return;

        if (!TryResolveBoxOverrides(target, out var boxOverrides))
            return;

        if (!IsBoxAllowedForThisPanel(boxOverrides))
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

    private bool TryResolveBoxOverrides(object target, out MonitorBoxOverrides boxOverrides)
    {
        boxOverrides = null;

        if (target is not Component component)
            return true;

        boxOverrides = component.GetComponent<MonitorBoxOverrides>();
        return true;
    }

    private bool IsBoxAllowedForThisPanel(MonitorBoxOverrides boxOverrides)
    {
        if (boxOverrides == null)
            return true;

        if (!boxOverrides.EnableRuntimeBox)
            return false;

        var boxAnchor = boxOverrides.OverrideAnchor ? boxOverrides.Anchor : resolvedConfig.Anchor;
        return boxAnchor == resolvedConfig.Anchor;
    }

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

            rowBindings.Add(new RowBinding
            {
                Handle = handle,
                KeyLabel = keyLabel,
                ValueLabel = valueLabel
            });
        }

        toggleButton.clicked += () =>
        {
            statsContainer.ToggleInClassList("stats-content-holder--collapsed");
            toggleButton.text = statsContainer.ClassListContains("stats-content-holder--collapsed") ? "+" : "−";
        };

        box.Add(statsContainer);
        return box;
    }

    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
        RefreshMonitoredValues();
    }

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

    private void ApplyLayoutAnchor(MonitorPanelAnchor anchor)
    {
        if (columnContainer == null) return;

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

    private void EnsureUIExists()
    {
        if (uiDocument == null)
        {
            if (!TryGetComponent(out uiDocument))
                uiDocument = gameObject.AddComponent<UIDocument>();
        }

        if (uiDocument.panelSettings == null && panelSettings != null)
            uiDocument.panelSettings = panelSettings.panelSettings;

        ApplyPanelStyleSheet();
    }

    private void ApplyPanelStyleSheet()
    {
        if (uiDocument == null)
            return;

        var root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        if (resolvedConfig.BoxStyleSheet != null && !root.styleSheets.Contains(resolvedConfig.BoxStyleSheet))
        {
            root.styleSheets.Add(resolvedConfig.BoxStyleSheet);
        }
    }

    private static MonitorPanelSettings LoadDefaultPanelSettings()
    {
        var settings = Resources.Load<MonitorPanelSettings>("Defaults/MonitorPanelSettings");

        if (settings == null)
        {
            Debug.LogWarning("MonitorPanelView: could not load default MonitorPanelSettings from Resources/Defaults/MonitorPanelSettings.");
        }

        return settings;
    }

    private sealed class RowBinding
    {
        public IMonitorHandle Handle;
        public Label ValueLabel;
        public Label KeyLabel;
    }
}
```

What changed in the panel builder:
- it loads the default settings if no settings asset is assigned
- it resolves the panel anchor from the panel config
- it looks for `MonitorBoxOverrides` on the monitored component
- it skips boxes that are disabled
- it routes a box to the current panel only if the box anchor matches the panel anchor

## 3) Keep `MonitorPanelOverrides` as the panel-level override layer (with full code)

Open [Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorPanelOverrides.cs](../Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorPanelOverrides.cs) and replace it with:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MonitorPanelOverrides : MonoBehaviour
{
    [Header("Override Switches")]
    [SerializeField] private bool enableRuntimePanel = true;
    [SerializeField] private bool overridePanelSettings;
    [SerializeField] private bool overrideBoxStyleSheet;
    [SerializeField] private bool overrideAnchor;
    [SerializeField] private bool overrideUpdateInterval;
    [SerializeField] private bool overridePadding;

    [Header("Per-Panel Values")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private StyleSheet boxStyleSheet;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;
    [SerializeField, Min(0.05f)] private float updateInterval = 1f;
    [SerializeField, Min(0f)] private float padding = 15f;

    public bool EnableRuntimePanel => enableRuntimePanel;

    public bool OverridePanelSettings => overridePanelSettings;
    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public bool OverrideAnchor => overrideAnchor;
    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public bool OverridePadding => overridePadding;

    public PanelSettings PanelSettings => panelSettings;
    public StyleSheet BoxStyleSheet => boxStyleSheet;
    public MonitorPanelAnchor Anchor => anchor;
    public float UpdateInterval => updateInterval;
    public float Padding => padding;
}
```

Then ensure the resolver consumes those properties.

Open [Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorPanelConfigResolver.cs](../Assets/com.acproject.scoperuntimemonitoring/Runtime/Settings/MonitorPanelConfigResolver.cs) and replace it with:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public readonly struct MonitorPanelConfig
{
    public readonly PanelSettings PanelSettings;
    public readonly StyleSheet BoxStyleSheet;
    public readonly MonitorPanelAnchor Anchor;
    public readonly float UpdateInterval;
    public readonly float Padding;

    public MonitorPanelConfig(
        PanelSettings panelSettings,
        StyleSheet boxStyleSheet,
        MonitorPanelAnchor anchor,
        float updateInterval,
        float padding
    )
    {
        PanelSettings = panelSettings;
        BoxStyleSheet = boxStyleSheet;
        Anchor = anchor;
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
        float updateInterval = defaults.defaultUpdateInterval;
        float padding = defaults.defaultPadding;

        if (overrides != null)
        {
            if (!overrides.EnableRuntimePanel)
                return default;

            if (overrides.OverridePanelSettings && overrides.PanelSettings != null)
                panelSettings = overrides.PanelSettings;

            if (overrides.OverrideBoxStyleSheet && overrides.BoxStyleSheet != null)
                boxStyleSheet = overrides.BoxStyleSheet;

            if (overrides.OverrideAnchor)
                anchor = overrides.Anchor;

            if (overrides.OverrideUpdateInterval)
                updateInterval = Mathf.Max(0.05f, overrides.UpdateInterval);

            if (overrides.OverridePadding)
                padding = Mathf.Max(0f, overrides.Padding);
        }

        if (panelSettings == null)
            Debug.LogError("MonitorPanelConfigResolver: panelSettings is null after resolution.");

        if (boxStyleSheet == null)
            Debug.LogError("MonitorPanelConfigResolver: boxStyleSheet is null after resolution.");

        return new MonitorPanelConfig(panelSettings, boxStyleSheet, anchor, updateInterval, padding);
    }
}
```

Important note:
- If your resolver returns `default` when `EnableRuntimePanel` is false, make sure your panel view handles this by early-returning before UI creation.

## 4) How to use it in the scene

Add components like this:

On the panel GameObject:
- `MonitorPanelView`
- `MonitorPanelOverrides`

Set `MonitorPanelOverrides.Anchor` to one of:
- `TopLeft`
- `TopRight`
- `BottomLeft`
- `BottomRight`

On the monitored object GameObject:
- your monitored MonoBehaviour with `[Monitor]` fields/properties
- `MonitorBoxOverrides`

Set `MonitorBoxOverrides.OverrideAnchor = true` and choose the anchor you want.

Examples:
- a health box with `TopLeft`
- a combat box with `TopRight`
- a debug box with `BottomLeft`
- a diagnostics box with `BottomRight`

## 5) Testing checklist

Create 4 panels, one for each anchor:
- `TopLeft`
- `TopRight`
- `BottomLeft`
- `BottomRight`

Create 4 monitored GameObjects and assign their `MonitorBoxOverrides.Anchor` values to match.

Expected behavior:
- the top-left box appears only in the top-left panel
- the top-right box appears only in the top-right panel
- the bottom-left box appears only in the bottom-left panel
- the bottom-right box appears only in the bottom-right panel

Then test this fallback:
- disable `OverrideAnchor` on a box
- it should fall back to the default anchor from `MonitorPanelSettings`

## 6) Future extension

If later you want more than one panel per anchor, add a `PanelId` string to both panel and box overrides.

That gives you two routing keys:
- `Anchor` for layout family
- `PanelId` for exact target panel selection

For now, anchor routing is enough and is the simplest stable solution.
