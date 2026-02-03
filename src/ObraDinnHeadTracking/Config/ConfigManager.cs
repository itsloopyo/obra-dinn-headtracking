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
        public ConfigEntry<KeyCode> RecenterKey { get; private set; }
        public ConfigEntry<KeyCode> ToggleReticleKey { get; private set; }

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
        public ConfigEntry<float> Smoothing { get; private set; }

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
                "Key to toggle head tracking on/off"
            );

            RecenterKey = config.Bind(
                "Keybindings",
                "RecenterKey",
                KeyCode.Home,
                "Key to recenter head tracking (resets offset to current head position)"
            );

            ToggleReticleKey = config.Bind(
                "Keybindings",
                "ToggleReticleKey",
                KeyCode.Insert,
                "Key to toggle the aim reticle on/off"
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
            Smoothing = config.Bind(
                "Smoothing",
                "Smoothing",
                0.0f,
                new ConfigDescription(
                    "Base smoothing level (0 = no smoothing, 1 = max smoothing). " +
                    "Higher values reduce jitter but add latency. " +
                    "Remote connections automatically use a minimum of 0.1 for network latency compensation.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
        }
    }
}
