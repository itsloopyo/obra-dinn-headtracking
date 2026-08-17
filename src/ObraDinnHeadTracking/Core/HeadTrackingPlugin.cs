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
        public const string PluginVersion = "1.2.1";

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

        /// <summary>
        /// The camera controller, exposed for Harmony patch access.
        /// </summary>
        public CameraController CameraController => _cameraController;

        // Harmony instance for patching
        private Harmony _harmony;

        // Components
        private ConfigManager _config;
        private OpenTrackReceiver _receiver;
        private TrackingProcessor _processor;
        private PoseInterpolator _interpolator;
        private PositionProcessor _positionProcessor;
        private PositionInterpolator _positionInterpolator;
        private CameraController _cameraController;
        private GameStateDetector _gameStateDetector;
        private InputHandler _inputHandler;
        private NotificationUI _notificationUI;
        private IMGUIReticle _aimReticle;
        private bool _reticleEnabled;
        private TrackingMode _trackingMode = TrackingMode.Normal;
        // Connection state tracking
        private bool _wasReceiving;

        private enum TrackingMode
        {
            Normal,
            RotationOnly,
            PositionOnly,
        }


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
            HeadMotionPatch.ApplyPatch(_harmony);

            // Initialize configuration (needed before framerate patch)
            _config = new ConfigManager();
            _config.Initialize(Config);

            // Apply framerate unlock patch if enabled
            FrameratePatch.ApplyPatch(_harmony, _config.UnlockFramerate.Value);

            // Initialize components
            _receiver = new OpenTrackReceiver();
            _processor = new TrackingProcessor
            {
                LocalSmoothing = _config.LocalSmoothing.Value,
                RemoteSmoothing = _config.RemoteSmoothing.Value,
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
            _positionProcessor = new PositionProcessor
            {
                Settings = PositionSettings.Symmetric(
                    _config.PositionSensitivityX.Value,
                    _config.PositionSensitivityY.Value,
                    _config.PositionSensitivityZ.Value,
                    _config.PositionLimitX.Value,
                    _config.PositionLimitY.Value,
                    _config.PositionLimitZ.Value,
                    _config.PositionLimitZBack.Value,
                    localSmoothing: _config.LocalSmoothing.Value,
                    remoteSmoothing: _config.RemoteSmoothing.Value,
                    invertX: true, invertY: false, invertZ: true
                ),
                TrackerPivotForward = _config.TrackerPivotForward.Value
            };
            _positionInterpolator = new PositionInterpolator();
            _cameraController = new CameraController(
                _receiver, _processor, _interpolator,
                _positionProcessor, _positionInterpolator);
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

            // Initialize position enabled from config
            _cameraController.PositionEnabled = _config.PositionEnabled.Value;

            // Subscribe to input events
            _inputHandler.OnTogglePressed += HandleToggle;
            _inputHandler.OnRecenterPressed += HandleRecenter;
            _inputHandler.OnToggleReticlePressed += HandleToggleReticle;
            _inputHandler.OnCycleTrackingModePressed += HandleCycleTrackingMode;

            // Subscribe to game state changes
            _gameStateDetector.StateChanged += OnGameStateChanged;
            _gameStateDetector.Initialize();

            // Subscribe to Harmony patch events
            CameraPatches.OnSceneLoaded += OnSceneLoadedPatch;
            CameraPatches.OnCameraChanged += OnCameraChangedPatch;

            // Start UDP receiver
            _receiver.Log = msg => Logger.LogInfo(msg);
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
                string keyInfo = $"[{_inputHandler.RecenterKey}] Recenter, [{_inputHandler.ToggleKey}] Toggle, [{_inputHandler.CycleTrackingModeKey}] Cycle Mode, [{_inputHandler.ToggleReticleKey}] Reticle";
                string statusInfo = TrackingEnabled ? "Head Tracking: ON" : "Head Tracking: OFF";
                _notificationUI.ShowNotification($"{statusInfo}\n{keyInfo}", 4f);
            }
        }

        private void Update()
        {
            if (_receiver.TryConsumeRecenterRequest())
            {
                HandleRecenter();
            }
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
            _inputHandler.OnCycleTrackingModePressed -= HandleCycleTrackingMode;
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
            _positionProcessor.SetCenter(_receiver.GetLatestPosition());
            _positionInterpolator.Reset();
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

        private void HandleCycleTrackingMode()
        {
            _trackingMode = (TrackingMode)(((int)_trackingMode + 1) % 3);

            switch (_trackingMode)
            {
                case TrackingMode.Normal:
                    _cameraController.RotationEnabled = true;
                    _cameraController.PositionEnabled = true;
                    _notificationUI.ShowNotification("Tracking: Rotation + Position", NotificationType.Success, 1.5f);
                    break;
                case TrackingMode.RotationOnly:
                    _cameraController.RotationEnabled = true;
                    _cameraController.PositionEnabled = false;
                    _notificationUI.ShowNotification("Tracking: Rotation only", NotificationType.Info, 1.5f);
                    break;
                case TrackingMode.PositionOnly:
                    _cameraController.RotationEnabled = false;
                    _cameraController.PositionEnabled = true;
                    _notificationUI.ShowNotification("Tracking: Position only", NotificationType.Info, 1.5f);
                    break;
            }
            Logger.LogInfo($"Tracking mode: {_trackingMode}");
        }

        /// <summary>
        /// Calculates the screen offset for the aim reticle based on current head tracking rotation.
        /// The reticle shows where you're aiming (mouse direction) vs where you're looking (head direction).
        /// With camera-local composition, gamePitch cancels out — the offset depends only on
        /// the head tracking rotation.
        /// </summary>
        private UnityEngine.Vector2 CalculateAimOffset()
        {
            var cam = _cameraController.GameplayCamera;
            if (cam == null)
            {
                return UnityEngine.Vector2.zero;
            }

            // Pitch is negated to match ApplyComposedRotation's Euler(-pitch, yaw, roll) convention.
            return UnityAimHelper.ComputeScreenOffsetFOV(
                _cameraController.LastTrackingYaw,
                -_cameraController.LastTrackingPitch,
                _cameraController.LastTrackingRoll,
                cam);
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
