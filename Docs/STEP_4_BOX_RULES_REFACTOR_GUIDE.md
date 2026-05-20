# Step 4 Guide: Per-Box Rules, Flexible Layout, and Refactor Map

This guide continues your learning path with the same copy-paste style.

Goal
----
Refactor from one rigid `MonitorsPanel` into a flexible system where each box defines its own:

- positioning rules,
- filter/sort rules,
- visual settings,
- size and spacing.

You will keep your current runtime (`Monitor`, `MonitoringRegistry`, `IMonitorHandle`) and only restructure UI architecture.

What You Will Build
-------------------
A small box-driven UI framework made of:

- `MonitorPanelController` (creates boxes),
- `MonitorBoxDefinition` (per-box configuration),
- `IBoxFilterRule` / `IBoxSortRule` / `IBoxLayoutRule` (box behavior),
- `MonitorBoxView` (renders one box),
- `MonitorRowView` (renders one monitored row).

By the end, adding a new box is mostly adding a new definition + rule classes.

Folder Suggestion
-----------------
Create these files under `Assets/Scope/UIFramework/` to keep structure clean.

- `MonitorPanelController.cs`
- `MonitorBoxDefinition.cs`
- `BoxRules.cs`
- `MonitorBoxView.cs`
- `MonitorRowView.cs`
- `DefaultBoxDefinitions.cs`

You can keep your existing files untouched while migrating.

Step 1: Add Rule Interfaces
---------------------------
Create `Assets/Scope/UIFramework/BoxRules.cs`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public interface IBoxFilterRule
{
    bool Include(IMonitorHandle handle);
}

public interface IBoxSortRule
{
    int Compare(IMonitorHandle a, IMonitorHandle b);
}

public interface IBoxLayoutRule
{
    Vector2 GetAnchoredPosition(Rect safeArea, Vector2 panelSize, Vector2 margin);
}

public sealed class IncludeAllFilterRule : IBoxFilterRule
{
    public bool Include(IMonitorHandle handle)
    {
        return handle != null && handle.Enabled;
    }
}

public sealed class NameContainsFilterRule : IBoxFilterRule
{
    private readonly string _query;

    public NameContainsFilterRule(string query)
    {
        _query = string.IsNullOrWhiteSpace(query) ? string.Empty : query.ToLowerInvariant();
    }

    public bool Include(IMonitorHandle handle)
    {
        if (handle == null || !handle.Enabled)
        {
            return false;
        }

        if (_query.Length == 0)
        {
            return true;
        }

        return handle.Name != null && handle.Name.ToLowerInvariant().Contains(_query);
    }
}

