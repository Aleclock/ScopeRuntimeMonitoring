using UnityEngine;

// Non-Mono static UI service that instantiates the runtime UI on first registered target
public static class MonitorUIService
{
    private static bool s_initialized = false;
    private static bool s_uiCreated = false;

    // Static constructor subscribes to the Monitor event when this class is first referenced.
    static MonitorUIService()
    {
        EnsureSubscribed();
    }

    // Public method to ensure the type is initialized from other code (e.g. Monitor static constructor)
    public static void EnsureSubscribed()
    {
        if (s_initialized) return;

        Monitor.TargetRegistered += OnTargetRegistered;
        s_initialized = true;
    }

    private static void OnTargetRegistered(object target)
    {
        if (s_uiCreated) return;

        // Try to instantiate a prefab named "RuntimeMonitorUILayout" from Resources first

        var prefab = Resources.Load<GameObject>("RuntimeMonitorUILayout");
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(go);
            s_uiCreated = true;
            return;
        }

        // Fallback: create a GameObject and attach MonitorPanelView
        var runtimeGo = new GameObject("RuntimeMonitorUI");
        Object.DontDestroyOnLoad(runtimeGo);
        runtimeGo.AddComponent<MonitorPanelView>();
        s_uiCreated = true;
    }
}