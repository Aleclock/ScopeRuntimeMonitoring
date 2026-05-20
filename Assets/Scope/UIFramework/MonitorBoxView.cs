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
        _panelRect.pivot = GetPivotForAnchor(definition.Anchor);
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
        _contentRect.offsetMin = Vector2.zero;
        _contentRect.offsetMax = Vector2.zero;
        _contentRect.sizeDelta = Vector2.zero;

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
            row.Refresh();
    }

    private static Vector2 GetPivotForAnchor(BoxAnchor anchor)
    {
        var pivotX = anchor == BoxAnchor.TopRight || anchor == BoxAnchor.BottomRight ? 1f : 0f;
        var pivotY = anchor == BoxAnchor.BottomLeft || anchor == BoxAnchor.BottomRight ? 0f : 1f;

        return new Vector2(pivotX, pivotY);
    }
}