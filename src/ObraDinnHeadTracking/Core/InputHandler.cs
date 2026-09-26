using System;
using System.Collections.Generic;
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

        public InputHandler(ObraDinnConfig config, Action<string> logWarning)
        {
            _toggle = Parse("ToggleKey", config.ToggleKeyName, logWarning);
            _cycleTrackingMode = Parse("CycleTrackingModeKey", config.CycleTrackingModeKeyName, logWarning);
            _yawMode = Parse("YawModeKey", config.YawModeKeyName, logWarning);
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

        // The table's hotkey codec has read every list the file holds, so a list that does not
        // parse reaches here only from a legacy import the owner deferred: a .cfg key code Unity
        // names no key for, which the import writes as the number. v1.3.0 never fired on such a
        // key and still fired the chord beside it, so the items that parse are bound and the
        // rest are named in the log.
        private static KeyBinding[] Parse(string key, string text, Action<string> logWarning)
        {
            KeyBinding[] bindings;
            string error;
            if (KeyBindings.TryParse(text, out bindings, out error)) return bindings;

            var kept = new List<KeyBinding>();
            foreach (string item in text.Split(','))
            {
                if (KeyBindings.TryParse(item, out bindings, out error)) kept.AddRange(bindings);
                else logWarning("[Hotkeys] " + key + ": " + error + ", so it is not bound this session");
            }
            return kept.ToArray();
        }
    }
}
