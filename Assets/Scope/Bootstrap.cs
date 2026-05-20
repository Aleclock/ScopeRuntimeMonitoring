
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        var example = FindObjectOfType<MonitoredExample>();
        if (example != null)
        {
            Monitor.StartMonitoring(example);
            DebugManager.Instance.CreateDebugPanel<MonitorsPanel>().Initialize();
        }
    }
}