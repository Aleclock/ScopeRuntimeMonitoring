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

        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var handlesForTarget = allHandles.Where(handle => ReferenceEquals(handle.Target, target)).ToList();

        if (handlesForTarget.Count == 0)
            return;

        var title = GetDisplayNameForTarget(target);
        var boxElement = CreateUITKBoxForInstance(target, title, handlesForTarget);

        targetToBoxMap[target] = boxElement;
        columnContainer.Add(boxElement);
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

    private sealed class RowBinding
    {
        public IMonitorHandle Handle;
        public Label ValueLabel;
        public Label KeyLabel;
    }
}