public sealed class NameAscendingSortRule : IBoxSortRule
{
    public int Compare(IMonitorHandle a, IMonitorHandle b)
    {
        var an = a?.Name ?? string.Empty;
        var bn = b?.Name ?? string.Empty;
        return string.Compare(an, bn, System.StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TypeThenNameSortRule : IBoxSortRule
{
    public int Compare(IMonitorHandle a, IMonitorHandle b)
    {
        var at = a?.ValueType?.Name ?? string.Empty;
        var bt = b?.ValueType?.Name ?? string.Empty;

        var typeCompare = string.Compare(at, bt, System.StringComparison.OrdinalIgnoreCase);
        if (typeCompare != 0)
        {
            return typeCompare;
        }

        var an = a?.Name ?? string.Empty;
        var bn = b?.Name ?? string.Empty;
        return string.Compare(an, bn, System.StringComparison.OrdinalIgnoreCase);
    }
}

public enum BoxAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class ScreenAnchorLayoutRule : IBoxLayoutRule
{
    private readonly BoxAnchor _anchor;

    public ScreenAnchorLayoutRule(BoxAnchor anchor)
    {
        _anchor = anchor;
    }

    public Vector2 GetAnchoredPosition(Rect safeArea, Vector2 panelSize, Vector2 margin)
    {
        var left = safeArea.xMin + margin.x;
        var right = safeArea.xMax - margin.x;
        var top = safeArea.yMax - margin.y;
        var bottom = safeArea.yMin + margin.y;

        switch (_anchor)
        {
            case BoxAnchor.TopLeft:
                return new Vector2(left, top);
            case BoxAnchor.TopRight:
                return new Vector2(right - panelSize.x, top);
            case BoxAnchor.BottomLeft:
                return new Vector2(left, bottom + panelSize.y);
            case BoxAnchor.BottomRight:
                return new Vector2(right - panelSize.x, bottom + panelSize.y);
            default:
                return new Vector2(left, top);
        }
    }
}
```

Why this step matters:

- Behavior moves out of panel code.
- Each box can pick rules independently.
- You can add new rule classes without touching rendering code.

Step 2: Add Box Definition Model
--------------------------------
Create `Assets/Scope/UIFramework/MonitorBoxDefinition.cs`.

```csharp
using UnityEngine;

[System.Serializable]
public sealed class MonitorBoxDefinition
{
    public string Id;
    public string Title;

    public Vector2 PanelSize = new Vector2(420f, 260f);
    public Vector2 Margin = new Vector2(12f, 12f);

    public int FontSize = 17;
    public float RowHeight = 26f;
    public float RowSpacing = 4f;

    public Color PanelColor = new Color(0f, 0f, 0f, 0.58f);
    public Color RowColor = new Color(1f, 1f, 1f, 0.05f);
    public Color TextColor = Color.white;
    public Color TitleColor = new Color(1f, 1f, 1f, 0.85f);

    public int PaddingLeft = 10;
    public int PaddingRight = 10;
    public int PaddingTop = 10;
    public int PaddingBottom = 10;

    public IBoxFilterRule FilterRule;
    public IBoxSortRule SortRule;
    public IBoxLayoutRule LayoutRule;

    // Parameterless constructor required for object initializers
    public MonitorBoxDefinition()
    {
    }
}
```

Why this step matters:

- Box properties become data instead of hardcoded values.
- The same renderer can create many different boxes.
- Parameterless constructor enables object initializer syntax (e.g., `new MonitorBoxDefinition { Id = "gameplay", ... }`)

Step 3: Add Reusable Row View
-----------------------------
Create `Assets/Scope/UIFramework/MonitorRowView.cs`.

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonitorRowView
{
    private readonly IMonitorHandle _handle;
    private readonly TextMeshProUGUI _label;

    public MonitorRowView(
        Transform parent,
        IMonitorHandle handle,
        string initialText,
        int fontSize,
        float rowHeight,
        Color rowColor,
        Color textColor)
    {
        _handle = handle;

        var rowObject = new GameObject($"Row_{handle.Name}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        var rowImage = rowObject.GetComponent<Image>();
        rowImage.color = rowColor;

        var rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(rowObject.transform, false);

        _label = labelObject.AddComponent<TextMeshProUGUI>();
        _label.text = initialText;
        _label.fontSize = fontSize;
        _label.color = textColor;
        _label.alignment = TextAlignmentOptions.MidlineLeft;
        _label.enableWordWrapping = false;

        var labelRect = _label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
    }

    public void Refresh()
    {
        if (_handle == null)
        {
            return;
        }

        _label.text = $"{_handle.Name}: {_handle.GetValueString()}";
    }
}
```

Why this step matters:

- Row rendering logic is isolated.
- Easier to add icons/units/buttons per row later.

Step 4: Add Box View Renderer
-----------------------------
Create `Assets/Scope/UIFramework/MonitorBoxView.cs`.

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonitorBoxView
{
    private readonly MonitorBoxDefinition _definition;
    private readonly List<MonitorRowView> _rows = new List<MonitorRowView>();

    private RectTransform _panelRect;
    private RectTransform _contentRect;

    public MonitorBoxView(Transform parent, MonitorBoxDefinition definition, IReadOnlyList<IMonitorHandle> handles)
    {
        _definition = definition;

        var panelObject = new GameObject($"Box_{definition.Id}", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = definition.PanelColor;

        _panelRect = panelObject.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0f, 1f);
        _panelRect.anchorMax = new Vector2(0f, 1f);
        _panelRect.pivot = new Vector2(0f, 1f);
        _panelRect.sizeDelta = definition.PanelSize;

        CreateTitle(panelObject.transform, definition);
        CreateScrollLayout(panelObject.transform, definition);
        CreateRows(handles, definition);

        ApplyLayout(definition);
    }

    private void CreateTitle(Transform parent, MonitorBoxDefinition definition)
    {
        var titleObject = new GameObject("Title", typeof(RectTransform));
        titleObject.transform.SetParent(parent, false);

        var title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = definition.Title;
        title.fontSize = definition.FontSize + 1;
        title.color = definition.TitleColor;
        title.alignment = TextAlignmentOptions.TopLeft;

        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(10f, -8f);
        titleRect.sizeDelta = new Vector2(-20f, 24f);
    }

    private void CreateScrollLayout(Transform parent, MonitorBoxDefinition definition)
    {
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        var scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);

        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(0f, 0f);
        scrollRectTransform.offsetMax = new Vector2(0f, -30f);

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var mask = scrollObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);

        var viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);

        var viewportMask = viewportObject.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        var viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);

