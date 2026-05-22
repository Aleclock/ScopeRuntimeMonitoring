using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DynamicColumnLayout : MonoBehaviour
{
    [Header("Shared Defaults")]
    [SerializeField] private MonitorPanelSettings panelSettings;

    [Header("Optional Per-Panel Overrides")]
    [SerializeField] private MonitorPanelOverrides overrides;

    private readonly List<RowBinding> rowBindings = new List<RowBinding>();

    private UIDocument uiDocument;
    private MonitorPanelConfig resolvedConfig;

    private VisualElement columnContainer;

    private void OnEnable()
    {
        if (!TryGetComponent(out uiDocument))
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        resolvedConfig = MonitorPanelConfigResolver.Resolve(panelSettings, overrides);

        if (resolvedConfig.PanelSettings == null || resolvedConfig.BoxStyleSheet == null)
        {
            if (panelSettings != null && panelSettings.logMissingReferences)
                Debug.LogError("DynamicColumnLayout: missing resolved UI Toolkit references.", this);
            return;
        }

        uiDocument.panelSettings = resolvedConfig.PanelSettings;

        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        if (!root.styleSheets.Contains(resolvedConfig.BoxStyleSheet))
            root.styleSheets.Add(resolvedConfig.BoxStyleSheet);

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

        // Build monitored boxes from the live registry instead of demo placeholders
        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var definitions = DefaultBoxDefinitions.Create();

        foreach (var definition in definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            if (handlesForBox == null || handlesForBox.Count == 0)
                continue;

            var boxElement = CreateUITKBoxForDefinition(definition, handlesForBox);
            columnContainer.Add(boxElement);
        }

        root.Add(columnContainer);
    }

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

    private VisualElement CreateUITKBoxForDefinition(MonitorBoxDefinition definition, IReadOnlyList<IMonitorHandle> handles)
    {
        var box = new VisualElement();
        box.AddToClassList("custom-box");
        box.pickingMode = PickingMode.Position;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("box-header-row");

        var titleLabel = new Label(definition.Title);
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

    private void Update()
    {
        if (resolvedConfig.PanelSettings == null)
            return;

        ApplyLayoutAnchor(resolvedConfig.Anchor);
        RefreshMonitoredValues();
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

    private sealed class RowBinding
    {
        public IMonitorHandle Handle;
        public Label ValueLabel;
        public Label KeyLabel;
    }
}