using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Unity.Extensions;
using CameraUnlock.Core.Unity.Rendering;
using CameraUnlock.Core.Unity.UI;
using HeadTracking.Camera;
using HeadTracking.Config;
using HeadTracking.Patches;

namespace HeadTracking.Core
{
    /// <summary>
    /// BepInEx plugin entry point for Obra Dinn Head Tracking.
    /// Initializes all subsystems and manages the plugin lifecycle.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HeadTrackingPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.headtracking.obradinn";
        public const string PluginName = "Obra Dinn Head Tracking";
        public const string PluginVersion = "1.0.0";

        /// <summary>
        /// Singleton instance for cross-component access.
        /// </summary>
        public static HeadTrackingPlugin Instance { get; private set; }

        /// <summary>
        /// Plugin logger for all components.
        /// </summary>
        public new ManualLogSource Logger => base.Logger;

        /// <summary>
        /// Whether head tracking is currently enabled.
        /// </summary>
        public bool TrackingEnabled { get; private set; }

        // Harmony instance for patching
        private Harmony _harmony;

        // Components
        private ConfigManager _config;
        private OpenTrackReceiver _receiver;
        private TrackingProcessor _processor;
        private PoseInterpolator _interpolator;
        private CameraController _cameraController;
        private GameStateDetector _gameStateDetector;
        private InputHandler _inputHandler;
        private NotificationUI _notificationUI;
        private IMGUIReticle _aimReticle;
        private bool _reticleEnabled;
        // Connection state tracking
        private bool _wasReceiving;


        private void Awake()
        {
            Instance = this;

            Logger.LogInfo($"{PluginName} v{PluginVersion} initializing...");

            // Initialize Harmony patching
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(typeof(HeadTrackingPlugin).Assembly);
            Logger.LogInfo("Harmony patches applied");

            // Try to apply game-specific patches
            MouseLookPatches.ApplyPatch(_harmony);

            // Initialize configuration (needed before framerate patch)
            _config = new ConfigManager();
            _config.Initialize(Config);

            // Apply framerate unlock patch if enabled
            FrameratePatch.ApplyPatch(_harmony, _config.UnlockFramerate.Value);

            // Initialize components
            _receiver = new OpenTrackReceiver();
            _processor = new TrackingProcessor
            {
                SmoothingFactor = _config.Smoothing.Value,
                Sensitivity = new SensitivitySettings(
                    _config.YawSensitivity.Value,
                    _config.PitchSensitivity.Value,
                    _config.RollSensitivity.Value,
                    invertYaw: false,
                    invertPitch: false,
                    invertRoll: false
                ),
                Deadzone = DeadzoneSettings.None
            };
            _interpolator = new PoseInterpolator();
            _cameraController = new CameraController(_receiver, _processor, _interpolator);
            _gameStateDetector = new GameStateDetector();
            _inputHandler = new InputHandler(_config);
            _notificationUI = new NotificationUI();

            // Initialize aim reticle
            _reticleEnabled = _config.ShowReticle.Value;
            _aimReticle = gameObject.AddComponent<IMGUIReticle>();
            _aimReticle.Style = ReticleStyle.Dot;
            _aimReticle.BaseSizeAt1080p = 6;
            _aimReticle.OutlineWidthAt1080p = 2;
            _aimReticle.ReticleColor = UnityEngine.Color.white;
            _aimReticle.OutlineColor = UnityEngine.Color.black;
            _aimReticle.IsVisible = _reticleEnabled;
            _aimReticle.InitializeWithOffset(
                getOffset: () => CalculateAimOffset(),
                shouldDraw: () => _gameStateDetector.IsGameplayActive && _reticleEnabled && _cameraController.IsApplyingTracking
            );

            // Subscribe to input events
            _inputHandler.OnTogglePressed += HandleToggle;
            _inputHandler.OnRecenterPressed += HandleRecenter;
            _inputHandler.OnToggleReticlePressed += HandleToggleReticle;

            // Subscribe to game state changes
            _gameStateDetector.StateChanged += OnGameStateChanged;
            _gameStateDetector.Initialize();

            // Subscribe to Harmony patch events
            CameraPatches.OnSceneLoaded += OnSceneLoadedPatch;
            CameraPatches.OnCameraChanged += OnCameraChangedPatch;

            // Start UDP receiver
            _receiver.Start(_config.UDPPort.Value);

            // Set initial tracking state from config
            TrackingEnabled = _config.EnabledOnStartup.Value;

            Logger.LogInfo($"{PluginName} initialized. Tracking {(TrackingEnabled ? "enabled" : "disabled")}");

            if (!MouseLookPatches.PatchApplied)
                Logger.LogWarning("MouseLook patch FAILED - head tracking will NOT work");
            Logger.LogInfo($"Listening on UDP port {_config.UDPPort.Value}");

            // Show startup notification if enabled
            if (_config.ShowStartupNotification.Value)
            {
                string keyInfo = $"[{_inputHandler.ToggleKey}] Toggle, [{_inputHandler.RecenterKey}] Recenter, [{_inputHandler.ToggleReticleKey}] Reticle";
                string statusInfo = TrackingEnabled ? "Head Tracking: ON" : "Head Tracking: OFF";
                _notificationUI.ShowNotification($"{statusInfo}\n{keyInfo}", 4f);
            }
        }

