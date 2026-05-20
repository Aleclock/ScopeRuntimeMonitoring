# Step 7: Auto-Size Panels and Wrap Boxes Into New Columns

This guide explains how to make each monitor box:

1. Auto-calculate its height from the number of rows.
2. Auto-calculate its width from the content, with a cap so it does not become too wide.
3. Wrap into a new column when the current column would go outside the viewport.

The important idea is this:

- `MonitorBoxDefinition` stores the sizing settings.
- `BoxPlacementManager` calculates the final panel size and the box positions.
- `MonitorPanelController` applies the calculated size before creating the view.
- `MonitorBoxView` does not need structural changes, because it already uses `definition.PanelSize`.

## 1. Auto-size formulas

Use these formulas:

- Panel height:

  `height = titleHeight + paddingTop + paddingBottom + rowCount * rowHeight + max(0, rowCount - 1) * rowSpacing + extraChrome`

- Panel width:

  `width = max(estimatedTitleWidth, estimatedRowWidth) + paddingLeft + paddingRight + extraChrome`

Then clamp both values:

- `width = clamp(width, minPanelWidth, maxPanelWidth)`
- `height = clamp(height, minPanelHeight, maxPanelHeight)`

That gives you automatic sizing, but still prevents a box from growing too large.

## 2. Column wrapping rule

When the boxes for one anchor would exceed the available safe-area height:

- keep stacking downward in the current column until there is no more room;
- then start a new column next to it;
- for left anchors, the new column goes to the right;
- for right anchors, the new column goes to the left.

This keeps the UI readable when there are many panels.

## 3. Replace `MonitorBoxDefinition.cs`

Replace the full contents of [Assets/Scope/UIFramework/MonitorBoxDefinition.cs](../Assets/Scope/UIFramework/MonitorBoxDefinition.cs) with this version:

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

    public bool AutoSizeWidth = true;
    public bool AutoSizeHeight = true;
    public float MinPanelWidth = 280f;
    public float MaxPanelWidth = 460f;
    public float MinPanelHeight = 160f;
    public float MaxPanelHeight = 340f;
    public float EstimatedCharacterWidth = 8.5f;

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

## 4. Replace `BoxPlacementManager.cs`

