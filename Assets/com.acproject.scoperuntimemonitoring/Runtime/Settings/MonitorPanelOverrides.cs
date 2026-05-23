using UnityEngine;
using UnityEngine.UIElements;

public sealed class MonitorPanelOverrides : MonoBehaviour
{
    [Header("Override Switches")]
    [SerializeField] private bool enableRuntimePanel = true; // per-panel on/off
    [SerializeField] private bool overridePanelSettings;
    [SerializeField] private bool overrideBoxStyleSheet;
    [SerializeField] private bool overrideAnchor;
    [SerializeField] private bool overrideNumberOfBoxes;
    [SerializeField] private bool overrideUpdateInterval;
    [SerializeField] private bool overridePadding;

    [Header("Per-Panel Values")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private StyleSheet boxStyleSheet;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;
    [SerializeField, Min(0.05f)] private float updateInterval = 1f;
    [SerializeField, Min(0f)] private float padding = 15f;

    [SerializeField, Min(0f)] private float panelWidth = 430f;
    [SerializeField, Min(0f)] private float panelHeight = 280f;
    [SerializeField, Min(0f)] private float margin = 12f;
    [SerializeField, Min(0f)] private float fontSize = 17f;
    [SerializeField, Min(0f)] private float titleHeight = 30f;
    [SerializeField, Min(0f)] private float rowHeight = 26f;
    [SerializeField, Min(0f)] private float rowSpacing = 4f;

    public bool OverridePanelSettings => overridePanelSettings;
    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public bool OverrideAnchor => overrideAnchor;
    public bool OverrideNumberOfBoxes => overrideNumberOfBoxes;
    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public bool OverridePadding => overridePadding;

    public PanelSettings PanelSettings => panelSettings;
    public StyleSheet BoxStyleSheet => boxStyleSheet;
    public MonitorPanelAnchor Anchor => anchor;
    public float UpdateInterval => updateInterval;
    public float Padding => padding;
    public float PanelWidth => panelWidth;
    public float PanelHeight => panelHeight;
    public float Margin => margin;
    public float FontSize => fontSize;
    public float TitleHeight => titleHeight;
    public float RowHeight => rowHeight;
    public float RowSpacing => rowSpacing;
}