        private void Update()
        {
            _inputHandler.CheckInput();
            _gameStateDetector.Update();
            _notificationUI.Update();

            // Check for camera changes each frame
            CameraPatches.CheckCameraChange();

            // Monitor connection state and show notifications on change
            MonitorConnectionState();
        }

        private void MonitorConnectionState()
        {
            bool isReceiving = _receiver.IsReceiving;

            if (isReceiving != _wasReceiving)
            {
                if (_config.ShowConnectionNotifications.Value)
                {
                    if (isReceiving)
                    {
                        _notificationUI.ShowConnectionEstablished();
                        Logger.LogInfo("OpenTrack connection established");
                    }
                    else
                    {
                        _notificationUI.ShowConnectionLost();
                        Logger.LogInfo("OpenTrack connection lost");
                    }
                }
                _wasReceiving = isReceiving;
            }
        }

        private void LateUpdate()
        {
            bool shouldTrack = TrackingEnabled && _gameStateDetector.IsGameplayActive;
            _cameraController.ProcessFrame(shouldTrack);
        }

        private void OnGUI()
        {
            _notificationUI.Draw();
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"{PluginName} shutting down...");

            // Unsubscribe from events
            _inputHandler.OnTogglePressed -= HandleToggle;
            _inputHandler.OnRecenterPressed -= HandleRecenter;
            _inputHandler.OnToggleReticlePressed -= HandleToggleReticle;
            _gameStateDetector.StateChanged -= OnGameStateChanged;
            CameraPatches.OnSceneLoaded -= OnSceneLoadedPatch;
            CameraPatches.OnCameraChanged -= OnCameraChangedPatch;

            // Cleanup components
            _gameStateDetector.Shutdown();
            _receiver.Dispose();
            CameraPatches.Reset();

            // Unpatch Harmony
            _harmony?.UnpatchSelf();