        _contentRect = contentObject.GetComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = definition.RowSpacing;
        layout.padding = new RectOffset(
            definition.PaddingLeft,
            definition.PaddingRight,
            definition.PaddingTop,
            definition.PaddingBottom);

        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = _contentRect;
    }

    private void CreateRows(IReadOnlyList<IMonitorHandle> handles, MonitorBoxDefinition definition)
    {
        for (int i = 0; i < handles.Count; i++)
        {
            var handle = handles[i];
            var row = new MonitorRowView(
                _contentRect,
                handle,
                $"{handle.Name}: {handle.GetValueString()}",
                definition.FontSize,
                definition.RowHeight,
                definition.RowColor,
                definition.TextColor);

            _rows.Add(row);
        }
    }

    private void ApplyLayout(MonitorBoxDefinition definition)
    {
        var safeArea = Screen.safeArea;
        var position = definition.LayoutRule.GetAnchoredPosition(safeArea, definition.PanelSize, definition.Margin);
        _panelRect.anchoredPosition = position;
    }

    public void RefreshRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].Refresh();
        }
    }
}
```

Why this step matters:

- Box becomes a reusable renderer with independent rules.
- Positioning is rule-based and safe-area aware.
- Content uses scroll + vertical layout by default.

Step 5: Add Default Box Definitions
-----------------------------------
Create `Assets/Scope/UIFramework/DefaultBoxDefinitions.cs`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public static class DefaultBoxDefinitions
{
    public static List<MonitorBoxDefinition> Create()
    {
        var gameplayBox = new MonitorBoxDefinition
        {
            Id = "gameplay",
            Title = "Gameplay",
            PanelSize = new Vector2(430f, 280f),
            Margin = new Vector2(12f, 12f),
            FontSize = 17,
            RowHeight = 26f,
            RowSpacing = 4f,
            FilterRule = new NameContainsFilterRule(""),
            SortRule = new NameAscendingSortRule(),
            LayoutRule = new ScreenAnchorLayoutRule(BoxAnchor.TopLeft)
        };

        var transformBox = new MonitorBoxDefinition
        {
            Id = "transform",
            Title = "Transform",
            PanelSize = new Vector2(430f, 220f),
            Margin = new Vector2(12f, 12f),
            FontSize = 17,
            RowHeight = 26f,
            RowSpacing = 4f,
            FilterRule = new NameContainsFilterRule("position"),
            SortRule = new TypeThenNameSortRule(),
            LayoutRule = new ScreenAnchorLayoutRule(BoxAnchor.TopRight)
        };

        return new List<MonitorBoxDefinition>
        {
            gameplayBox,
            transformBox
        };
    }
}
```

