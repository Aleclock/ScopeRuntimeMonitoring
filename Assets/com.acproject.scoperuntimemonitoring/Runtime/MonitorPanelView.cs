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
    private readonly Dictionary<MonitorPanelAnchor, VisualElement> singlePanelAnchorContainers = new Dictionary<MonitorPanelAnchor, VisualElement>();

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
            AddBoxForTarget(target);
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

        EnsureSinglePanelAnchorContainers();
    }

    private void EnsureSinglePanelAnchorContainers()
    {
        if (GetActivePanelCount() > 1)
            return;

        if (singlePanelAnchorContainers.Count > 0)
            return;

        CreateSinglePanelAnchorContainer(MonitorPanelAnchor.TopLeft, Justify.FlexStart, Align.FlexStart);
        CreateSinglePanelAnchorContainer(MonitorPanelAnchor.TopRight, Justify.FlexStart, Align.FlexEnd);
        CreateSinglePanelAnchorContainer(MonitorPanelAnchor.BottomLeft, Justify.FlexEnd, Align.FlexStart);
        CreateSinglePanelAnchorContainer(MonitorPanelAnchor.BottomRight, Justify.FlexEnd, Align.FlexEnd);
    }

    private void CreateSinglePanelAnchorContainer(MonitorPanelAnchor anchor, Justify justify, Align align)
    {
        var anchorContainer = new VisualElement();
        anchorContainer.style.position = Position.Absolute;
        anchorContainer.style.left = 0f;
        anchorContainer.style.right = 0f;
        anchorContainer.style.top = 0f;
        anchorContainer.style.bottom = 0f;
        anchorContainer.style.flexDirection = FlexDirection.Column;
        anchorContainer.style.justifyContent = justify;
        anchorContainer.style.alignItems = align;
        anchorContainer.style.paddingTop = resolvedConfig.Padding;
        anchorContainer.style.paddingBottom = resolvedConfig.Padding;
        anchorContainer.style.paddingLeft = resolvedConfig.Padding;
        anchorContainer.style.paddingRight = resolvedConfig.Padding;
        anchorContainer.pickingMode = PickingMode.Ignore;

        singlePanelAnchorContainers[anchor] = anchorContainer;
        columnContainer.Add(anchorContainer);
    }

    private string GetDisplayNameForTarget(object target)
    {
        if (target == null)
            return "Unknown";

        if (target is Component component && component.gameObject != null)
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

        var targetAnchor = ResolveBoxAnchor(boxOverrides);

        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var handlesForTarget = allHandles.Where(handle => ReferenceEquals(handle.Target, target)).ToList();

        if (handlesForTarget.Count == 0)
            return;

        var title = GetDisplayNameForTarget(target);
        var boxElement = CreateUITKBoxForInstance(target, title, handlesForTarget, boxOverrides);

        targetToBoxMap[target] = boxElement;

        if (GetActivePanelCount() <= 1 && singlePanelAnchorContainers.TryGetValue(targetAnchor, out var anchorContainer))
            anchorContainer.Add(boxElement);
        else
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
        // In single-panel mode, show all enabled boxes regardless of routing anchor.
        // This keeps the default experience intuitive when users have only one runtime panel.
        if (GetActivePanelCount() <= 1)
        {
            return boxOverrides == null || boxOverrides.EnableRuntimeBox;
        }

        var boxAnchor = ResolveBoxAnchor(boxOverrides);

        if (boxOverrides != null && !boxOverrides.EnableRuntimeBox)
            return false;

        return boxAnchor == resolvedConfig.Anchor;
    }

    private MonitorPanelAnchor ResolveBoxAnchor(MonitorBoxOverrides boxOverrides)
    {
        var defaultAnchor = panelSettings != null
            ? panelSettings.defaultAnchor
            : MonitorPanelAnchor.TopLeft;

        if (boxOverrides == null)
            return defaultAnchor;

        return boxOverrides.OverrideAnchor ? boxOverrides.Anchor : defaultAnchor;
    }

    private static int GetActivePanelCount()
    {
        return FindObjectsOfType<MonitorPanelView>().Length;
    }

    private VisualElement CreateUITKBoxForInstance(object target, string title, IReadOnlyList<IMonitorHandle> handles, MonitorBoxOverrides boxOverrides)
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

        ApplyBoxOverrides(box, statsContainer, boxOverrides);

        toggleButton.clicked += () =>
        {
            statsContainer.ToggleInClassList("stats-content-holder--collapsed");
            toggleButton.text = statsContainer.ClassListContains("stats-content-holder--collapsed") ? "+" : "−";
        };

        box.Add(statsContainer);
        return box;
    }

    private void ApplyBoxOverrides(VisualElement box, VisualElement statsContainer, MonitorBoxOverrides boxOverrides)
    {
        if (boxOverrides == null)
            return;

        if (boxOverrides.OverrideBoxStyleSheet && boxOverrides.BoxStyleSheet != null && !box.styleSheets.Contains(boxOverrides.BoxStyleSheet))
        {
            box.styleSheets.Add(boxOverrides.BoxStyleSheet);
        }

        if (boxOverrides.OverridePanelWidth)
        {
            box.style.width = boxOverrides.PanelWidth;
            box.style.minWidth = boxOverrides.PanelWidth;
        }

        if (boxOverrides.OverridePanelHeight)
        {
            box.style.height = boxOverrides.PanelHeight;
            box.style.minHeight = boxOverrides.PanelHeight;
        }

        if (boxOverrides.OverrideFontSize || boxOverrides.OverrideRowHeight || boxOverrides.OverrideRowSpacing)
        {
            foreach (var row in statsContainer.Children())
            {
                if (row == null)
                    continue;

                if (boxOverrides.OverrideRowHeight)
                    row.style.height = boxOverrides.RowHeight;

                if (boxOverrides.OverrideRowSpacing)
                    row.style.marginBottom = boxOverrides.RowSpacing;

                if (boxOverrides.OverrideFontSize)
                {
                    foreach (var child in row.Children())
                    {
                        if (child is Label label)
                            label.style.fontSize = boxOverrides.FontSize;
                    }
                }
            }
        }

        if (boxOverrides.OverrideFontSize)
        {
            foreach (var child in box.Children())
            {
                if (child is Label label)
                    label.style.fontSize = boxOverrides.FontSize;
            }
        }
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

        columnContainer.style.flexDirection = FlexDirection.Column;
        columnContainer.style.justifyContent = Justify.FlexStart;
        columnContainer.style.alignItems = Align.FlexStart;

        switch (anchor)
        {
            case MonitorPanelAnchor.TopLeft:
                columnContainer.style.justifyContent = Justify.FlexStart;
                columnContainer.style.alignItems = Align.FlexStart;
                break;
            case MonitorPanelAnchor.TopRight:
                columnContainer.style.justifyContent = Justify.FlexStart;
                columnContainer.style.alignItems = Align.FlexEnd;
                break;
            case MonitorPanelAnchor.BottomLeft:
                columnContainer.style.justifyContent = Justify.FlexEnd;
                columnContainer.style.alignItems = Align.FlexStart;
                break;
            case MonitorPanelAnchor.BottomRight:
                columnContainer.style.justifyContent = Justify.FlexEnd;
                columnContainer.style.alignItems = Align.FlexEnd;
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
            root.styleSheets.Add(resolvedConfig.BoxStyleSheet);
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