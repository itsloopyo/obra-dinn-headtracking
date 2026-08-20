using System;
using CameraUnlock.Core.Unity.Extensions;
using HeadTracking.Config;
using UnityEngine;

namespace HeadTracking.Core
{
    /// <summary>
    /// Handles configurable keyboard input for the mod's hotkey actions.
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
            if (ChordHotkeys.IsActionPressed(_config.ToggleKey.Value, ChordHotkeys.ToggleLetter))
            {
                OnTogglePressed?.Invoke();
            }

            if (ChordHotkeys.IsActionPressed(_config.CycleTrackingModeKey.Value, ChordHotkeys.PositionLetter))
            {
                OnCycleTrackingModePressed?.Invoke();
            }

            if (ChordHotkeys.IsActionPressed(_config.ToggleReticleKey.Value, ChordHotkeys.FourthToggleLetter))
            {
                OnToggleReticlePressed?.Invoke();
            }
        }
    }
}
