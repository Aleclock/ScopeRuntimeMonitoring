# Step 6 Guide: Exact Copy-Paste Placement Manager

This is the follow-up to Step 5.

Short answer
------------
No, Step 5 is **not** the full copy-paste code version.
It explains the architecture.

This guide gives you the exact code to paste so that:

- each box declares its preferred screen anchor,
- boxes sharing the same anchor are stacked automatically,
- the title area is reserved,
- the scroll content starts below the title,
- placement stays separate from rendering.

What you will create
--------------------
You will update or add these files:

- `Assets/Scope/UIFramework/MonitorBoxDefinition.cs`
- `Assets/Scope/UIFramework/BoxPlacementManager.cs`
- `Assets/Scope/UIFramework/MonitorBoxView.cs`
- `Assets/Scope/UIFramework/MonitorPanelController.cs`
- `Assets/Scope/UIFramework/DefaultBoxDefinitions.cs`

Step 1: Update `MonitorBoxDefinition.cs`
----------------------------------------
Add an anchor, title height, and keep the class as pure data.

```csharp
using UnityEngine;

[System.Serializable]
public sealed class MonitorBoxDefinition
{
    public string Id;
    public string Title;

    public BoxAnchor Anchor = BoxAnchor.TopLeft;
    public Vector2 PanelSize = new Vector2(420f, 260f);
    public Vector2 Margin = new Vector2(12f, 12f);

    public int FontSize = 17;
    public float TitleHeight = 30f;
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

    public MonitorBoxDefinition()
    {
    }
}
```

Why this matters:

- `Anchor` tells the placer where the box wants to live.
- `TitleHeight` reserves space for the header.
- `MonitorBoxDefinition` stays simple and reusable.

Step 2: Add `BoxPlacementManager.cs`
------------------------------------
Create `Assets/Scope/UIFramework/BoxPlacementManager.cs`.

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BoxPlacementManager
{
    private const float BoxGap = 10f;

    public Dictionary<string, Vector2> BuildPositions(IReadOnlyList<MonitorBoxDefinition> definitions)
    {
        var positions = new Dictionary<string, Vector2>();
        if (definitions == null || definitions.Count == 0)
        {
            return positions;
        }

        var safeArea = Screen.safeArea;

        foreach (var group in definitions
                     .Where(definition => definition != null && !string.IsNullOrEmpty(definition.Id))
                     .GroupBy(definition => definition.Anchor))
        {
            var ordered = group.OrderBy(definition => definition.Title).ToList();
            var stackOffset = 0f;

            foreach (var definition in ordered)
            {
                positions[definition.Id] = GetPositionForAnchor(
                    group.Key,
                    safeArea,
                    definition.PanelSize,
                    definition.Margin,
                    stackOffset
                );

                stackOffset += definition.PanelSize.y + BoxGap;
            }
        }

        return positions;
    }

    private Vector2 GetPositionForAnchor(
        BoxAnchor anchor,
        Rect safeArea,
        Vector2 panelSize,
        Vector2 margin,
        float stackOffset)
    {
        var left = safeArea.xMin + margin.x;
        var right = safeArea.xMax - panelSize.x - margin.x;
        var top = -(Screen.height - safeArea.yMax + margin.y);
        var bottom = -(Screen.height - safeArea.yMin - panelSize.y - margin.y);

        switch (anchor)
        {
            case BoxAnchor.TopLeft:
                return new Vector2(left, top - stackOffset);
            case BoxAnchor.TopRight:
                return new Vector2(right, top - stackOffset);
            case BoxAnchor.BottomLeft:
                return new Vector2(left, bottom + stackOffset);
            case BoxAnchor.BottomRight:
                return new Vector2(right, bottom + stackOffset);
            default:
                return new Vector2(left, top - stackOffset);
        }
    }
}
```

Why this works:

- boxes with the same anchor are grouped together,
- boxes inside each group get a vertical stack offset,
- the manager returns final positions, so rendering stays separate.

Step 3: Update `MonitorBoxView.cs`
----------------------------------
The box view should render the panel, title, and rows — but not decide stacking.

Paste this version.

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonitorBoxView
{
    private readonly MonitorBoxDefinition _definition;
    private readonly List<MonitorRowView> _rows = new List<MonitorRowView>();

    private readonly RectTransform _panelRect;
    private readonly RectTransform _contentRect;

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

        var title = CreateTitle(panelObject.transform, definition);
        CreateScrollLayout(panelObject.transform, definition);
        CreateRows(handles, definition, title.font);
    }

    private TextMeshProUGUI CreateTitle(Transform parent, MonitorBoxDefinition definition)
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
        titleRect.sizeDelta = new Vector2(-20f, definition.TitleHeight);

        return title;
    }

    private void CreateScrollLayout(Transform parent, MonitorBoxDefinition definition)
    {
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        var scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);

        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(0f, 0f);
        scrollRectTransform.offsetMax = new Vector2(0f, -definition.TitleHeight);

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);

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

    private void CreateRows(IReadOnlyList<IMonitorHandle> handles, MonitorBoxDefinition definition, TMP_FontAsset fontAsset)
    {
        foreach (var handle in handles)
        {
            var row = new MonitorRowView(
                _contentRect,
                handle,
                $"{handle.Name}: {handle.GetValueString()}",
                definition.FontSize,
                definition.RowHeight,
                definition.RowColor,
                definition.TextColor,
                fontAsset
            );

            _rows.Add(row);
        }
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        _panelRect.anchoredPosition = anchoredPosition;
    }

    public void RefreshRows()
    {
        foreach (var row in _rows)
        {
            row.Refresh();
        }
    }
}
```

