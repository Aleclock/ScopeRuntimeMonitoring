using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScopeRuntimeMonitoring
{
    public class MonitorPanelView : MonoBehaviour
    {
        [Header("Shared Defaults")]
        [SerializeField] private MonitorPanelSettings panelSettings;

        [Header("Optional Per-Panel Overrides")]
        [SerializeField] private MonitorPanelOverrides overrides;

        private readonly List<RowBinding> rowBindings = new List<RowBinding>();
        private readonly Dictionary<string, VisualElement> groupToBoxMap = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Dictionary<string, VisualElement>> groupSubCardMap = new Dictionary<string, Dictionary<string, VisualElement>>();

        private UIDocument uiDocument;
        private MonitorPanelConfig resolvedConfig;

        // Responsive layout settings
        private float responsiveBoxMinPx = 160f;
        private float responsiveBoxMaxPx = 340f;
        private float responsiveBoxFraction = 0.22f; // fraction of root width for box size

        private VisualElement columnContainer;
        private readonly Dictionary<MonitorPanelAnchor, VisualElement> singlePanelAnchorContainers = new Dictionary<MonitorPanelAnchor, VisualElement>();
        private MonitorPanelAnchor? lastAppliedAnchor = null;
        private float lastUpdateTime = 0f;

        private void OnEnable()
        {
            panelSettings ??= LoadDefaultPanelSettings();

            rowBindings.Clear();
            groupToBoxMap.Clear();
            groupSubCardMap.Clear();
            lastAppliedAnchor = null;
            lastUpdateTime = 0f;

            resolvedConfig = MonitorPanelConfigResolver.Resolve(panelSettings, overrides);

            Monitor.TargetUnregistered += OnTargetUnregistered;
            Monitor.TargetRegistered += OnTargetRegistered;

            EnsureUIExists();
            ApplyPanelStyleSheet();
            BuildInitialLayoutFromExistingTargets();
        }

        private void OnDisable()
        {
            Monitor.TargetRegistered -= OnTargetRegistered;
            Monitor.TargetUnregistered -= OnTargetUnregistered;

            rowBindings.Clear();
            groupToBoxMap.Clear();
            groupSubCardMap.Clear();
        }

        private void OnTargetRegistered(object target)
        {
            EnsureUIExists();
            EnsureColumnContainer();

            var allHandles = Monitor.Registry.GetMonitorHandles();
            var handlesForTarget = allHandles.Where(handle => ReferenceEquals(handle.Target, target)).ToList();

            foreach (var handle in handlesForTarget)
            {
                RegisterHandleUI(handle);
            }
        }

        private void OnTargetUnregistered(object target)
        {
            if (target == null)
                return;

            var targetBindings = rowBindings.Where(binding => binding.Handle != null && ReferenceEquals(binding.Handle.Target, target)).ToList();

            foreach (var binding in targetBindings)
            {
                if (binding.WidgetRoot != null && binding.WidgetRoot.parent != null)
                {
                    var statRow = binding.WidgetRoot.parent;
                    if (statRow != null && statRow.parent != null)
                    {
                        statRow.parent.Remove(statRow);
                    }
                }

                rowBindings.Remove(binding);
            }

            PruneEmptyBoxesAndSubGroups();
        }

        private void BuildInitialLayoutFromExistingTargets()
        {
            EnsureColumnContainer();

            var allHandles = Monitor.Registry.GetMonitorHandles();
            foreach (var handle in allHandles)
            {
                RegisterHandleUI(handle);
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
            lastAppliedAnchor = resolvedConfig.Anchor;

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

        private void RegisterHandleUI(IMonitorHandle handle)
        {
            if (handle == null || handle.Metadata.Enabled == false)
                return;

            string groupName = !string.IsNullOrWhiteSpace(handle.Metadata.Group)
                ? handle.Metadata.Group
                : GetDisplayNameForTarget(handle.Target);

            string subGroupName = handle.Metadata.SubGroup;

            var boxOverrides = FindBoxOverrides(groupName, handle.Target);

            if (!IsBoxAllowedForThisPanel(boxOverrides))
                return;

            var targetAnchor = ResolveBoxAnchor(boxOverrides);

            // 1. Find or create the main box
            if (!groupToBoxMap.TryGetValue(groupName, out var boxElement))
            {
                boxElement = CreateEmptyBox(groupName, boxOverrides);
                groupToBoxMap[groupName] = boxElement;

                if (GetActivePanelCount() <= 1 && singlePanelAnchorContainers.TryGetValue(targetAnchor, out var anchorContainer))
                    anchorContainer.Add(boxElement);
                else
                    columnContainer.Add(boxElement);
            }

            var statsContentHolder = boxElement.Q<VisualElement>(className: "stats-content-holder");
            if (statsContentHolder == null)
                return;

            // 2. Find or create the destination container (sub-group or statsContentHolder directly)
            VisualElement targetContainer = statsContentHolder;
            if (!string.IsNullOrWhiteSpace(subGroupName))
            {
                if (!groupSubCardMap.TryGetValue(groupName, out var subMap))
                {
                    subMap = new Dictionary<string, VisualElement>();
                    groupSubCardMap[groupName] = subMap;
                }

                if (!subMap.TryGetValue(subGroupName, out var subGroupElement))
                {
                    subGroupElement = CreateSubGroupContainer(subGroupName);
                    subMap[subGroupName] = subGroupElement;
                    statsContentHolder.Add(subGroupElement);
                }
                targetContainer = subGroupElement;
            }

            // 3. Create the row element for this handle
            var rowContainer = new VisualElement();
            rowContainer.AddToClassList("stat-row");
            rowContainer.userData = handle;

            var keyLabel = new Label(handle.Name + ":");
            keyLabel.AddToClassList("stat-label");

            var binding = new RowBinding
            {
                Handle = handle,
                KeyLabel = keyLabel
            };

            var widgetRoot = CreateWidgetRoot(handle, binding);
            rowContainer.Add(keyLabel);
            rowContainer.Add(widgetRoot);
            InsertRowContainerSorted(targetContainer, rowContainer, handle);

            ApplyRowStyles(rowContainer, boxOverrides);

            rowBindings.Add(binding);
        }

        private void InsertRowContainerSorted(VisualElement container, VisualElement rowContainer, IMonitorHandle handle)
        {
            bool inserted = false;

            for (int i = 0; i < container.childCount; i++)
            {
                var child = container[i];
                if (child.userData is IMonitorHandle childHandle)
                {
                    if (handle.Metadata.Order < childHandle.Metadata.Order)
                    {
                        container.Insert(i, rowContainer);
                        inserted = true;
                        break;
                    }
                }
            }

            if (!inserted)
            {
                container.Add(rowContainer);
            }
        }

        private MonitorBoxOverrides FindBoxOverrides(string groupName, object target)
        {
            var overridesInScene = FindObjectsOfType<MonitorBoxOverrides>();
            foreach (var ov in overridesInScene)
            {
                if (ov != null && ov.BoxId == groupName)
                    return ov;
            }

            if (target is Component component && component != null)
            {
                var localOverrides = component.GetComponent<MonitorBoxOverrides>();
                if (localOverrides != null)
                    return localOverrides;
            }

            return null;
        }

        private void PruneEmptyBoxesAndSubGroups()
        {
            var emptyGroups = new List<string>();

            foreach (var groupPair in groupToBoxMap.ToList())
            {
                var groupName = groupPair.Key;
                var boxElement = groupPair.Value;
                var statsContentHolder = boxElement.Q<VisualElement>(className: "stats-content-holder");

                if (statsContentHolder == null)
                    continue;

                if (groupSubCardMap.TryGetValue(groupName, out var subMap))
                {
                    var emptySubGroups = new List<string>();

                    foreach (var subPair in subMap.ToList())
                    {
                        var subGroupName = subPair.Key;
                        var subGroupElement = subPair.Value;

                        int rowCount = subGroupElement.Query<VisualElement>(className: "stat-row").ToList().Count;
                        if (rowCount == 0)
                        {
                            if (subGroupElement.parent != null)
                                subGroupElement.parent.Remove(subGroupElement);
                            emptySubGroups.Add(subGroupName);
                        }
                    }

                    foreach (var emptySub in emptySubGroups)
                        subMap.Remove(emptySub);

                    if (subMap.Count == 0)
                        groupSubCardMap.Remove(groupName);
                }

                int totalRowsInBox = boxElement.Query<VisualElement>(className: "stat-row").ToList().Count;
                if (totalRowsInBox == 0)
                {
                    if (boxElement.parent != null)
                        boxElement.parent.Remove(boxElement);
                    emptyGroups.Add(groupName);
                }
            }

            foreach (var emptyGroup in emptyGroups)
            {
                groupToBoxMap.Remove(emptyGroup);
            }
        }

        private VisualElement CreateEmptyBox(string title, MonitorBoxOverrides boxOverrides)
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

            box.Add(statsContainer);

            ApplyBoxOverridesOnly(box, boxOverrides);

            toggleButton.clicked += () =>
            {
                statsContainer.ToggleInClassList("stats-content-holder--collapsed");
                bool isCollapsed = statsContainer.ClassListContains("stats-content-holder--collapsed");
                toggleButton.text = isCollapsed ? "+" : "−";
                statsContainer.style.display = isCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            };

            return box;
        }

        private VisualElement CreateSubGroupContainer(string subGroupName)
        {
            var subGroup = new VisualElement();
            subGroup.AddToClassList("sub-group-container");

            var subHeader = new Label(subGroupName);
            subHeader.AddToClassList("sub-group-header");
            subGroup.Add(subHeader);

            return subGroup;
        }

        private void ApplyBoxOverridesOnly(VisualElement box, MonitorBoxOverrides boxOverrides)
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

            if (boxOverrides.OverrideFontSize)
            {
                foreach (var child in box.Children())
                {
                    if (child is Label label)
                        label.style.fontSize = boxOverrides.FontSize;
                }
            }
        }

        private void ApplyRowStyles(VisualElement row, MonitorBoxOverrides boxOverrides)
        {
            if (boxOverrides == null)
                return;

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

        private bool IsBoxAllowedForThisPanel(MonitorBoxOverrides boxOverrides)
        {
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

        private VisualElement CreateWidgetRoot(IMonitorHandle handle, RowBinding binding)
        {
            var root = new VisualElement();
            root.AddToClassList("stat-widget");
            root.AddToClassList($"stat-widget--{handle.Metadata.WidgetType.ToString().ToLowerInvariant()}");

            var rawValue = handle.GetValueRaw();

            switch (handle.Metadata.WidgetType)
            {
                case MonitorWidgetType.Toggle:
                {
                    var toggle = new Toggle();
                    toggle.SetEnabled(false);
                    toggle.value = ToBool(rawValue);
                    root.Add(toggle);

                    binding.ToggleControl = toggle;
                    break;
                }

                case MonitorWidgetType.Slider:
                {
                    var container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Row;
                    container.style.alignItems = Align.Center;

                    var bar = new ProgressBar();
                    bar.value = ToPercent(rawValue, handle.Metadata.Min, handle.Metadata.Max);
                    bar.style.flexGrow = 1;
                    bar.AddToClassList("compact-progress");

                    var valueLabel = new Label(ValueFormatter.FormatValue(rawValue));
                    valueLabel.AddToClassList("stat-value");
                    valueLabel.style.marginLeft = 8;

                    container.Add(bar);
                    container.Add(valueLabel);
                    root.Add(container);

                    binding.ProgressControl = bar;
                    binding.ValueLabel = valueLabel;
                    break;
                }

                case MonitorWidgetType.Progress:
                {
                    var container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Row;
                    container.style.alignItems = Align.Center;

                    var bar = new ProgressBar();
                    bar.value = ToPercent(rawValue, handle.Metadata.Min, handle.Metadata.Max);
                    bar.style.flexGrow = 1;
                    bar.AddToClassList("compact-progress");

                    var valueLabel = new Label(ValueFormatter.FormatValue(rawValue));
                    valueLabel.AddToClassList("stat-value");
                    valueLabel.style.marginLeft = 8;

                    container.Add(bar);
                    container.Add(valueLabel);
                    root.Add(container);

                    binding.ProgressControl = bar;
                    binding.ValueLabel = valueLabel;
                    break;
                }

                case MonitorWidgetType.InputValue:
                case MonitorWidgetType.Value:
                default:
                {
                    var valueLabel = new Label(ValueFormatter.FormatValue(rawValue));
                    valueLabel.AddToClassList("stat-value");
                    root.Add(valueLabel);

                    binding.ValueLabel = valueLabel;
                    break;
                }
            }

            binding.WidgetRoot = root;
            return root;
        }

        private void Update()
        {
            if (resolvedConfig.PanelSettings == null)
                return;

            if (lastAppliedAnchor != resolvedConfig.Anchor)
            {
                ApplyLayoutAnchor(resolvedConfig.Anchor);
                lastAppliedAnchor = resolvedConfig.Anchor;
            }

            // Clamp update frequency to a maximum of 60Hz (0.0166s) to avoid excessive string allocations at 1000+ FPS
            float interval = Mathf.Max(0.0166f, resolvedConfig.UpdateInterval);
            if (Time.time - lastUpdateTime >= interval)
            {
                RefreshMonitoredValues();
                lastUpdateTime = Time.time;
            }
        }

        private void RefreshMonitoredValues()
        {
            // 1. Prune any rows associated with targets that were destroyed at runtime
            List<object> destroyedTargets = null;
            for (int i = 0; i < rowBindings.Count; i++)
            {
                var binding = rowBindings[i];
                if (binding != null && MonitoringHelper.IsDestroyed(binding.Handle.Target))
                {
                    destroyedTargets ??= new List<object>();
                    if (!destroyedTargets.Contains(binding.Handle.Target))
                        destroyedTargets.Add(binding.Handle.Target);
                }
            }

            if (destroyedTargets != null)
            {
                foreach (var target in destroyedTargets)
                {
                    OnTargetUnregistered(target);
                }
            }

            // 2. Refresh active values only when changed
            foreach (var binding in rowBindings)
            {
                if (binding == null || binding.Handle == null || binding.KeyLabel == null)
                    continue;

                if (!binding.Handle.UpdateValueAndCheckIfChanged())
                    continue;

                switch (binding.Handle.Metadata.WidgetType)
                {
                    case MonitorWidgetType.Toggle:
                        if (binding.ToggleControl != null)
                            binding.ToggleControl.SetValueWithoutNotify(binding.Handle.GetValueBool());
                        break;

                    case MonitorWidgetType.Slider:
                    case MonitorWidgetType.Progress:
                        if (binding.ProgressControl != null)
                        {
                            float min = binding.Handle.Metadata.Min;
                            float max = binding.Handle.Metadata.Max;
                            float val = binding.Handle.GetValueFloat();
                            float percent = Mathf.Approximately(min, max) ? 0f : Mathf.Clamp01(Mathf.InverseLerp(min, max, val)) * 100f;
                            binding.ProgressControl.value = percent;
                            
                            if (binding.ValueLabel != null)
                                binding.ValueLabel.text = binding.Handle.GetValueString();
                        }
                        break;

                    case MonitorWidgetType.InputValue:
                    case MonitorWidgetType.Value:
                    default:
                        if (binding.ValueLabel != null)
                            binding.ValueLabel.text = binding.Handle.GetValueString();
                        break;
                }
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

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            // Empty: layout is handled via USS stylesheets to maintain consistency
        }

        private void UpdateResponsiveLayout(float rootWidth)
        {
            // Empty: layout is handled via USS stylesheets to maintain consistency
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

        #region UTILS

        private static bool ToBool(object rawValue)
        {
            try
            {
                return rawValue != null && Convert.ToBoolean(rawValue);
            }
            catch
            {
                return false;
            }
        }

        private static float ToPercent(object rawValue, float min, float max)
        {
            if (!TryGetFloat(rawValue, out var numericValue))
                return 0f;

            if (Mathf.Approximately(min, max))
                return 0f;

            var normalized = Mathf.InverseLerp(min, max, numericValue);
            return Mathf.Clamp01(normalized) * 100f;
        }

        private static bool TryGetFloat(object rawValue, out float value)
        {
            try
            {
                if (rawValue == null)
                {
                    value = 0f;
                    return false;
                }

                value = Convert.ToSingle(rawValue);
                return true;
            }
            catch
            {
                value = 0f;
                return false;
            }
        }

        #endregion

        private sealed class RowBinding
        {
            public IMonitorHandle Handle;
            public Label KeyLabel;
            public Label ValueLabel;
            public Toggle ToggleControl;
            public ProgressBar ProgressControl;
            public VisualElement WidgetRoot;
        }
    }
}