#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

static class MonitorPanelProjectSettingsProvider
{
    private const string FolderPath = "Assets/ScopeRuntimeMonitoringSettings";
    private const string AssetPath = FolderPath + "/MonitorPanelSettings.asset";

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new SettingsProvider("Project/Scope Runtime Monitoring", SettingsScope.Project)
        {
            guiHandler = DrawSettings,
            keywords = new[] { "scope", "runtime", "monitor", "panel", "theme", "stylesheet", "ui toolkit" }
        };
    }

    private static void DrawSettings(string searchContext)
    {
        MonitorPanelSettings settings = GetOrCreateSettings();
        
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Could not create or load MonitorPanelSettings.asset.", MessageType.Error);
            return;
        }

        Editor editor = Editor.CreateEditor(settings);

        try
        {
            editor.OnInspectorGUI();
        }
        finally
        {
            Object.DestroyImmediate(editor);
        }
    }

    private static MonitorPanelSettings GetOrCreateSettings()
    {
        MonitorPanelSettings settings = AssetDatabase.LoadAssetAtPath<MonitorPanelSettings>(AssetPath);

        if (settings != null)
            return settings;
        
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets", "ScopeRuntimeMonitoringSettings");
        
        settings = ScriptableObject.CreateInstance<MonitorPanelSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
    }
}
#endif