            Instance = null;
        }

        private void HandleToggle()
        {
            TrackingEnabled = !TrackingEnabled;

            if (TrackingEnabled)
            {
                _cameraController.OnTrackingEnabled();
                _notificationUI.ShowTrackingEnabled();
                Logger.LogInfo("Head tracking enabled");
            }
            else
            {
                _cameraController.OnTrackingDisabled();
                _notificationUI.ShowTrackingDisabled();
                Logger.LogInfo("Head tracking disabled");
            }
        }

        private void HandleRecenter()
        {
            var rawPose = _receiver.GetLatestPose();
            _processor.RecenterTo(rawPose);
            _interpolator.Reset();
            _notificationUI.ShowRecentered();
            Logger.LogInfo("Head tracking recentered");
        }

        private void HandleToggleReticle()
        {
            _reticleEnabled = !_reticleEnabled;
            _aimReticle.IsVisible = _reticleEnabled;

            if (_reticleEnabled)
            {
                _notificationUI.ShowNotification("Reticle: ON", NotificationType.Success, 1.5f);
            }
            else
            {
                _notificationUI.ShowNotification("Reticle: OFF", NotificationType.Warning, 1.5f);
            }
            Logger.LogInfo($"Reticle {(_reticleEnabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Calculates the screen offset for the aim reticle based on current head tracking rotation.
        /// The reticle shows where you're aiming (mouse direction) vs where you're looking (head direction).
        /// Must match the rotation composition in CameraController.ApplyComposedRotation:
        ///   rotAfter = Euler(gamePitch, 0, 0) * FromYawPitchRoll(yaw, -pitch, roll)
        /// </summary>
        private UnityEngine.Vector2 CalculateAimOffset()
        {
            var cam = CameraPatches.CachedMainCamera;
            if (cam == null)
            {
                return UnityEngine.Vector2.zero;
            }

            float yawDeg = _cameraController.LastTrackingYaw;
            float pitchDeg = _cameraController.LastTrackingPitch;
            float rollDeg = _cameraController.LastTrackingRoll;
            float gamePitchDeg = _cameraController.GameCameraPitch;

            // Match the rotation composition from ApplyComposedRotation:
            //   rotBefore = Euler(gamePitch, 0, 0)                                    — camera before tracking
            //   rotAfter  = Euler(gamePitch, 0, 0) * FromYawPitchRoll(yaw, -pitch, roll) — camera after tracking
            // Aim direction in camera-local space = Inverse(rotAfter) * rotBefore * forward
            var trackingQ = CameraUnlock.Core.Math.QuaternionUtils.FromYawPitchRoll(yawDeg, -pitchDeg, rollDeg);
            UnityEngine.Quaternion rotBefore = UnityEngine.Quaternion.Euler(gamePitchDeg, 0f, 0f);
            UnityEngine.Quaternion rotAfter = rotBefore * new UnityEngine.Quaternion(trackingQ.X, trackingQ.Y, trackingQ.Z, trackingQ.W);
            UnityEngine.Vector3 aimDir = UnityEngine.Quaternion.Inverse(rotAfter) * rotBefore * UnityEngine.Vector3.forward;

            if (aimDir.z < 0.01f)
            {
                aimDir.z = 0.01f;
            }

            if (float.IsNaN(aimDir.x) || float.IsNaN(aimDir.y) || float.IsNaN(aimDir.z))
            {
                return UnityEngine.Vector2.zero;
            }

            float? vFovNullable = _cameraController.GameplayCameraFov;
            if (!vFovNullable.HasValue)
            {
                return UnityEngine.Vector2.zero;
            }

            float vFov = vFovNullable.Value;
            float vFovRad = vFov * UnityEngine.Mathf.Deg2Rad;
            float tanHalfVFov = UnityEngine.Mathf.Tan(vFovRad / 2f);
            float tanHalfHFov = tanHalfVFov * cam.aspect;

            float screenWidth = UnityEngine.Screen.width;
            float screenHeight = UnityEngine.Screen.height;

            float normalizedX = aimDir.x / (aimDir.z * tanHalfHFov);
            float normalizedY = aimDir.y / (aimDir.z * tanHalfVFov);

            float offsetX = normalizedX * (screenWidth / 2f);
            float offsetY = normalizedY * (screenHeight / 2f);

            return new UnityEngine.Vector2(offsetX, offsetY);
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Gameplay && TrackingEnabled)
            {
                // Force recapture of base rotation when entering gameplay
                _cameraController.OnTrackingEnabled();
            }
            else if (newState != GameState.Gameplay)
            {
                // Leaving gameplay - reset camera state
                _cameraController.ResetState();
            }
        }

        private void OnSceneLoadedPatch()
        {
            _cameraController.ResetState();
        }

        private void OnCameraChangedPatch(UnityEngine.Camera newCamera)
        {
        }

    }
}
