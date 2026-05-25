using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RuntimeFlexLayout : MonoBehaviour
{
    [Header("Theme & Styles Configuration")]
    [SerializeField] private ThemeStyleSheet defaultTheme;
    // --- DRAG AND DROP YOUR BoxStyles.uss FILE HERE IN THE INSPECTOR ---
    [SerializeField] private StyleSheet boxStyleSheet; 

    [Header("Layout Settings")]
    [SerializeField] private int numberOfBoxes = 12;
    public enum LayoutAnchor { TopLeft, TopRight, BottomLeft, BottomRight }
    [SerializeField] private LayoutAnchor currentAnchor = LayoutAnchor.TopLeft;

    private VisualElement columnContainer;
    private List<Label> dynamicValueLabels = new List<Label>();
    private float updateTimer = 0f;
    private float updateInterval = 1.0f;

    private void OnEnable()
    {
        if (!TryGetComponent<UIDocument>(out UIDocument uiDocument))
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        if (uiDocument.panelSettings == null)
        {
            if (defaultTheme == null || boxStyleSheet == null)
            {
                Debug.LogError("RuntimeFlexLayout: Missing Theme or Style Sheet references in Inspector!", this);
                return;
            }

            PanelSettings runtimeSettings = ScriptableObject.CreateInstance<PanelSettings>();
            runtimeSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimeSettings.referenceResolution = new Vector2Int(1920, 1080);
            runtimeSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimeSettings.match = 0.5f;
            runtimeSettings.themeStyleSheet = defaultTheme;
            uiDocument.panelSettings = runtimeSettings;
        }

        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();
        dynamicValueLabels.Clear();

        // Inject our custom USS stylesheet rules into the root element so everything can read them
        root.styleSheets.Add(boxStyleSheet);

        // 1. Create Layout Container
        columnContainer = new VisualElement();
        columnContainer.style.height = Length.Percent(100);
        columnContainer.style.width = Length.Percent(100);
        columnContainer.style.backgroundColor = Color.clear; // Keep transparent so multiple objects overlay
        columnContainer.style.paddingTop = 15;
        columnContainer.style.paddingBottom = 15;
        columnContainer.style.paddingLeft = 15;
        columnContainer.style.paddingRight = 15;
        columnContainer.pickingMode = PickingMode.Ignore;
        
        ApplyLayoutAnchor(currentAnchor);

        // 2. Generate Boxes
        for (int i = 0; i < numberOfBoxes; i++)
        {
            VisualElement box = new VisualElement();
            box.AddToClassList("custom-box");
            box.pickingMode = PickingMode.Position;

            // Header row layout (Title + Toggle Button)
            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("box-header-row");

            Label titleLabel = new Label($"Box #{i + 1}");
            titleLabel.AddToClassList("box-header");
            headerRow.Add(titleLabel);

            Button toggleButton = new Button();
            toggleButton.text = "−"; 
            toggleButton.AddToClassList("collapse-btn");
            headerRow.Add(toggleButton);
            
            box.Add(headerRow);

            // --- CHANGED: Apply our base transition style class here ---
            VisualElement statsContainer = new VisualElement();
            statsContainer.AddToClassList("stats-content-holder");
            statsContainer.pickingMode = PickingMode.Ignore;

            int randomRowCount = Random.Range(1, 6); 
            for (int r = 0; r < randomRowCount; r++)
            {
                VisualElement rowContainer = new VisualElement();
                rowContainer.AddToClassList("stat-row");

                Label keyLabel = new Label($"Stat Name {r + 1}:");
                keyLabel.AddToClassList("stat-label");
                
                Label valueLabel = new Label("---");
                valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                rowContainer.Add(keyLabel);
                rowContainer.Add(valueLabel);
                dynamicValueLabels.Add(valueLabel);
                
                statsContainer.Add(rowContainer);
            }

            box.Add(statsContainer);

            // --- CHANGED: New clean transition toggle logic ---
            toggleButton.clicked += () => 
            {
                // This cleanly adds the collapsed style if missing, or removes it if present
                statsContainer.ToggleInClassList("stats-content-holder--collapsed");
                
                // Update the button indicator symbol based on whether the class is active
                if (statsContainer.ClassListContains("stats-content-holder--collapsed"))
                {
                    toggleButton.text = "+";
                }
                else
                {
                    toggleButton.text = "−";
                }
            };

            columnContainer.Add(box);
        }

        root.Add(columnContainer);
        RandomizeFields();
    }

    private void ApplyLayoutAnchor(LayoutAnchor anchor)
    {
        if (columnContainer == null) return;
        switch (anchor)
        {
            case LayoutAnchor.TopLeft:
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.flexWrap = Wrap.Wrap;
                break;
            case LayoutAnchor.TopRight:
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.flexWrap = Wrap.WrapReverse;
                break;
            case LayoutAnchor.BottomLeft:
                columnContainer.style.flexDirection = FlexDirection.ColumnReverse;
                columnContainer.style.flexWrap = Wrap.Wrap;
                break;
            case LayoutAnchor.BottomRight:
                columnContainer.style.flexDirection = FlexDirection.ColumnReverse;
                columnContainer.style.flexWrap = Wrap.WrapReverse;
                break;
        }
    }

    private void Update()
    {
        ApplyLayoutAnchor(currentAnchor);
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RandomizeFields();
        }
    }

    private void RandomizeFields()
    {
        foreach (Label label in dynamicValueLabels)
        {
            float randomStat = Random.Range(0.0f, 100.0f);
            label.text = $"{randomStat:F1}%";
            
            if (randomStat < 30.0f) label.style.color = Color.red;
            else if (randomStat > 80.0f) label.style.color = Color.green;
            else label.style.color = Color.white;
        }
    }
}