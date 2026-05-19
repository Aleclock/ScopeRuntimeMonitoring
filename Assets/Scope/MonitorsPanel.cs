using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonitorsPanel : DebugPanelBase
{
    private readonly List<IMonitorHandle> handles = new List<IMonitorHandle>();
    private readonly List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    public override void Initialize()
    {
        CreatePanelBackground();

        handles.AddRange(Monitor.Registry.GetMonitorHandles());

        float y = -10f;
        foreach (var handle in handles)
        {
            var text = CreateTextField(
                handle.Name,
                new Vector2(10f, y),
                18,
                $"{handle.Name}: {handle.GetValueString()}",
                400f,
                24f);

            texts.Add(text);
            y -= 24f;
        }
    }

    private TextMeshProUGUI CreateTextField(
        string name,
        Vector2 anchoredPosition,
        int fontSize,
        string initialText,
        float width,
        float height)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(panelBackground.transform, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;

        var rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(width, height);

        return text;
    }

    private void Update()
    {
        for (int i = 0; i < handles.Count; i++)
        {
            texts[i].text = $"{handles[i].Name}: {handles[i].GetValueString()}";
        }
    }

    public override void InizializePanelData()
    {
    }

    public override void UpdatePanelData()
    {
    }
}