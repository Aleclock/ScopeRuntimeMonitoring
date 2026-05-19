using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
    private static DebugManager instance;

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

    private Canvas debugCanvas;

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

        return panel;
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
}
