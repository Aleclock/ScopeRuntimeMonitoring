using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BoxPlacementManager
{
    private const float ColumnGap = 12f;
    private const float ColumnWrapTolerance = 12f;

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
            + 12f;

        var contentHeight = rowCount > 0
            ? (rowCount * definition.RowHeight) + ((rowCount - 1) * definition.RowSpacing)
            : 0f;

        var autoHeight = definition.TitleHeight
            + definition.PaddingTop
            + definition.PaddingBottom
            + contentHeight
            + 8f;

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

        var minMarginY = GetMinMarginY(definitions);
        var maxMarginY = GetMaxMarginY(definitions);
        var availableHeight = Mathf.Max(0f, safeArea.height - minMarginY - maxMarginY);
        var currentColumn = new PackedColumn();

        foreach (var definition in definitions)
        {
            var size = definition.PanelSize;
            var nextHeight = currentColumn.Boxes.Count == 0
                ? size.y
                : currentColumn.Height + ColumnGap + size.y;

            if (currentColumn.Boxes.Count > 0 && nextHeight > availableHeight + ColumnWrapTolerance)
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
        var right = safeArea.xMax - margin.x - columnOffset;
        var top = -(Screen.height - safeArea.yMax + margin.y);
        var bottom = -(Screen.height - safeArea.yMin - margin.y);

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

    private float GetMinMarginY(List<MonitorBoxDefinition> definitions)
    {
        var minMargin = float.MaxValue;

        foreach (var definition in definitions)
        {
            if (definition != null && definition.Margin.y < minMargin)
                minMargin = definition.Margin.y;
        }

        return minMargin == float.MaxValue ? 0f : minMargin;
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