using HeadTracking.Config;

namespace HeadTracking.Legacy
{
    /// <summary>Carries what <see cref="LegacyConfigReader"/> read into the settings the plugin runs on.</summary>
    internal static class LegacyConfigMap
    {
        public static ModConfig ToRuntime(LegacyConfig legacy)
        {
            return new ModConfig
            {
                EnabledOnStartup = legacy.EnabledOnStartup,
                ShowStartupNotification = legacy.ShowStartupNotification,
                UnlockFramerate = legacy.UnlockFramerate,
                ToggleKey = legacy.ToggleKey,
                ToggleReticleKey = legacy.ToggleReticleKey,
                CycleTrackingModeKey = legacy.CycleTrackingModeKey,
                ShowConnectionNotifications = legacy.ShowConnectionNotifications,
                ShowReticle = legacy.ShowReticle,
                UdpPort = legacy.UDPPort,
                YawSensitivity = legacy.YawSensitivity,
                PitchSensitivity = legacy.PitchSensitivity,
                RollSensitivity = legacy.RollSensitivity,
                LocalSmoothing = legacy.LocalSmoothing,
                RemoteSmoothing = legacy.RemoteSmoothing,
                PositionEnabled = legacy.PositionEnabled,
                PositionSensitivityX = legacy.PositionSensitivityX,
                PositionSensitivityY = legacy.PositionSensitivityY,
                PositionSensitivityZ = legacy.PositionSensitivityZ,
                PositionLimitX = legacy.PositionLimitX,
                PositionLimitY = legacy.PositionLimitY,
                PositionLimitZ = legacy.PositionLimitZ,
                PositionLimitZBack = legacy.PositionLimitZBack,
                TrackerPivotForward = legacy.TrackerPivotForward,
            };
        }
    }
}
