using System.Collections.Generic;
using System.Diagnostics;

namespace ScopeRuntimeMonitoring
{
    public delegate UnityEngine.UIElements.VisualElement CreateWidgetDelegate(IMonitorHandle handle, out System.Action<object> updateCallback);

    public static class Monitor
    {
        public static MonitoringRegistry Registry { get; private set; }
        public static event System.Action<object> TargetRegistered;
        public static event System.Action<object> TargetUnregistered;

        public static System.Func<object, string> GroupCustomizer;
        public static System.Func<object, string> SubGroupCustomizer;

        private static readonly Dictionary<string, CreateWidgetDelegate> _customWidgetCreators = new Dictionary<string, CreateWidgetDelegate>();

        public static void RegisterCustomWidget(string variant, CreateWidgetDelegate creator)
        {
            if (string.IsNullOrEmpty(variant)) return;
            _customWidgetCreators[variant] = creator;
        }

        public static bool TryGetCustomWidgetCreator(string variant, out CreateWidgetDelegate creator)
        {
            if (string.IsNullOrEmpty(variant))
            {
                creator = null;
                return false;
            }
            return _customWidgetCreators.TryGetValue(variant, out creator);
        }

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