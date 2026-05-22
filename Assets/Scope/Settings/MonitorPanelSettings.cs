using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Scope Runtime Monitoring/Monitor Panel Settings", fileName = "MonitorPanelSettings")]
public sealed class MonitorPanelSettings : ScriptableObject
{
    [Header("UI Toolkit Assets")]
    public PanelSettings panelSettings;
    public StyleSheet boxStyleSheet;

    [Header("Runtime Defaults")]
    public bool enableRuntimeMonitoring = true; // toggle runtime monitoring for the project
    public MonitorPanelAnchor defaultAnchor = MonitorPanelAnchor.TopLeft;
    [Min(0.05f)] public float defaultUpdateInterval = 0.5f;
    [Min(0f)] public float defaultPadding = 15f;
    // TODO Add default margin?

    [Header("UIToolkit Layout Defaults")]
    [Min(0f)] public float defaultPanelWidth = 430f;
    [Min(0f)] public float defaultPanelHeight = 280f;
    [Min(0f)] public float defaultMargin = 12f;
    [Min(0f)] public float defaultFontSize = 17f;
    [Min(0f)] public float defaultTitleHeight = 30f;
    [Min(0f)] public float defaultRowHeight = 26f;
    [Min(0f)] public float defaultRowSpacing = 4f;

    [Header("Optional Safety")]
    public bool logMissingReferences = true;
}

public enum MonitorPanelAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}