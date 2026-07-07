using UnityEngine;

public static class MonitoringHelper
{
    /// <summary>
    /// Checks if a generic object is null or represents a destroyed Unity Object.
    /// </summary>
    public static bool IsDestroyed(object obj)
    {
        if (obj == null)
            return true;

        if (obj is Object unityObj)
            return unityObj == null; // Invokes Unity's overloaded null-check operator

        return false;
    }
}