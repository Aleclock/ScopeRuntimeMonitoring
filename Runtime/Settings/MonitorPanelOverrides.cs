using UnityEngine;
using UnityEngine.UIElements;

namespace ScopeRuntimeMonitoring
{
    public sealed class MonitorPanelOverrides : MonoBehaviour
    {
        [Header("Override Switches")]
        [SerializeField] private bool enableRuntimePanel = true;
        [SerializeField] private bool overridePanelSettings;
        [SerializeField] private bool overrideBoxStyleSheet;
        [SerializeField] private bool overrideAnchor;
        [SerializeField] private bool overrideUpdateInterval;
        [SerializeField] private bool overridePadding;

        [Header("Per-Panel Values")]
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private StyleSheet boxStyleSheet;
        [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;
        [SerializeField, Min(0.05f)] private float updateInterval = 1f;
        [SerializeField, Min(0f)] private float padding = 15f;

        public bool EnableRuntimePanel => enableRuntimePanel;

        public bool OverridePanelSettings => overridePanelSettings;
        public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
        public bool OverrideAnchor => overrideAnchor;
        public bool OverrideUpdateInterval => overrideUpdateInterval;
        public bool OverridePadding => overridePadding;

        public PanelSettings PanelSettings => panelSettings;
        public StyleSheet BoxStyleSheet => boxStyleSheet;
        public MonitorPanelAnchor Anchor => anchor;
        public float UpdateInterval => updateInterval;
        public float Padding => padding;
    }
}