using System.Collections.Generic;
using System.Diagnostics;

namespace ScopeRuntimeMonitoring
{
    public static class Monitor
    {
        public static MonitoringRegistry Registry { get; private set; }
        public static event System.Action<object> TargetRegistered;
        public static event System.Action<object> TargetUnregistered;

        static Monitor()
        {
            Registry = new MonitoringRegistry();
            MonitorUIService.EnsureSubscribed();
        }

        public static void StartMonitoring(object target)
        {
            bool added = Registry.RegisterTarget(target);
            if (added)
                TargetRegistered?.Invoke(target);
        }

        public static void StopMonitoring(object target)
        {
            Registry.UnregisterTarget(target);
            TargetUnregistered?.Invoke(target);
        }
    }
}