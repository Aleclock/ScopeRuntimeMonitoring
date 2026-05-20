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
        Color textColor,
        TMP_FontAsset fontAsset
    )
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

        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObject.transform.SetParent(rowObject.transform, false);

        _label = labelObject.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            _label.font = fontAsset;
        }
        _label.text = initialText;
        _label.fontSize = fontSize;
        var opaqueTextColor = textColor;
        opaqueTextColor.a = 1f;
        _label.color = opaqueTextColor;
        _label.alignment = TextAlignmentOptions.Left;
        _label.enableWordWrapping = false;
        _label.overflowMode = TextOverflowModes.Ellipsis;
        _label.margin = new Vector4(8f, 0f, 8f, 0f);
        _label.raycastTarget = false;

        var labelRect = _label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
    }

    public void Refresh()
    {
        if (_handle == null)
            return;
        
        _label.text = $"{_handle.Name}: {_handle.GetValueString()}";
    }
}