Why this matters:

- the box size is still handled here,
- the title height is reserved,
- placement is now a separate step.

Step 4: Update `MonitorPanelController.cs`
------------------------------------------
This controller creates the definitions, builds positions, then applies them.

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

        var placementManager = new BoxPlacementManager();
        var positions = placementManager.BuildPositions(_definitions);

        foreach (var definition in _definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            var boxView = new MonitorBoxView(transform, definition, handlesForBox);

            if (positions.TryGetValue(definition.Id, out var position))
            {
                boxView.SetPosition(position);
            }

            _boxViews.Add(boxView);
        }
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

    private void Update()
    {
        foreach (var boxView in _boxViews)
        {
            boxView.RefreshRows();
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

Step 5: Update `DefaultBoxDefinitions.cs`
-----------------------------------------
Use the anchor field explicitly.

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
            Anchor = BoxAnchor.TopLeft,
            PanelSize = new Vector2(430f, 280f),
            Margin = new Vector2(12f, 12f),
            FontSize = 17,
            TitleHeight = 30f,
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
            Anchor = BoxAnchor.TopLeft,
            PanelSize = new Vector2(430f, 280f),
            Margin = new Vector2(12f, 12f),
            FontSize = 17,
            TitleHeight = 30f,
            RowHeight = 26f,
            RowSpacing = 4f,
            FilterRule = new NameContainsFilterRule(""),
            SortRule = new NameAscendingSortRule(),
            LayoutRule = new ScreenAnchorLayoutRule(BoxAnchor.TopLeft)
        };

        return new List<MonitorBoxDefinition>
        {
            gameplayBox,
            transformBox
        };
    }
}
```

What should happen now
----------------------
With the code above:

- `Gameplay` is placed at `TopLeft`
- `Transform` also wants `TopLeft`
- the placement manager stacks `Transform` below `Gameplay`
- the scroll area starts below the title
- the rows no longer get trimmed by the title region

Small rule of thumb
-------------------
Use this separation:

- **definition**: anchor, size, colors, spacing
- **placement manager**: where each box ends up
- **box view**: how the box renders itself

That keeps the system flexible and easy to extend.

If you want, I can make the next guide for a **ScriptableObject-based box config system** so you can edit anchors and sizes from the Inspector instead of code.
