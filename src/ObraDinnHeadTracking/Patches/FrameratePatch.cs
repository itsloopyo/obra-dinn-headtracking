using HarmonyLib;
using HeadTracking.Core;
using UnityEngine;

namespace HeadTracking.Patches
{
    /// <summary>
    /// Harmony patch to unlock the game's framerate cap.
    /// The game defaults to 60 FPS in ScreenHelper.ApplyScreenResolution().
    /// </summary>
    public static class FrameratePatch
    {
        private static bool _patchApplied;
        private static bool _unlockEnabled;

        /// <summary>
        /// Whether the framerate patch was successfully applied.
        /// </summary>
        public static bool PatchApplied => _patchApplied;

        /// <summary>
        /// Apply the ScreenHelper patch to unlock framerate.
        /// </summary>
        public static void ApplyPatch(Harmony harmony, bool unlockEnabled)
        {
            _unlockEnabled = unlockEnabled;

            if (!unlockEnabled)
            {
                HeadTrackingPlugin.Instance?.Logger.LogInfo("Framerate unlock: disabled (using game default 60 FPS)");
                return;
            }

            var screenHelperType = AccessTools.TypeByName("ScreenHelper");
            if (screenHelperType == null)
            {
                HeadTrackingPlugin.Instance?.Logger.LogWarning(
                    "ScreenHelper type not found - cannot unlock framerate");
                return;
            }

            var applyMethod = AccessTools.Method(screenHelperType, "ApplyScreenResolution");
            if (applyMethod == null)
            {
                HeadTrackingPlugin.Instance?.Logger.LogWarning(
                    "ScreenHelper.ApplyScreenResolution not found - cannot unlock framerate");
                return;
            }

            harmony.Patch(
                applyMethod,
                postfix: new HarmonyMethod(typeof(FrameratePatch), nameof(ApplyScreenResolutionPostfix)));

            _patchApplied = true;
            HeadTrackingPlugin.Instance?.Logger.LogInfo("Framerate unlock: ENABLED (VSync off, unlimited FPS)");
        }

        /// <summary>
        /// Postfix patch that overrides the game's framerate settings.
        /// </summary>
        public static void ApplyScreenResolutionPostfix()
        {
            if (!_unlockEnabled)
            {
                return;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }
}
