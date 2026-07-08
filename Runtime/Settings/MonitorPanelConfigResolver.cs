using UnityEngine;
using UnityEngine.UIElements;

namespace ScopeRuntimeMonitoring
{
    public readonly struct MonitorPanelConfig
    {
        public readonly PanelSettings PanelSettings;
        public readonly StyleSheet BoxStyleSheet;
        public readonly MonitorPanelAnchor Anchor;
        public readonly float UpdateInterval;
        public readonly float Padding;

        public MonitorPanelConfig(
            PanelSettings panelSettings,
            StyleSheet boxStyleSheet,
            MonitorPanelAnchor anchor,
            float updateInterval,
            float padding
        )
        {
            PanelSettings = panelSettings;
            BoxStyleSheet = boxStyleSheet;
            Anchor = anchor;
            UpdateInterval = updateInterval;
            Padding = padding;
        }
    }

    public static class MonitorPanelConfigResolver
    {
        public static MonitorPanelConfig Resolve(MonitorPanelSettings defaults, MonitorPanelOverrides overrides)
        {
            if (defaults == null)
            {
                Debug.LogError("MonitorPanelConfigResolver: missing MonitorPanelSettings asset.");
                return default;
            }

            PanelSettings panelSettings = defaults.panelSettings;
            StyleSheet boxStyleSheet = defaults.boxStyleSheet;
            MonitorPanelAnchor anchor = defaults.defaultAnchor;
            float updateInterval = defaults.defaultUpdateInterval;
            float padding = defaults.defaultPadding;

            if (overrides != null)
            {
                if (!overrides.EnableRuntimePanel)
                    return default;

                if (overrides.OverridePanelSettings && overrides.PanelSettings != null)
                    panelSettings = overrides.PanelSettings;

                if (overrides.OverrideBoxStyleSheet && overrides.BoxStyleSheet != null)
                    boxStyleSheet = overrides.BoxStyleSheet;

                if (overrides.OverrideAnchor)
                    anchor = overrides.Anchor;

                if (overrides.OverrideUpdateInterval)
                    updateInterval = Mathf.Max(0f, overrides.UpdateInterval);

                if (overrides.OverridePadding)
                    padding = Mathf.Max(0f, overrides.Padding);
            }

            if (panelSettings == null)
                Debug.LogError("MonitorPanelConfigResolver: panelSettings is null after resolution.");

            if (boxStyleSheet == null)
                Debug.LogError("MonitorPanelConfigResolver: boxStyleSheet is null after resolution.");

            return new MonitorPanelConfig(panelSettings, boxStyleSheet, anchor, updateInterval, padding);
        }
    }
}