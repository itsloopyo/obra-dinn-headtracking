using BepInEx.Configuration;
using UnityEngine;

namespace HeadTracking.Config
{
    /// <summary>
    /// Manages BepInEx configuration entries with sensible defaults.
    /// </summary>
    public class ConfigManager
    {
        // General settings
        public ConfigEntry<bool> EnabledOnStartup { get; private set; }
        public ConfigEntry<bool> ShowStartupNotification { get; private set; }
        public ConfigEntry<bool> UnlockFramerate { get; private set; }

        // Keybinding settings
        public ConfigEntry<KeyCode> ToggleKey { get; private set; }
        public ConfigEntry<KeyCode> ToggleReticleKey { get; private set; }
        public ConfigEntry<KeyCode> CycleTrackingModeKey { get; private set; }
        // UI settings
        public ConfigEntry<bool> ShowConnectionNotifications { get; private set; }
        public ConfigEntry<bool> ShowReticle { get; private set; }

        // Network settings
        public ConfigEntry<int> UDPPort { get; private set; }

        // Sensitivity settings
        public ConfigEntry<float> YawSensitivity { get; private set; }
        public ConfigEntry<float> PitchSensitivity { get; private set; }
        public ConfigEntry<float> RollSensitivity { get; private set; }

        // Smoothing settings
        public ConfigEntry<float> LocalSmoothing { get; private set; }
        public ConfigEntry<float> RemoteSmoothing { get; private set; }

        // Position settings
        public ConfigEntry<bool> PositionEnabled { get; private set; }
        public ConfigEntry<float> PositionSensitivityX { get; private set; }
        public ConfigEntry<float> PositionSensitivityY { get; private set; }
        public ConfigEntry<float> PositionSensitivityZ { get; private set; }
        public ConfigEntry<float> PositionLimitX { get; private set; }
        public ConfigEntry<float> PositionLimitY { get; private set; }
        public ConfigEntry<float> PositionLimitZ { get; private set; }
        public ConfigEntry<float> PositionLimitZBack { get; private set; }

        // Pivot compensation
        public ConfigEntry<float> TrackerPivotForward { get; private set; }

