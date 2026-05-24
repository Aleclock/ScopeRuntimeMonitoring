using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MonitorBoxOverrides : MonoBehaviour
{
    [Header("Box Overrides")]
    [SerializeField] private bool enableRuntimeBox = true;

    [Header("Routing")]
    [SerializeField] private bool overrideAnchor = false;
    [SerializeField] private MonitorPanelAnchor anchor = MonitorPanelAnchor.TopLeft;

    [Header("Optional Overrides")]
    [SerializeField] private bool overrideUpdateInterval = false;
    [SerializeField, Min(0.01f)] private float updateInterval = 1f;

    [SerializeField] private bool overrideBoxStyleSheet = false;
    [SerializeField] private StyleSheet boxStyleSheet;

    [Header("Layout Overrides")]
    [SerializeField] private bool overridePanelWidth = false;
    [SerializeField, Min(0f)] private float panelWidth = 430f;

    [SerializeField] private bool overridePanelHeight = false;
    [SerializeField, Min(0f)] private float panelHeight = 280f;

    [SerializeField] private bool overrideFontSize = false;
    [SerializeField, Min(0f)] private float fontSize = 17f;

    [SerializeField] private bool overrideRowHeight = false;
    [SerializeField, Min(0f)] private float rowHeight = 26f;

    [SerializeField] private bool overrideRowSpacing = false;
    [SerializeField, Min(0f)] private float rowSpacing = 4f;

    [Header("Identification")]
    [SerializeField] private string boxId = "";

    public bool EnableRuntimeBox => enableRuntimeBox;

    public bool OverrideAnchor => overrideAnchor;
    public MonitorPanelAnchor Anchor => anchor;

    public bool OverrideUpdateInterval => overrideUpdateInterval;
    public float UpdateInterval => updateInterval;

    public bool OverrideBoxStyleSheet => overrideBoxStyleSheet;
    public StyleSheet BoxStyleSheet => boxStyleSheet;

    public bool OverridePanelWidth => overridePanelWidth;
    public float PanelWidth => panelWidth;

    public bool OverridePanelHeight => overridePanelHeight;
    public float PanelHeight => panelHeight;

    public bool OverrideFontSize => overrideFontSize;
    public float FontSize => fontSize;

    public bool OverrideRowHeight => overrideRowHeight;
    public float RowHeight => rowHeight;

    public bool OverrideRowSpacing => overrideRowSpacing;
    public float RowSpacing => rowSpacing;

    public string BoxId => boxId;
}