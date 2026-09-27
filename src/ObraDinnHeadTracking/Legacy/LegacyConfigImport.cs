using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Input;
using HeadTracking.Config;
using UnityEngine;

namespace HeadTracking.Legacy
{
    /// <summary>
    /// The import the config owner runs on com.headtracking.obradinn.cfg while CameraUnlock.ini is
    /// absent: <see cref="LegacyConfigReader"/> on the plugin's own ConfigFile, then the map into
    /// <see cref="ObraDinnConfig"/>.
    /// </summary>
    internal static class LegacyConfigImport
    {
        /// <summary>The position multiplier every published build shipped on all three axes.</summary>
        public const float ShippedPositionSensitivity = 2.0f;

        /// <summary>The rotation multiplier every published build shipped on all three axes.</summary>
        public const float ShippedRotationSensitivity = 1.0f;

        /// <param name="pluginConfig">The plugin's Config, whose file is the legacy file.</param>
        public static LegacyImport<ObraDinnConfig> For(ConfigFile pluginConfig)
        {
            return new LegacyImport<ObraDinnConfig>((input, config) => Run(pluginConfig, input, config), LegacyConfigKeys.All());
        }

        public static ImportResult Run(ConfigFile pluginConfig, LegacyImportInput input, ObraDinnConfig config)
        {
            if (!string.Equals(Path.GetFullPath(input.Path), Path.GetFullPath(pluginConfig.ConfigFilePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("the owner hands over " + input.Path + ", and the plugin's ConfigFile reads "
                                                    + pluginConfig.ConfigFilePath);
            }

            bool found;
            LegacyConfig legacy = LegacyConfigReader.Read(pluginConfig, out found);
            var dropped = new List<DroppedValue>();
            var poseShaping = new List<PoseShapingValue>();
            var followsDefaultsIni = new LegacyFollowsDefaultsIni();
            Map(legacy, config, dropped, poseShaping, followsDefaultsIni);
            return found
                ? ImportResult.Imported(dropped, poseShaping, followsDefaultsIni.Concepts)
                : ImportResult.Absent(dropped, poseShaping, followsDefaultsIni.Concepts);
        }

        /// <summary>
        /// Every float the reader returns is inside its AcceptableValueRange, which BepInEx clamps
        /// NaN and infinity into, so no value reaches here that normalisation N2 would change.
        /// </summary>
        public static void Map(LegacyConfig legacy, ObraDinnConfig config, List<DroppedValue> dropped,
            List<PoseShapingValue> poseShaping, LegacyFollowsDefaultsIni followsDefaultsIni)
        {
            var shipped = new LegacyConfig();

            config.EnableOnStartup = legacy.EnabledOnStartup;
            config.ShowStartupNotification = legacy.ShowStartupNotification;
            config.UnlockFramerate = legacy.UnlockFramerate;
            config.ShowConnectionNotifications = legacy.ShowConnectionNotifications;
            config.UdpPort = legacy.UDPPort;

            config.ToggleKeyName = HotkeyList(legacy.ToggleKey, KeyCode.Y, "ToggleKey", dropped);
            config.CycleTrackingModeKeyName = HotkeyList(legacy.CycleTrackingModeKey, KeyCode.G, "CycleTrackingModeKey", dropped);

            // The reticle toggle is gone for everyone who had it bound. ShowReticle=true is what
            // the mod still does, so only a player who had hidden the reticle loses a choice.
            if (legacy.ToggleReticleKey != KeyCode.None)
            {
                dropped.Add(new DroppedValue(DropRule.Reticle, "Keybindings", "ToggleReticleKey", legacy.ToggleReticleKey.ToString()));
            }
            if (!legacy.ShowReticle)
            {
                dropped.Add(new DroppedValue(DropRule.Reticle, "UI", "ShowReticle", "false"));
            }

            LegacyPoseShaping.Record(legacy.YawSensitivity, ShippedRotationSensitivity, "Sensitivity", "YawSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PitchSensitivity, ShippedRotationSensitivity, "Sensitivity", "PitchSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.RollSensitivity, ShippedRotationSensitivity, "Sensitivity", "RollSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityX, ShippedPositionSensitivity, "Position", "PositionSensitivityX", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityY, ShippedPositionSensitivity, "Position", "PositionSensitivityY", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityZ, ShippedPositionSensitivity, "Position", "PositionSensitivityZ", poseShaping, dropped);

            // The published builds had one position switch and a three-state cycle that always
            // started from rotation and position, so the switch set the startup mode.
            config.RotationEnabled = true;
            config.PositionEnabled = legacy.PositionEnabled;

            config.LocalSmoothing = legacy.LocalSmoothing;
            config.RemoteSmoothing = legacy.RemoteSmoothing;
            PositionSettings p = config.Position;
            // v1.3.0 built its limits with PositionSettings.Symmetric, so PositionLimitY was the
            // downward limit too.
            config.Position = new PositionSettings(
                p.SensitivityX, p.SensitivityY, p.SensitivityZ,
                legacy.PositionLimitX, legacy.PositionLimitY, legacy.PositionLimitY, legacy.PositionLimitZ, legacy.PositionLimitZBack,
                legacy.LocalSmoothing, legacy.RemoteSmoothing,
                p.InvertX, p.InvertY, p.InvertZ);

            LegacyTrackerPivot.Record(legacy.TrackerPivotForward, shipped.TrackerPivotForward, "Position", "TrackerPivotForward", dropped);

            followsDefaultsIni.Setting(ConfigConcepts.UdpPort, legacy.UDPPort, shipped.UDPPort);
            followsDefaultsIni.Setting(ConfigConcepts.EnableOnStartup, legacy.EnabledOnStartup, shipped.EnabledOnStartup);
            followsDefaultsIni.NotInLegacy(ConfigConcepts.WorldSpaceYaw);
            followsDefaultsIni.TrackingMode(legacy.PositionEnabled, shipped.PositionEnabled);
            followsDefaultsIni.Setting(ConfigConcepts.LocalSmoothing, legacy.LocalSmoothing, shipped.LocalSmoothing);
            followsDefaultsIni.Setting(ConfigConcepts.RemoteSmoothing, legacy.RemoteSmoothing, shipped.RemoteSmoothing);
            followsDefaultsIni.Setting(ConfigConcepts.PositionLimitX, legacy.PositionLimitX, shipped.PositionLimitX);
            followsDefaultsIni.Setting(ConfigConcepts.PositionLimitY, legacy.PositionLimitY, shipped.PositionLimitY);
            followsDefaultsIni.Setting(ConfigConcepts.PositionLimitYDown, legacy.PositionLimitY, shipped.PositionLimitY);
            followsDefaultsIni.Setting(ConfigConcepts.PositionLimitZ, legacy.PositionLimitZ, shipped.PositionLimitZ);
            followsDefaultsIni.Setting(ConfigConcepts.PositionLimitZBack, legacy.PositionLimitZBack, shipped.PositionLimitZBack);
            followsDefaultsIni.Setting(ConfigConcepts.ToggleKey, legacy.ToggleKey, shipped.ToggleKey);
            followsDefaultsIni.Setting(ConfigConcepts.CycleTrackingModeKey, legacy.CycleTrackingModeKey, shipped.CycleTrackingModeKey);
            followsDefaultsIni.NotInLegacy(ConfigConcepts.YawModeKey);
        }

        /// <summary>
        /// The keys v1.3.0 fired an action on: the configured key, unless it was None, and the
        /// Ctrl+Shift chord that InputHandler checked beside it. A Ctrl, Shift or Alt key is
        /// unbound and dropped (N3), which leaves the chord. A key code Unity names no key for (a
        /// number in the .cfg, which BepInEx's enum parse accepts) is written as that number,
        /// which no hotkey list reads, so the owner defers the import and says which line.
        /// </summary>
        public static string HotkeyList(KeyCode primary, KeyCode chordLetter, string key, ICollection<DroppedValue> dropped)
        {
            string chord = KeyBindings.Format(new[] { new KeyBinding(KeyModifiers.Ctrl | KeyModifiers.Shift, (int)chordLetter) });
            string text;
            try
            {
                text = LegacyNormalisations.KeyCodeToBindings((int)primary, "Keybindings", key, dropped);
            }
            catch (ArgumentException)
            {
                text = ((int)primary).ToString(CultureInfo.InvariantCulture);
            }
            return text.Length == 0 ? chord : text + ", " + chord;
        }
    }
}
