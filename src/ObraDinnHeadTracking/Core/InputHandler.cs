using System;
using HeadTracking.Config;
using UnityEngine;

namespace HeadTracking.Core
{
    /// <summary>
    /// Handles configurable keyboard input for toggle and recenter actions.
    /// Default keys: End (toggle), Home (recenter).
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

        public InputHandler(ConfigManager config)
        {
            _config = config;
        }

        /// <summary>
        /// Check for input. Call from Update.
        /// </summary>
        public void CheckInput()
        {
            if (Input.GetKeyDown(_config.ToggleKey.Value))
            {
                OnTogglePressed?.Invoke();
            }

            if (Input.GetKeyDown(_config.RecenterKey.Value))
            {
                OnRecenterPressed?.Invoke();
            }

            if (Input.GetKeyDown(_config.ToggleReticleKey.Value))
            {
                OnToggleReticlePressed?.Invoke();
            }
        }
    }
}
