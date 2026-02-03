using System;
using System.Reflection;
using HeadTracking.Core;
using HarmonyLib;

namespace HeadTracking.Camera
{
    /// <summary>
    /// Shared reflection helper for accessing Player.instance from game assemblies.
    /// Used by both CameraController and GameStateDetector.
    /// </summary>
    internal static class PlayerReflection
    {
        private static bool _initialized;
        private static bool _failed;
        private static Type _playerType;
        private static FieldInfo _instanceField;

        /// <summary>
        /// Whether reflection initialization failed (Player type or instance field not found).
        /// </summary>
        public static bool Failed => _failed;

        /// <summary>
        /// The resolved Player type from Assembly-CSharp.
        /// </summary>
        public static Type PlayerType => _playerType;

        /// <summary>
        /// Initialize reflection for Player.instance. Safe to call multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized || _failed)
            {
                return;
            }

            _playerType = AccessTools.TypeByName("Player");
            if (_playerType == null)
            {
                _failed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError("Player type not found");
                return;
            }

            _instanceField = _playerType.GetField("instance",
                BindingFlags.Public | BindingFlags.Static);
            if (_instanceField == null)
            {
                _failed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError("Player.instance field not found");
                return;
            }

            _initialized = true;
        }

        /// <summary>
        /// Get the current Player.instance value, or null if not spawned.
        /// </summary>
        public static object GetInstance()
        {
            if (_failed || !_initialized)
            {
                return null;
            }

            return _instanceField.GetValue(null);
        }

        private static FieldInfo _mainCameraField;
        private static bool _mainCameraFieldResolved;

        /// <summary>
        /// Get the Camera from Player.instance.mainCamera, or null if unavailable.
        /// </summary>
        public static UnityEngine.Camera GetMainCamera()
        {
            Initialize();
            if (Failed) return null;

            if (!_mainCameraFieldResolved)
            {
                _mainCameraField = _playerType.GetField("mainCamera",
                    BindingFlags.Public | BindingFlags.Instance);
                if (_mainCameraField == null)
                    HeadTrackingPlugin.Instance?.Logger.LogError("Player.mainCamera field not found");
                _mainCameraFieldResolved = true;
            }

            if (_mainCameraField == null) return null;

            var playerInstance = GetInstance();
            if (playerInstance == null) return null;

            return _mainCameraField.GetValue(playerInstance) as UnityEngine.Camera;
        }
    }
}