Replace the full contents of [Assets/Scope/UIFramework/BoxPlacementManager.cs](../Assets/Scope/UIFramework/BoxPlacementManager.cs) with this version:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BoxPlacementManager
{
    private const float ColumnGap = 12f;

    public Vector2 CalculatePanelSize(MonitorBoxDefinition definition, IReadOnlyList<IMonitorHandle> handles)
    {
        if (definition == null)
            return Vector2.zero;

        if (!definition.AutoSizeWidth && !definition.AutoSizeHeight)
            return definition.PanelSize;

        var rowCount = handles?.Count ?? 0;
        var longestRowLength = GetLongestRowLength(handles);

        var titleLength = definition.Title != null ? definition.Title.Length : 0;
        var estimatedCharacterWidth = Mathf.Max(4f, definition.EstimatedCharacterWidth);
        var estimatedTitleWidth = titleLength * (definition.FontSize + 1) * 0.6f;
        var estimatedRowWidth = longestRowLength * estimatedCharacterWidth;

        var autoWidth = Mathf.Max(estimatedTitleWidth, estimatedRowWidth)
            + definition.PaddingLeft
            + definition.PaddingRight
            + 24f;

        var contentHeight = rowCount > 0
            ? (rowCount * definition.RowHeight) + ((rowCount - 1) * definition.RowSpacing)
            : 0f;

        var autoHeight = definition.TitleHeight
            + definition.PaddingTop
            + definition.PaddingBottom
            + contentHeight
            + 20f;

        var width = definition.AutoSizeWidth
            ? Mathf.Clamp(autoWidth, definition.MinPanelWidth, definition.MaxPanelWidth)
            : definition.PanelSize.x;

        var height = definition.AutoSizeHeight
            ? Mathf.Clamp(autoHeight, definition.MinPanelHeight, definition.MaxPanelHeight)
            : definition.PanelSize.y;

        return new Vector2(width, height);
    }

    public Dictionary<string, Vector2> BuildPositions(IReadOnlyList<MonitorBoxDefinition> definitions)
    {
        var positions = new Dictionary<string, Vector2>();

        if (definitions == null || definitions.Count == 0)
            return positions;

        var safeArea = Screen.safeArea;

        foreach (var group in definitions
                     .Where(definition => definition != null && !string.IsNullOrEmpty(definition.Id))
                     .GroupBy(definition => definition.Anchor))
        {
            var ordered = group.OrderBy(definition => definition.Title).ToList();
            var columns = BuildColumns(ordered, safeArea);
            var columnOffset = 0f;

            foreach (var column in columns)
            {
                foreach (var box in column.Boxes)
                {
                    positions[box.Definition.Id] = GetPositionForAnchor(
                        group.Key,
                        safeArea,
                        box.Definition.PanelSize,
                        box.Definition.Margin,
                        columnOffset,
                        box.VerticalOffset
                    );
                }

                columnOffset += column.Width + ColumnGap;
            }
        }

        return positions;
    }

    private List<PackedColumn> BuildColumns(List<MonitorBoxDefinition> definitions, Rect safeArea)
    {
        var columns = new List<PackedColumn>();

        if (definitions == null || definitions.Count == 0)
            return columns;

        var availableHeight = Mathf.Max(0f, safeArea.height - (GetMaxMarginY(definitions) * 2f));
        var currentColumn = new PackedColumn();

        foreach (var definition in definitions)
        {
            var size = definition.PanelSize;
            var nextHeight = currentColumn.Boxes.Count == 0
                ? size.y
                : currentColumn.Height + ColumnGap + size.y;

            if (currentColumn.Boxes.Count > 0 && nextHeight > availableHeight)
            {
                columns.Add(currentColumn);
                currentColumn = new PackedColumn();
            }

            var verticalOffset = currentColumn.Boxes.Count == 0
                ? 0f
                : currentColumn.Height + ColumnGap;

            currentColumn.Boxes.Add(new PackedBox(definition, size, verticalOffset));
            currentColumn.Width = Mathf.Max(currentColumn.Width, size.x);
            currentColumn.Height = verticalOffset + size.y;
        }

        if (currentColumn.Boxes.Count > 0)
            columns.Add(currentColumn);

        return columns;
    }

    private Vector2 GetPositionForAnchor(
        BoxAnchor anchor,
        Rect safeArea,
        Vector2 panelSize,
        Vector2 margin,
        float columnOffset,
        float stackOffset)
    {
        var left = safeArea.xMin + margin.x + columnOffset;
        var right = safeArea.xMax - panelSize.x - margin.x - columnOffset;
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

    private int GetLongestRowLength(IReadOnlyList<IMonitorHandle> handles)
    {
        var longest = 0;

        if (handles == null)
            return longest;

        foreach (var handle in handles)
        {
            if (handle == null)
                continue;

            var nameLength = handle.Name != null ? handle.Name.Length : 0;
            var valueLength = handle.GetValueString() != null ? handle.GetValueString().Length : 0;
            var totalLength = nameLength + 2 + valueLength;

            if (totalLength > longest)
                longest = totalLength;
        }

        return longest;
    }

    private float GetMaxMarginY(List<MonitorBoxDefinition> definitions)
    {
        var maxMargin = 0f;

        foreach (var definition in definitions)
        {
            if (definition != null && definition.Margin.y > maxMargin)
                maxMargin = definition.Margin.y;
        }

        return maxMargin;
    }

    private sealed class PackedColumn
    {
        public readonly List<PackedBox> Boxes = new List<PackedBox>();
        public float Width;
        public float Height;
    }

    private sealed class PackedBox
    {
        public readonly MonitorBoxDefinition Definition;
        public readonly Vector2 Size;
        public readonly float VerticalOffset;

        public PackedBox(MonitorBoxDefinition definition, Vector2 size, float verticalOffset)
        {
            Definition = definition;
            Size = size;
            VerticalOffset = verticalOffset;
        }
    }
}
```

## 5. Replace `MonitorPanelController.cs`

Replace the full contents of [Assets/Scope/UIFramework/MonitorPanelController.cs](../Assets/Scope/UIFramework/MonitorPanelController.cs) with this version:

```csharp
using System.Collections.Generic;

public class MonitorPanelController : DebugPanelBase
{
    private readonly List<MonitorBoxView> _boxViews = new List<MonitorBoxView>();
    private List<MonitorBoxDefinition> _definitions;

    public override void Initialize()
    {
        _definitions = DefaultBoxDefinitions.Create();
        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var placementManager = new BoxPlacementManager();
        var handlesByBoxId = new Dictionary<string, IReadOnlyList<IMonitorHandle>>();

        foreach (var definition in _definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            handlesByBoxId[definition.Id] = handlesForBox;
            definition.PanelSize = placementManager.CalculatePanelSize(definition, handlesForBox);
        }

        var positions = placementManager.BuildPositions(_definitions);

        foreach (var definition in _definitions)
        {
            var boxView = new MonitorBoxView(transform, definition, handlesByBoxId[definition.Id]);

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
            boxView.RefreshRows();
    }

    public override void InizializePanelData()
    {
        
    }

    public override void UpdatePanelData()
    {
        
    }
}
```

## 6. Replace `DefaultBoxDefinitions.cs`

Replace the full contents of [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](../Assets/Scope/UIFramework/DefaultBoxDefinitions.cs) with this version:

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
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            MinPanelWidth = 280f,
            MaxPanelWidth = 460f,
            MinPanelHeight = 160f,
            MaxPanelHeight = 340f,
            EstimatedCharacterWidth = 8.5f,
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
        };
    }
}
```

## 7. What you should not change

You do not need to change `MonitorBoxView.cs` for this step, because it already reads `definition.PanelSize` when it builds the panel.

The key rule is:

- calculate the panel size first,
- then create the box view,
- then set the position.

## 8. Why this works

This approach keeps the responsibilities clean:

- `MonitorBoxDefinition` decides what the panel is allowed to do.
- `BoxPlacementManager` calculates how large the panel should be and where it should go.
- `MonitorPanelController` applies those decisions.
- `MonitorBoxView` only renders the result.

That separation is what makes it easy to add new rules later, like drag repositioning or per-box layouts.

## 9. Suggested next experiment

After you paste these changes, try this order:

1. Add a second or third box definition.
2. Put enough rows in one box so it would normally exceed the viewport.
3. Confirm the next box starts in a new column instead of overlapping.
4. Tune `MaxPanelWidth` and `MaxPanelHeight` until the UI feels right.

If you want, the next guide can be about making the panel height based on the actual number of visible rows but still letting the content scroll inside the panel.
