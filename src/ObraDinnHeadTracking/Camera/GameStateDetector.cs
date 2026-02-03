using System;
using System.Reflection;
using HeadTracking.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadTracking.Camera
{
    /// <summary>
    /// Detects when player is in gameplay vs menus/cutscenes to control tracking activation.
    /// Uses Obra Dinn's Clock.play.running as the authoritative source for gameplay state.
    /// </summary>
    public class GameStateDetector
    {
        private const float CheckIntervalSeconds = 0.1f;

        private GameState _currentState = GameState.Unknown;
        private float _lastCheckTime;

        // Reflection cache for Clock.play.running
        private static bool _reflectionInitialized;
        private static bool _reflectionFailed;
        private static PropertyInfo _clockPlayProperty;
        private static PropertyInfo _clockRunningProperty;

        // Reflection cache for Player.inputEnabled (Player type/instance from PlayerReflection)
        private static PropertyInfo _playerInputEnabledProperty;
        private static bool _inputEnabledResolved;

        // Frame-level cache: avoids repeated reflection from OnGUI shouldDraw callbacks
        private int _inputEnabledCacheFrame = -1;
        private bool _inputEnabledCacheValue;

        /// <summary>
        /// Event fired when game state changes.
        /// </summary>
        public event Action<GameState> StateChanged;

        /// <summary>
        /// Current detected game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Returns true if tracking should be active based on current game state.
        /// Checks Player.instance.inputEnabled which is false during:
        /// - Prop interactions (case, doors, etc.)
        /// - CannedAnim (ladders)
        /// - Dialogs and cutscenes
        /// Result is cached per frame to avoid repeated reflection from OnGUI callbacks.
        /// </summary>
        public bool IsGameplayActive
        {
            get
            {
                if (_currentState != GameState.Gameplay)
                {
                    return false;
                }

                int frame = Time.frameCount;
                if (_inputEnabledCacheFrame != frame)
                {
                    _inputEnabledCacheFrame = frame;
                    _inputEnabledCacheValue = GetPlayerInputEnabled();
                }
                return _inputEnabledCacheValue;
            }
        }

        /// <summary>
        /// Returns true if player input is currently enabled (player has camera control).
        /// </summary>
        public bool IsPlayerInputEnabled() => GetPlayerInputEnabled();

        /// <summary>
        /// Initialize the detector.
        /// </summary>
        public void Initialize()
        {
            InitializeReflection();
            SceneManager.sceneLoaded += OnSceneLoaded;
            UpdateState();
        }

        /// <summary>
        /// Clean up event subscriptions.
        /// </summary>
        public void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Check for state changes. Call from Update (rate-limited internally).
        /// </summary>
        public void Update()
        {
            // Rate limit state checks to avoid per-frame overhead
            if (Time.time - _lastCheckTime < CheckIntervalSeconds)
            {
                return;
            }

            _lastCheckTime = Time.time;
            UpdateState();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Force immediate state check on scene load
            _lastCheckTime = 0f;
            UpdateState();
        }

        private void UpdateState()
        {
            var newState = DetectState();

            if (newState != _currentState)
            {
                _currentState = newState;
                StateChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// Initialize reflection to access Clock.play.running.
        /// </summary>
        private static void InitializeReflection()
        {
            if (_reflectionInitialized || _reflectionFailed)
            {
                return;
            }

            var clockType = AccessTools.TypeByName("Clock");
            if (clockType == null)
            {
                _reflectionFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "Clock type not found. Game state detection will not work correctly.");
                return;
            }

            _clockPlayProperty = clockType.GetProperty("play", BindingFlags.Public | BindingFlags.Static);
            if (_clockPlayProperty == null)
            {
                _reflectionFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "Clock.play property not found. Game state detection will not work correctly.");
                return;
            }

            _clockRunningProperty = _clockPlayProperty.PropertyType.GetProperty("running", BindingFlags.Public | BindingFlags.Instance);
            if (_clockRunningProperty == null)
            {
                _reflectionFailed = true;
                HeadTrackingPlugin.Instance?.Logger.LogError(
                    "Clock.running property not found. Game state detection will not work correctly.");
                return;
            }

            _reflectionInitialized = true;
        }

        /// <summary>
        /// Get the value of Clock.play.running via reflection.
        /// Returns true if gameplay is active (clock is running).
        /// </summary>
        private static bool GetClockPlayRunning()
        {
            if (_reflectionFailed || !_reflectionInitialized)
            {
                // Reflection not available - cannot determine state
                // This will cause DetectState to use secondary detection
                return false;
            }

            var playInstance = _clockPlayProperty.GetValue(null, null);
            if (playInstance == null)
            {
                return false;
            }

            return (bool)_clockRunningProperty.GetValue(playInstance, null);
        }

        /// <summary>
        /// Resolve the Player.inputEnabled property via PlayerReflection.
        /// </summary>
        private static void ResolveInputEnabledProperty()
        {
            if (_inputEnabledResolved)
            {
                return;
            }

            PlayerReflection.Initialize();
            if (PlayerReflection.Failed)
            {
                _inputEnabledResolved = true;
                return;
            }

            _playerInputEnabledProperty = PlayerReflection.PlayerType.GetProperty("inputEnabled",
                BindingFlags.Public | BindingFlags.Instance);
            if (_playerInputEnabledProperty == null)
            {
                HeadTrackingPlugin.Instance?.Logger.LogWarning(
                    "Player.inputEnabled property not found. Input state detection disabled.");
            }

            _inputEnabledResolved = true;
        }

        /// <summary>
        /// Check if player input is currently enabled via Player.instance.inputEnabled.
        /// Returns true if player has camera control, false during animations/interactions.
        /// </summary>
        private static bool GetPlayerInputEnabled()
        {
            ResolveInputEnabledProperty();

            if (PlayerReflection.Failed || _playerInputEnabledProperty == null)
            {
                return true;
            }

            var playerInstance = PlayerReflection.GetInstance();
            if (playerInstance == null)
            {
                return true;
            }

            return (bool)_playerInputEnabledProperty.GetValue(playerInstance, null);
        }

        private GameState DetectState()
        {
            // Always check for menu scenes first, regardless of clock state
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.Equals(sceneName, "Title", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sceneName, "Credits", StringComparison.OrdinalIgnoreCase))
            {
                return GameState.MainMenu;
            }

            if (string.Equals(sceneName, "Intro", StringComparison.OrdinalIgnoreCase))
            {
                return GameState.Loading;
            }

            // Primary detection: Use Clock.play.running
            // This is what MouseLook.Update() checks before processing input
            if (_reflectionInitialized && !_reflectionFailed)
            {
                bool clockRunning = GetClockPlayRunning();
                if (clockRunning)
                {
                    return GameState.Gameplay;
                }

                // Clock not running - paused state
                return GameState.Paused;
            }

            // Reflection failed - use secondary heuristics
            return DetectStateSecondary();
        }

        /// <summary>
        /// Secondary detection when Clock reflection is unavailable.
        /// </summary>
        private GameState DetectStateSecondary()
        {
            // Check time scale (paused game sets timeScale to 0)
            if (Time.timeScale < 0.01f)
            {
                return GameState.Paused;
            }

            var sceneName = SceneManager.GetActiveScene().name.ToLowerInvariant();

            if (sceneName.Contains("title") || sceneName.Contains("menu"))
            {
                return GameState.MainMenu;
            }

            if (sceneName.Contains("intro") || sceneName.Contains("load"))
            {
                return GameState.Loading;
            }

            if (sceneName.Contains("credit"))
            {
                return GameState.MainMenu;
            }

            // Default to gameplay
            return GameState.Gameplay;
        }
    }
}
