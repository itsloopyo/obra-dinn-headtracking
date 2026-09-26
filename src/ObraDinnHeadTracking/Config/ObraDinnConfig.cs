using CameraUnlock.Core.Config;

namespace HeadTracking.Config
{
    /// <summary>
    /// Everything the mod reads from BepInEx\config\CameraUnlock.ini. Unity-free, so the test
    /// project compiles it and holds the committed file to it.
    /// </summary>
    public sealed class ObraDinnConfig : HeadTrackingConfigData
    {
        /// <summary>The game's name as data/games.json spells it.</summary>
        public const string DisplayName = "Return of the Obra Dinn";

        public bool UnlockFramerate { get; set; } = true;

        public bool ShowStartupNotification { get; set; } = true;

        public bool ShowConnectionNotifications { get; set; } = true;

        public static ConfigTable<ObraDinnConfig> Table()
        {
            return HeadTrackingConfigTable.Create<ObraDinnConfig>(
                    ConfigConcepts.UdpPort,
                    ConfigConcepts.EnableOnStartup,
                    ConfigConcepts.RotationEnabled,
                    ConfigConcepts.LocalSmoothing,
                    ConfigConcepts.RemoteSmoothing,
                    ConfigConcepts.PositionEnabled,
                    ConfigConcepts.PositionLimitX,
                    ConfigConcepts.PositionLimitY,
                    ConfigConcepts.PositionLimitYDown,
                    ConfigConcepts.PositionLimitZ,
                    ConfigConcepts.PositionLimitZBack,
                    ConfigConcepts.TrackerPivotForward,
                    ConfigConcepts.ToggleKey,
                    ConfigConcepts.CycleTrackingModeKey)
                .Select(ConfigConcepts.RotationEnabled).Writable()
                .Select(ConfigConcepts.PositionEnabled).Writable()
                .Select(ConfigConcepts.TrackerPivotForward)
                .Comment("Metres from the pivot of your neck forward to the point the tracker follows.\n" +
                         "Used to remove the lean that turning your head adds. 0 turns it off.")
                .Local("Display", "UnlockFramerate", c => c.UnlockFramerate, (c, v) => c.UnlockFramerate = v,
                    new BoolCodec(),
                    "true: remove the game's 60 FPS cap. false: keep it.")
                .Local("Notifications", "ShowStartupNotification", c => c.ShowStartupNotification,
                    (c, v) => c.ShowStartupNotification = v, new BoolCodec(),
                    "true: show whether head tracking is on, and its hotkeys, when the game starts.")
                .Local("Notifications", "ShowConnectionNotifications", c => c.ShowConnectionNotifications,
                    (c, v) => c.ShowConnectionNotifications = v, new BoolCodec(),
                    "true: show a message when tracker data starts or stops arriving.");
        }
    }
}
