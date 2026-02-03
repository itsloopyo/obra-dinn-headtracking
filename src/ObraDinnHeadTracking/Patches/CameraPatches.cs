using System;
using HeadTracking.Core;
using UnityEngine;

namespace HeadTracking.Patches
{
    /// <summary>
    /// Harmony patches for camera and scene integration.
    /// These patches ensure the head tracking system responds properly to game events.
    /// </summary>
    public static class CameraPatches
    {
        /// <summary>
        /// Event fired when a scene finishes loading.
        /// </summary>
        public static event Action OnSceneLoaded;

        /// <summary>
        /// Event fired when the main camera changes.
        /// </summary>
        public static event Action<UnityEngine.Camera> OnCameraChanged;

        private static UnityEngine.Camera _lastKnownCamera;

        /// <summary>
        /// The cached main camera reference. Updated once per frame by CheckCameraChange().
        /// Use this instead of Camera.main to avoid repeated FindGameObjectWithTag calls.
        /// </summary>
        public static UnityEngine.Camera CachedMainCamera => _lastKnownCamera;

        /// <summary>
        /// Check if the main camera has changed since last frame.
        /// Should be called once per frame from Update() to detect camera transitions.
        /// </summary>
        public static void CheckCameraChange()
        {
            var currentCamera = UnityEngine.Camera.main;
            if (currentCamera != _lastKnownCamera)
            {
                _lastKnownCamera = currentCamera;
                OnCameraChanged?.Invoke(currentCamera);
            }
        }

        /// <summary>
        /// Reset internal state (e.g., on shutdown).
        /// </summary>
        public static void Reset()
        {
            ResetAllFields();
        }

        /// <summary>
        /// Internal method called by Harmony patch when scene loads.
        /// </summary>
        internal static void NotifySceneLoaded()
        {
            ResetAllFields();
            OnSceneLoaded?.Invoke();
        }

        private static void ResetAllFields()
        {
            _lastKnownCamera = null;
            MouseLookPatches.ResetState();
        }
    }
}
