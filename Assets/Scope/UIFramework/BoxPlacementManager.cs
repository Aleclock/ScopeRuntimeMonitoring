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
            return positions;
        
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
        float stackOffset
    )
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