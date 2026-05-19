using System.Collections.Generic;
using System.Diagnostics;

public static class Monitor
{
    public static MonitoringRegistry Registry { get; private set; }

    static Monitor()
    {
        Registry = new MonitoringRegistry();
    }

    public static void StartMonitoring(object target)
    {
        Registry.RegisterTarget(target);
    }

    public static void StopMonitoring(object target)
    {
        Registry.UnregisterTarget(target);
    }
}