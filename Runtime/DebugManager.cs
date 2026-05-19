using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
    private static DebugManager instance;
    private Canvas debugCanvas;
    private List<DebugPanelBase> activePanels = new List<DebugPanelBase>();
    private int initializedPanelCount = 0;

    public static DebugManager Instance
    {
        get
        {
            if (instance == null)
            {
                var managerObject = new GameObject("DebugManager");
                instance = managerObject.AddComponent<DebugManager>();
                DontDestroyOnLoad(managerObject);
            }
            return instance;
        }
    }

    private void EnsureCanvasExists()
    {
        if (debugCanvas == null)
        {
            debugCanvas = new GameObject("DebugCanvas").AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = debugCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080); // Target resolution
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasScaler.matchWidthOrHeight = 0.5f;

            //debugCanvas.enabled = false;
        }
    }

    // Method to create and add debug panels to the single canvas
    public T CreateDebugPanel<T>() where T : DebugPanelBase
    {
        EnsureCanvasExists();

        // Create the panel object and set it as a child of the canvas
        var panelGameObject = new GameObject(typeof(T).Name, typeof(RectTransform));
        var panel = panelGameObject.AddComponent<T>();
        panel.transform.SetParent(debugCanvas.transform, false);

        // Configure the RectTransform for viewport adaptation
        ConfigurePanelForViewport(panelGameObject.GetComponent<RectTransform>());
        activePanels.Add(panel);
        return panel;
    }

    public void RemoveDebugPanel(DebugPanelBase panel)
    {
        if (activePanels.Contains(panel))
        {
            activePanels.Remove(panel);
            Destroy(panel.gameObject);
        }
    }

    // Method to adapt the panel to fit within the viewport
    private void ConfigurePanelForViewport(RectTransform rectTransform)
    {
        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate width and height ratios to scale with screen size
        float widthRatio = rectTransform.rect.width / screenWidth;
        float heightRatio = rectTransform.rect.height / screenHeight;

        // Calculate the screen safe area
        Rect safeArea = Screen.safeArea;

        // Calculate offsets based on safe area and scaling ratios
        float offsetTop = (safeArea.yMax - screenHeight) * heightRatio;
        float offsetLeft = safeArea.xMin * widthRatio;

        // Set offsets using RectTransform’s offsetMax and offsetMin
        rectTransform.anchorMin = new Vector2(0, 1); // Top-left anchor
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1); // Set pivot to top-left
        rectTransform.offsetMax = new Vector2(-offsetLeft, offsetTop); // Right and Top offset
        rectTransform.offsetMin = new Vector2(offsetLeft, 0); // Left and Bottom offset
    }

    public List<DebugPanelBase> GetActivePanels()
    {
        return new List<DebugPanelBase>(activePanels); // Return a copy to avoid direct modification
    }

    // This is called by each panel when initialized
    public void OnPanelInitialized()
    {
        initializedPanelCount++;

        // Check if all panels have initialized
        if (initializedPanelCount == activePanels.Count)
        {
            CheckAndRepositionPanels();
        }
    }

    // Check for overlapping panels and reposition if necessary
   
   private void CheckAndRepositionPanels()
   {
        // Sort panels by importance in descending order (higher importance first)
        activePanels.Sort((a, b) => b.importance.CompareTo(a.importance));

        for (int i = 0; i < activePanels.Count; i++)
        {
            var panelA = activePanels[i];
            
            // Create a Rect based on panelA's panelPosition and panelSize
            var rectA = new Rect(panelA.panelPosition.x, panelA.panelPosition.y - panelA.panelSize.y, panelA.panelSize.x, panelA.panelSize.y);

            for (int j = i + 1; j < activePanels.Count; j++)
            {
                var panelB = activePanels[j];
                
                // Create a Rect based on panelB's panelPosition and panelSize
                var rectB = new Rect(panelB.panelPosition.x, panelB.panelPosition.y - panelB.panelSize.y, panelB.panelSize.x, panelB.panelSize.y);

                // Check if the panels overlap
                if (RectOverlaps(rectA, rectB))
                {
                    // Calculate an offset to move panelB downward until there's no overlap
                    float newYOffset = -panelA.panelSize.y - 10; // Move downward by panel height + padding
                    panelB.panelPosition += new Vector2(0, newYOffset);

                    // Update rectB to the new position to check further overlaps if needed
                    rectB = new Rect(panelB.panelPosition.x, panelB.panelPosition.y - panelB.panelSize.y, panelB.panelSize.x, panelB.panelSize.y);
                }
            }
        }

        // Apply the adjusted positions to all panels after repositioning
        foreach (var panel in activePanels)
        {
            panel.ApplyPanelPosition();
        }
    }


    private bool RectOverlaps(Rect rectA, Rect rectB)
    {
        return rectA.Overlaps(rectB);
    }
}
