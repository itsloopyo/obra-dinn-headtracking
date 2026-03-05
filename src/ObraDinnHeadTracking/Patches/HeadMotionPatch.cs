using HarmonyLib;
using HeadTracking.Core;
using UnityEngine;

namespace HeadTracking.Patches
{
    /// <summary>
    /// Harmony patch for HeadMotion.LateUpdate.
    /// HeadMotion unconditionally overwrites localPosition every frame with walk bob,
    /// wave motion, and crouch offsets. We postfix it to add our tracking position offset
    /// on top of whatever HeadMotion computed.
    /// </summary>
    public static class HeadMotionPatch
    {
        private static bool _patchApplied;
        private static bool _patchFailed;

        public static bool PatchApplied => _patchApplied;

        public static void ApplyPatch(Harmony harmony)
        {
            if (_patchApplied || _patchFailed)
            {
                return;
            }

            var headMotionType = AccessTools.TypeByName("HeadMotion");
            if (headMotionType == null)
            {
                _patchFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "HeadMotion type not found — positional tracking will not work");
                return;
            }

            var lateUpdateMethod = AccessTools.Method(headMotionType, "LateUpdate");
            if (lateUpdateMethod == null)
            {
                _patchFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "HeadMotion.LateUpdate method not found — positional tracking will not work");
                return;
            }

            harmony.Patch(
                lateUpdateMethod,
                postfix: new HarmonyMethod(typeof(HeadMotionPatch), nameof(OnHeadMotionLateUpdatePostfix)));

            _patchApplied = true;
            HeadTrackingPlugin.Instance?.Logger.LogInfo(
                "HeadMotion.LateUpdate patch applied — positional tracking enabled");
        }

        /// <summary>
        /// Postfix: after HeadMotion writes its final localPosition, add our tracking offset.
        /// </summary>
        public static void OnHeadMotionLateUpdatePostfix(MonoBehaviour __instance)
        {
            var plugin = HeadTrackingPlugin.Instance;
            if (plugin == null || !plugin.TrackingEnabled)
            {
                return;
            }

            plugin.CameraController.ApplyPendingPosition(__instance.transform);
        }
    }
}
