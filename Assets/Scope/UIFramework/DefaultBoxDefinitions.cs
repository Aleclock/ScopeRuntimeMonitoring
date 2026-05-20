using System.Collections.Generic;
using UnityEngine;

public static class DefaultBoxDefinitions
{
    public static MonitorBoxDefinition CreateBox(int i)
    {
        return new MonitorBoxDefinition
        {
            Id = "gameplay" + i,
            Title = "Gameplay" + i,
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
    }
    public static List<MonitorBoxDefinition> Create()
    {
        var gameplayBox = new MonitorBoxDefinition
        {
            Id = "gameplay",
            Title = "Gameplay",
            Anchor = BoxAnchor.BottomRight,
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
            LayoutRule = new ScreenAnchorLayoutRule(BoxAnchor.BottomRight)
        };

        var gameplayBox1 = CreateBox(1);
        var gameplayBox2 = CreateBox(2);
        var gameplayBox3 = CreateBox(3);
        var gameplayBox4 = CreateBox(4);
        var gameplayBox5 = CreateBox(5);
        var gameplayBox6 = CreateBox(6);
        var gameplayBox7 = CreateBox(7);
        var gameplayBox8 = CreateBox(8);
        var gameplayBox9 = CreateBox(9);
        var gameplayBox10 = CreateBox(10);


        return new List<MonitorBoxDefinition>
        {
            gameplayBox,
            gameplayBox1,
            gameplayBox2,
            gameplayBox3,
            gameplayBox4,
            /*
            gameplayBox5,
            gameplayBox6,
            gameplayBox7,
            gameplayBox8,
            gameplayBox9,
            gameplayBox10
            */
        };
    }
}