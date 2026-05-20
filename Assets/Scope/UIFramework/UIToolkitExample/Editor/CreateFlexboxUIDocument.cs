#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class CreateFlexboxUIDocument
{
    [MenuItem("Tools/UIToolkit/Create Flexbox Example")]
    public static void Create()
    {
        var uxmlPath = "Assets/Scope/UIFramework/UIToolkitExample/FlexContainer.uxml";

        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

        if (vta == null)
        {
            Debug.LogError($"VisualTreeAsset not found at {uxmlPath}");
            return;
        }

        var panelSettingsPath = "Assets/Scope/UIFramework/UIToolkitExample/DefaultPanelSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, panelSettingsPath);
            AssetDatabase.SaveAssets();
        }

        var go = new GameObject("UIToolkit_FlexboxExample");
        var doc = go.AddComponent<UIDocument>();
        doc.visualTreeAsset = vta;
        doc.panelSettings = settings;

        var bootstrap = go.AddComponent<FlexboxExampleBootstrap>();

        // Save scene selection
        Selection.activeGameObject = go;
        Debug.Log("Created UIToolkit_FlexboxExample GameObject with UIDocument. Press Play to preview.");
    }
}
#endif