Why this step matters:

- You now create multiple boxes with different behavior.
- Each box can have unique filter/sort/layout.

Step 6: Add Panel Controller
----------------------------
Create `Assets/Scope/UIFramework/MonitorPanelController.cs`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class MonitorPanelController : DebugPanelBase
{
    private readonly List<MonitorBoxView> _boxViews = new List<MonitorBoxView>();
    private List<MonitorBoxDefinition> _definitions;

    public override void Initialize()
    {
        _definitions = DefaultBoxDefinitions.Create();

        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());

        for (int i = 0; i < _definitions.Count; i++)
        {
            var def = _definitions[i];
            var handlesForBox = BuildHandleListForBox(allHandles, def);
            var boxView = new MonitorBoxView(transform, def, handlesForBox);
            _boxViews.Add(boxView);
        }
    }

    private IReadOnlyList<IMonitorHandle> BuildHandleListForBox(List<IMonitorHandle> source, MonitorBoxDefinition def)
    {
        var filtered = new List<IMonitorHandle>();

        for (int i = 0; i < source.Count; i++)
        {
            var handle = source[i];
            if (def.FilterRule == null || def.FilterRule.Include(handle))
            {
                filtered.Add(handle);
            }
        }

        if (def.SortRule != null)
        {
            filtered.Sort(def.SortRule.Compare);
        }

        return filtered;
    }

    private void Update()
    {
        for (int i = 0; i < _boxViews.Count; i++)
        {
            _boxViews[i].RefreshRows();
        }
    }

    public override void InizializePanelData()
    {
    }

    public override void UpdatePanelData()
    {
    }
}
```

Why this step matters:

- `MonitorPanelController` orchestrates boxes, not row rendering.
- You can keep adding/removing boxes with minimal code changes.

Step 7: Integrate Into Bootstrap
--------------------------------
Update your bootstrap to instantiate this controller instead of `MonitorsPanel`.

If your existing bootstrap currently creates `MonitorsPanel`, change only that line.

### Copy-paste bootstrap example

```csharp
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private void Start()
    {
        var example = FindObjectOfType<MonitoredExample>();
        if (example != null)
        {
            Monitor.StartMonitoring(example);
            DebugManager.Instance.CreateDebugPanel<MonitorPanelController>().Initialize();
        }
    }
}
```

Step 8: Verify Behavior
-----------------------
Run the scene and check:

1. You see two boxes (`Gameplay` and `Transform`).
2. Boxes are at different screen anchors.
3. Rows update live in both boxes.
4. If rows exceed box height, scrolling works.
5. Different filter rules produce different row sets.

If all five pass, your refactor succeeded.

Step 9: Common Improvements After This
--------------------------------------
Now you can add high customization safely:

- scriptable box definitions (instead of hardcoded defaults),
- per-box theme presets,
- drag-to-move box position with persisted user prefs,
- collapse/expand per box,
- per-box refresh rate.

This is now a future-proof architecture, not a single hardcoded panel.

Quick Troubleshooting
---------------------
If boxes do not appear:

- Confirm `Bootstrap` creates `MonitorPanelController`, not old `MonitorsPanel`.
- Confirm at least one handle exists in registry before panel initialization.
- Confirm `LayoutRule` is set in every box definition.

If rows appear but do not update:

- Confirm `Update()` in `MonitorPanelController` calls `RefreshRows()`.
- Confirm handles still return changing values from `GetValueString()`.

If scroll does not work:

- Confirm `ScrollRect.viewport` and `ScrollRect.content` are assigned.
- Confirm `ContentSizeFitter.verticalFit = PreferredSize`.

Next Step
---------
After this refactor, the next learning step should be:

- Step 5 guide: ScriptableObject box configs + runtime customization panel.

That will let each box truly define its own rule set through data, not code changes.
