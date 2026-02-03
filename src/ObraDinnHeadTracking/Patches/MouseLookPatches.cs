using HarmonyLib;
using HeadTracking.Core;
using UnityEngine;

namespace HeadTracking.Patches
{
    /// <summary>
    /// Harmony patch for MouseLook to track mouse-driven yaw independently of head tracking.
    /// Prevents head tracking yaw from compounding with mouse yaw each frame.
    /// </summary>
    public static class MouseLookPatches
    {
        private static bool _patchApplied;
        private static bool _patchFailed;

        // Internalized state — only used within the postfix
        private static float _pureAimYaw;
        private static bool _pureAimInitialized;
        private static MonoBehaviour _targetInstance;

        /// <summary>
        /// Whether the MouseLook patch was successfully applied.
        /// </summary>
        public static bool PatchApplied => _patchApplied;

        /// <summary>
        /// Reset postfix tracking state. Called from CameraPatches on scene load/shutdown.
        /// </summary>
        internal static void ResetState()
        {
            _pureAimYaw = 0f;
            _pureAimInitialized = false;
            _targetInstance = null;
        }

        /// <summary>
        /// Attempt to apply the MouseLook patch dynamically.
        /// </summary>
        public static void ApplyPatch(Harmony harmony)
        {
            if (_patchApplied || _patchFailed)
            {
                return;
            }

            var mouseLookType = AccessTools.TypeByName("MouseLook");
            if (mouseLookType == null)
            {
                _patchFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "MouseLook type not found in game assembly. " +
                    "Head tracking will not work correctly - yaw will compound infinitely.");
                return;
            }

            var updateMethod = AccessTools.Method(mouseLookType, "Update");
            if (updateMethod == null)
            {
                _patchFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "MouseLook.Update method not found. " +
                    "Head tracking will not work correctly - yaw will compound infinitely.");
                return;
            }

            harmony.Patch(
                updateMethod,
                postfix: new HarmonyMethod(typeof(MouseLookPatches), nameof(OnMouseLookUpdatePostfix)));

            _patchApplied = true;
            HeadTrackingPlugin.Instance?.Logger.LogInfo("MouseLook.Update patch applied - head tracking yaw isolation enabled");
        }

        /// <summary>
        /// Postfix patch for MouseLook.Update.
        /// Tracks mouse delta to maintain "pure aim" yaw without tracking contamination.
        /// </summary>
        public static void OnMouseLookUpdatePostfix(MonoBehaviour __instance)
        {
            var mouseLookTransform = __instance.transform;
            var euler = mouseLookTransform.localEulerAngles;
            float currentYaw = euler.y;

            // Identify the yaw MouseLook by checking if it has significant yaw
            float yaw = currentYaw > 180f ? currentYaw - 360f : currentYaw;
            bool isYawController = Mathf.Abs(yaw) > 1f ||
                                   (_targetInstance != null &&
                                    __instance == _targetInstance);

            if (!isYawController)
            {
                return;
            }

            // First time initialization only - don't re-init if already tracking
            if (!_pureAimInitialized)
            {
                _targetInstance = __instance;
                _pureAimYaw = currentYaw;
                _pureAimInitialized = true;
                return;
            }

            // Update target reference (in case Unity recreated the object)
            _targetInstance = __instance;

            // Calculate mouse delta: what MouseLook added this frame
            // Player body yaw = pureAimYaw (tracking is applied on camera only, not player body)
            float expectedYaw = _pureAimYaw;
            float delta = currentYaw - expectedYaw;
            // Handle 360 wraparound
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;

            _pureAimYaw += delta;
            // Keep in 0-360 range
            if (_pureAimYaw < 0f) _pureAimYaw += 360f;
            if (_pureAimYaw >= 360f) _pureAimYaw -= 360f;
        }
    }
}
