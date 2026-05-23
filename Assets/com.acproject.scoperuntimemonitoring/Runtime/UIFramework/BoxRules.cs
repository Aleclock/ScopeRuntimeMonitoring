using System;
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
    Vector2 GetAnchoredPosition(Rect SafeArea, Vector2 panelSize, Vector2 margin);
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
        _query = string.IsNullOrEmpty(query) ? string.Empty : query.ToLowerInvariant();
    }

    public bool Include(IMonitorHandle handle)
    {
        if (handle == null || !handle.Enabled)
            return false;
        
        if (_query.Length == 0)
            return true;
        
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
            return typeCompare;
        
        var an = a?.Name ?? string.Empty;
        var bn = b?.Name ?? string.Empty;
        return string.Compare(an, bn, System.StringComparison.OrdinalIgnoreCase);
    }
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
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;

        var left = safeArea.xMin + margin.x;
        var right = safeArea.xMax - panelSize.x - margin.x;
        var top = -(screenHeight - safeArea.yMax + margin.y);
        var bottom = -(screenHeight - safeArea.yMin - panelSize.y - margin.y);

        switch (_anchor)
        {
            case BoxAnchor.TopLeft:
                return new Vector2(left, top);
            case BoxAnchor.TopRight:
                return new Vector2(right, top);
            case BoxAnchor.BottomLeft:
                return new Vector2(left, bottom);
            case BoxAnchor.BottomRight:
                return new Vector2(right, bottom);
            default:
                return new Vector2(left, top);
        }
    }
}

// TODO I could add center
public enum BoxAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}