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