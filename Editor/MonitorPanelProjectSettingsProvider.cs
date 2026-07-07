#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ScopeRuntimeMonitoring
{
    static class MonitorPanelProjectSettingsProvider
    {
        private const string ResourcePath = "Defaults/MonitorPanelSettings";

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
            MonitorPanelSettings settings = LoadSettings();
            
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Could not load the package default MonitorPanelSettings asset.", MessageType.Error);
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

        private static MonitorPanelSettings LoadSettings()
        {
            return Resources.Load<MonitorPanelSettings>(ResourcePath);
        }
    }
}
#endif