using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScopeRuntimeMonitoring
{
    /// <summary>
    /// Standalone helper component to toggle all Scope Runtime Monitoring UI panels on and off.
    /// Can be triggered via a configurable hotkey or invoked from custom scripts / UI buttons.
    /// </summary>
    [AddComponentMenu("Scope Runtime Monitoring/Runtime Monitor Toggle")]
    [DisallowMultipleComponent]
    public sealed class RuntimeMonitorToggle : MonoBehaviour
    {
        [Header("Hotkey Configuration")]
        [Tooltip("Enable hotkey listening in Update().")]
        [SerializeField] private bool enableHotkey = true;

        [Tooltip("The keyboard key used to toggle monitoring UI visibility.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("Initial State")]
        [Tooltip("Whether the monitoring UI should be visible when starting.")]
        [SerializeField] private bool startVisible = true;

        [Tooltip("Persist this toggle controller across scene loads.")]
        [SerializeField] private bool persistAcrossScenes = false;

        /// <summary>
        /// Event fired whenever the monitoring UI visibility changes.
        /// Passes true when visible, false when hidden.
        /// </summary>
        public event Action<bool> VisibilityChanged;

        private bool _isVisible = true;

        /// <summary>
        /// Current visibility state of the monitoring UI.
        /// </summary>
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            _isVisible = startVisible;
        }

        private void OnEnable()
        {
            Monitor.TargetRegistered += OnTargetRegistered;

            // Apply the initial state if panels already exist
            if (!_isVisible)
            {
                SetVisible(false);
            }
        }

        private void OnDisable()
        {
            Monitor.TargetRegistered -= OnTargetRegistered;
        }

        private void Update()
        {
            if (enableHotkey && Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }
        }

        private void OnTargetRegistered(object target)
        {
            // If visibility is currently set to false, make sure any newly created UI stays hidden
            if (!_isVisible)
            {
                ApplyVisibilityToAllPanels(false);
            }
        }

        /// <summary>
        /// Toggles the visibility of all monitoring panels.
        /// </summary>
        public void Toggle()
        {
            SetVisible(!_isVisible);
        }

        /// <summary>
        /// Explicitly sets the visibility of all monitoring panels.
        /// </summary>
        /// <param name="visible">True to show panels, false to hide.</param>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            ApplyVisibilityToAllPanels(visible);
            VisibilityChanged?.Invoke(_isVisible);
        }

        private void ApplyVisibilityToAllPanels(bool visible)
        {
            // Find all MonitorPanelView instances (including inactive ones)
            var panels = FindObjectsOfType<MonitorPanelView>(true);

            foreach (var panel in panels)
            {
                if (panel != null && panel.gameObject != null)
                {
                    if (panel.gameObject.activeSelf != visible)
                    {
                        panel.gameObject.SetActive(visible);
                    }
                }
            }

            // Fallback: If UIDocument exists with "RuntimeMonitorUI" name
            var fallbackGo = GameObject.Find("RuntimeMonitorUI");
            if (fallbackGo != null && fallbackGo.activeSelf != visible)
            {
                fallbackGo.SetActive(visible);
            }
        }
    }
}
