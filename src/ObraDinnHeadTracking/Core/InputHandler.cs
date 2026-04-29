using System;
using HeadTracking.Config;
using UnityEngine;

namespace HeadTracking.Core
{
    /// <summary>
    /// Handles configurable keyboard input for toggle and recenter actions.
    /// Each action has a configurable nav-cluster key plus a fixed Ctrl+Shift+letter
    /// chord, drawn from the T/Y/U/G/H/J cluster so keyboards without a nav block
    /// still work.
    /// </summary>
    public class InputHandler
    {
        private readonly ConfigManager _config;

        /// <summary>
        /// Fired when toggle key is pressed.
        /// </summary>
        public event Action OnTogglePressed;

        /// <summary>
        /// Fired when recenter key is pressed.
        /// </summary>
        public event Action OnRecenterPressed;

        /// <summary>
        /// Fired when toggle reticle key is pressed.
        /// </summary>
        public event Action OnToggleReticlePressed;

        /// <summary>
        /// Fired when cycle tracking mode key is pressed.
        /// Cycles: normal -> rotation only -> position only -> normal.
        /// </summary>
        public event Action OnCycleTrackingModePressed;

        /// <summary>
        /// The currently configured toggle key.
        /// </summary>
        public KeyCode ToggleKey => _config.ToggleKey.Value;

        /// <summary>
        /// The currently configured recenter key.
        /// </summary>
        public KeyCode RecenterKey => _config.RecenterKey.Value;

        /// <summary>
        /// The currently configured toggle reticle key.
        /// </summary>
        public KeyCode ToggleReticleKey => _config.ToggleReticleKey.Value;

        /// <summary>
        /// The currently configured cycle tracking mode key.
        /// </summary>
        public KeyCode CycleTrackingModeKey => _config.CycleTrackingModeKey.Value;

        public InputHandler(ConfigManager config)
        {
            _config = config;
        }

        /// <summary>
        /// Check for input. Call from Update.
        /// </summary>
        public void CheckInput()
        {
            bool ctrlShift =
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            if (Input.GetKeyDown(_config.RecenterKey.Value) ||
                (ctrlShift && Input.GetKeyDown(KeyCode.T)))
            {
                OnRecenterPressed?.Invoke();
            }

            if (Input.GetKeyDown(_config.ToggleKey.Value) ||
                (ctrlShift && Input.GetKeyDown(KeyCode.Y)))
            {
                OnTogglePressed?.Invoke();
            }

            if (Input.GetKeyDown(_config.CycleTrackingModeKey.Value) ||
                (ctrlShift && Input.GetKeyDown(KeyCode.G)))
            {
                OnCycleTrackingModePressed?.Invoke();
            }

            if (Input.GetKeyDown(_config.ToggleReticleKey.Value) ||
                (ctrlShift && Input.GetKeyDown(KeyCode.H)))
            {
                OnToggleReticlePressed?.Invoke();
            }
        }
    }
}