        /// <summary>
        /// Initialize all configuration entries. Must be called in plugin Awake().
        /// </summary>
        public void Initialize(ConfigFile config)
        {
            // General section
            EnabledOnStartup = config.Bind(
                "General",
                "EnabledOnStartup",
                true,
                "Whether head tracking is enabled when the game starts"
            );

            ShowStartupNotification = config.Bind(
                "General",
                "ShowStartupNotification",
                true,
                "Whether to show a notification when the plugin initializes"
            );

            UnlockFramerate = config.Bind(
                "General",
                "UnlockFramerate",
                true,
                "Unlock the game's 60 FPS cap. Set to false to restore the default 60 FPS limit"
            );

            // UI section
            ShowConnectionNotifications = config.Bind(
                "UI",
                "ShowConnectionNotifications",
                true,
                "Whether to show notifications when OpenTrack connection is lost or restored"
            );

            ShowReticle = config.Bind(
                "UI",
                "ShowReticle",
                true,
                "Whether to show the aim reticle during gameplay"
            );

            // Keybindings section
            ToggleKey = config.Bind(
                "Keybindings",
                "ToggleKey",
                KeyCode.End,
                "Key to toggle head tracking on/off (also Ctrl+Shift+Y)"
            );

            ToggleReticleKey = config.Bind(
                "Keybindings",
                "ToggleReticleKey",
                KeyCode.PageDown,
                "Key to toggle the aim reticle on/off (also Ctrl+Shift+H)"
            );

            CycleTrackingModeKey = config.Bind(
                "Keybindings",
                "CycleTrackingModeKey",
                KeyCode.PageUp,
                "Key to cycle tracking mode: normal -> rotation only -> position only -> normal (also Ctrl+Shift+G)"
            );

            // Network section
            UDPPort = config.Bind(
                "Network",
                "UDPPort",
                4242,
                new ConfigDescription(
                    "UDP port to listen for OpenTrack data (must match OpenTrack output settings)",
                    new AcceptableValueRange<int>(1024, 65535)
                )
            );

            // Sensitivity section
            YawSensitivity = config.Bind(
                "Sensitivity",
                "YawSensitivity",
                1.0f,
                new ConfigDescription(
                    "Multiplier for horizontal head rotation (left/right)",
                    new AcceptableValueRange<float>(0.1f, 3.0f)
                )
            );

            PitchSensitivity = config.Bind(
                "Sensitivity",
                "PitchSensitivity",
                1.0f,
                new ConfigDescription(
                    "Multiplier for vertical head rotation (up/down)",
                    new AcceptableValueRange<float>(0.1f, 3.0f)
                )
            );

            RollSensitivity = config.Bind(
                "Sensitivity",
                "RollSensitivity",
                1.0f,
                new ConfigDescription(
                    "Multiplier for head tilt (ear to shoulder)",
                    new AcceptableValueRange<float>(0.0f, 3.0f)
                )
            );

            // Smoothing section
            LocalSmoothing = config.Bind(
                "Smoothing",
                "LocalSmoothing",
                0.0f,
                new ConfigDescription(
                    "Smoothing applied when the tracker runs on this machine (loopback). " +
                    "0 = no smoothing, 1 = heavy. Covers rotation and position.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            RemoteSmoothing = config.Bind(
                "Smoothing",
                "RemoteSmoothing",
                0.15f,
                new ConfigDescription(
                    "Smoothing applied when the tracker is a remote device on the network. " +
                    "0 = no smoothing, 1 = heavy. Covers rotation and position.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            // Position section
            PositionEnabled = config.Bind(
                "Position",
                "PositionEnabled",
                true,
                "Enable positional tracking (lean in/out/side-to-side)"
            );

            PositionSensitivityX = config.Bind(
                "Position",
                "PositionSensitivityX",
                2.0f,
                new ConfigDescription(
                    "Multiplier for lateral (left/right) position",
                    new AcceptableValueRange<float>(0f, 3.0f)
                )
            );

            PositionSensitivityY = config.Bind(
                "Position",
                "PositionSensitivityY",
                2.0f,
                new ConfigDescription(
                    "Multiplier for vertical (up/down) position",
                    new AcceptableValueRange<float>(0f, 3.0f)
                )
            );

            PositionSensitivityZ = config.Bind(
                "Position",
                "PositionSensitivityZ",
                2.0f,
                new ConfigDescription(
                    "Multiplier for depth (forward/back) position",
                    new AcceptableValueRange<float>(0f, 3.0f)
                )
            );

            PositionLimitX = config.Bind(
                "Position",
                "PositionLimitX",
                0.30f,
                new ConfigDescription(
                    "Maximum lateral displacement in meters (prevents wall clipping)",
                    new AcceptableValueRange<float>(0.01f, 0.5f)
                )
            );

            PositionLimitY = config.Bind(
                "Position",
                "PositionLimitY",
                0.20f,
                new ConfigDescription(
                    "Maximum vertical displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f)
                )
            );

            PositionLimitZ = config.Bind(
                "Position",
                "PositionLimitZ",
                0.40f,
                new ConfigDescription(
                    "Maximum forward depth displacement in meters (prevents wall clipping)",
                    new AcceptableValueRange<float>(0.01f, 0.5f)
                )
            );

            PositionLimitZBack = config.Bind(
                "Position",
                "PositionLimitZBack",
                0.10f,
                new ConfigDescription(
                    "Maximum backward depth displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f)
                )
            );

            TrackerPivotForward = config.Bind(
                "Position",
                "TrackerPivotForward",
                0.08f,
                new ConfigDescription(
                    "Distance in meters from the pivot point to the tracker's face point. " +
                    "Compensates the lateral arc that head yaw introduces into position data. " +
                    "Increase if yawing feels like orbiting forward, decrease if it orbits backward.",
                    new AcceptableValueRange<float>(0f, 0.20f)
                )
            );

        }
    }
}
