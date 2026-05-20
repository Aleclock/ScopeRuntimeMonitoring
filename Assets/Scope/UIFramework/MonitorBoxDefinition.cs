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