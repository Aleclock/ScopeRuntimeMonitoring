using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

public class FPSDebugPanel : DebugPanelBase
{
    private TextMeshProUGUI panelText;
    private float deltaTime;

    // Utils
    private int rowHeight = 25;
    private int rowWidth = 1000;
    private int fontSize = 24;
    private int offsetVertical = 10;
    private int offsetHorizontal = 10;
    private float lastCreatedPositionY = -10;
    private float maxLabelWidth = -1;

    public override void Initialize()
    {
        panelSize = new Vector2(500, 300);       // Specific size for this panel
        panelPosition = new Vector2(10, -10);    // Specific position for this panel
        CreatePanelBackground();
        panelText = CreateTextField("Field FPS", new Vector2(0,0), fontSize, "FPS: Nan", rowWidth, rowHeight);
        AdjustPanelSize();
        PositionPanelBackground();
    }

    private TextMeshProUGUI CreateTextField(string name, Vector2 anchoredPosition, int fontSize, string initialText, float width, float height)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(panelBackground.transform, false); // Attach to the background

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;   // Set the initial text
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;

        // Set position and padding inside the panel
        var rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1); // Top-left anchor
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1); // Top-left pivot
        rectTransform.anchoredPosition = anchoredPosition; // Position text inside the background

        // Set width and height
        rectTransform.sizeDelta = new Vector2(width, height); // Set size of the text field
        lastCreatedPositionY -= rowHeight;
        return text;
    }

    private void Update()
    {
        // Calculate frames per second
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        UpdatePanelData();
    }

    public override void InizializePanelData()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdatePanelData()
    {
        // Display FPS information
        float fps = 1.0f / deltaTime;
        panelText.text = $"FPS: {fps:0.0}";
    }

    private void AdjustPanelSize()
    {
        float requiredHeight = Mathf.Abs(lastCreatedPositionY) + offsetVertical;
        AdjustPanelSize(new Vector2(panelSize.x, requiredHeight));
    }
}