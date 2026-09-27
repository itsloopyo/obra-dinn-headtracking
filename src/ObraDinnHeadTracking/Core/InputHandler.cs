using System;
using CameraUnlock.Core.Input;
using CameraUnlock.Core.Unity.Extensions;
using HeadTracking.Config;

namespace HeadTracking.Core
{
    /// <summary>
    /// Fires the mod's hotkey actions from the key lists in CameraUnlock.ini. Every binding in a
    /// list is an ordinary item, the Ctrl+Shift chords included.
    /// </summary>
    public class InputHandler
    {
        private readonly KeyBinding[] _toggle;
        private readonly KeyBinding[] _cycleTrackingMode;
        private readonly KeyBinding[] _yawMode;

        /// <summary>
        /// Fired when toggle key is pressed.
        /// </summary>
        public event Action OnTogglePressed;

        /// <summary>
        /// Fired when cycle tracking mode key is pressed.
        /// Cycles: rotation and position -> rotation only -> position only -> rotation and position.
        /// </summary>
        public event Action OnCycleTrackingModePressed;

        /// <summary>
        /// Fired when the yaw mode key is pressed. Switches between world-locked and camera-local yaw.
        /// </summary>
        public event Action OnToggleYawModePressed;

        public InputHandler(ObraDinnConfig config)
        {
            _toggle = Parse("ToggleKey", config.ToggleKeyName);
            _cycleTrackingMode = Parse("CycleTrackingModeKey", config.CycleTrackingModeKeyName);
            _yawMode = Parse("YawModeKey", config.YawModeKeyName);
        }

        /// <summary>
        /// Check for input. Call from Update.
        /// </summary>
        public void CheckInput()
        {
            if (KeyBindingInput.IsTriggered(_toggle))
            {
                OnTogglePressed?.Invoke();
            }

            if (KeyBindingInput.IsTriggered(_cycleTrackingMode))
            {
                OnCycleTrackingModePressed?.Invoke();
            }

            if (KeyBindingInput.IsTriggered(_yawMode))
            {
                OnToggleYawModePressed?.Invoke();
            }
        }

        // The table's hotkey codec has read every list the file holds, and the legacy import
        // writes only key lists, so a list that does not parse is a bug.
        private static KeyBinding[] Parse(string key, string text)
        {
            KeyBinding[] bindings;
            string error;
            if (!KeyBindings.TryParse(text, out bindings, out error))
                throw new InvalidOperationException("[Hotkeys] " + key + "=" + text + ": " + error);
            return bindings;
        }
    }
}
