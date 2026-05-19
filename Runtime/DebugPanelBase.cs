using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public abstract class DebugPanelBase : MonoBehaviour
{
    public int importance; // Higher value = higher importance
    // Configurable size and position fields
    public Vector2 panelSize = new Vector2(200, 100);
    public Vector2 panelPosition = new Vector2(10, -10); // Offset from top-left

    // Panel background image component reference
    protected Image panelBackground;

    protected void CreatePanelBackground()
    {
        panelBackground = new GameObject("PanelBackground").AddComponent<Image>();
        panelBackground.transform.SetParent(transform);
        panelBackground.transform.position = Vector3.zero;
        panelBackground.color = new Color(0, 0, 0, 0.5f); // Semi-transparent background
    }
    
    protected void PositionPanelBackground()
    {
        var backgroundRectTransform = panelBackground.GetComponent<RectTransform>();
        backgroundRectTransform.anchorMin = new Vector2(0, 1); // Anchor to top-left
        backgroundRectTransform.anchorMax = new Vector2(0, 1);
        backgroundRectTransform.pivot = new Vector2(0, 1); // Top-left pivot
        backgroundRectTransform.sizeDelta = panelSize;
        backgroundRectTransform.anchoredPosition = panelPosition;
    }

    // Adjusts panel height based on row height and spacing
    protected void AdjustPanelSize(Vector2 newSize)
    {
        panelSize = new Vector2(panelSize.x, newSize.y);
        PositionPanelBackground();
    }

    public void ApplyPanelPosition()
    {
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = panelPosition;
    }

    protected float CalculateMaxLabelWidth(List<string> variableNames)
    {
        float maxWidth = 0;
        int fontSize = 24; // Match the font size in CreateTextField

        foreach (var name in variableNames)
        {
            // Create a temporary TextMeshPro object to measure width
            var tempText = new GameObject().AddComponent<TextMeshProUGUI>();
            tempText.fontSize = fontSize;
            tempText.text = name;
            
            // Measure the preferred width of the text
            float preferredWidth = tempText.GetPreferredValues(name).x;
            maxWidth = Mathf.Max(maxWidth, preferredWidth);

            // Destroy the temporary text object
            Destroy(tempText.gameObject);
        }

        // Add padding to ensure space between the variable_name and value fields
        return maxWidth + 10;
    }

    protected void NotifyPanelInitialized()
    {
        DebugManager.Instance.OnPanelInitialized();
    }

    // Abstract methods to be implemented in derived classes for custom debug info
    public abstract void Initialize();
    public abstract void InizializePanelData();
    public abstract void UpdatePanelData();
}

public enum AlignType
{
    NoAlign,
    AlignGroup,
    AlignTotal
}