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
        public const string PluginVersion = "1.0.4";

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
            _positionProcessor = new PositionProcessor
            {
                Settings = new PositionSettings(
                    _config.PositionSensitivityX.Value,
                    _config.PositionSensitivityY.Value,
                    _config.PositionSensitivityZ.Value,
                    _config.PositionLimitX.Value,
                    _config.PositionLimitY.Value,
                    _config.PositionLimitZ.Value,
                    _config.PositionSmoothing.Value,
                    invertX: true, invertY: false, invertZ: true
                ),
                NeckModelSettings = NeckModelSettings.Disabled,
                TrackerPivotForward = _config.TrackerPivotForward.Value
            };
            _positionInterpolator = new PositionInterpolator();
            _cameraController = new CameraController(
                _receiver, _processor, _interpolator,
                _positionProcessor, _positionInterpolator);
            _cameraController.NeckModelSettings = new NeckModelSettings(
                _config.NeckModelEnabled.Value,
                _config.NeckModelHeight.Value,
                _config.NeckModelForward.Value
            );
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
            _inputHandler.OnTogglePositionPressed += HandleTogglePosition;

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
                string keyInfo = $"[{_inputHandler.ToggleKey}] Toggle, [{_inputHandler.RecenterKey}] Recenter, [{_inputHandler.ToggleReticleKey}] Reticle, [{_inputHandler.TogglePositionKey}] Position";
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
            _inputHandler.OnTogglePositionPressed -= HandleTogglePosition;
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

        private void HandleTogglePosition()
        {
            _cameraController.PositionEnabled = !_cameraController.PositionEnabled;

            if (_cameraController.PositionEnabled)
            {
                _notificationUI.ShowNotification("Position: ON", NotificationType.Success, 1.5f);
            }
            else
            {
                _notificationUI.ShowNotification("Position: OFF", NotificationType.Warning, 1.5f);
            }
            Logger.LogInfo($"Position tracking {(_cameraController.PositionEnabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Calculates the screen offset for the aim reticle based on current head tracking rotation.
        /// The reticle shows where you're aiming (mouse direction) vs where you're looking (head direction).
        /// Mirrors ApplyComposedRotation exactly (horizon-locked yaw around world Y, matching DL2's
        /// crosshair_projection.h), then projects the body aim direction into the tracked camera frame.
        /// </summary>
        private UnityEngine.Vector2 CalculateAimOffset()
        {
            var cam = CameraPatches.CachedMainCamera;
            if (cam == null)
            {
                return UnityEngine.Vector2.zero;
            }

            float yaw = _cameraController.LastTrackingYaw;
            float pitch = _cameraController.LastTrackingPitch;
            float roll = _cameraController.LastTrackingRoll;
            float gamePitch = _cameraController.GameCameraPitch;

            float? vFovNullable = _cameraController.GameplayCameraFov;
            if (!vFovNullable.HasValue)
            {
                return UnityEngine.Vector2.zero;
            }

            float vFov = vFovNullable.Value;
            float tanHalfVFov = UnityEngine.Mathf.Tan(vFov * UnityEngine.Mathf.Deg2Rad * 0.5f);
            float tanHalfHFov = tanHalfVFov * cam.aspect;

            float halfWidth = UnityEngine.Screen.width * 0.5f;
            float halfHeight = UnityEngine.Screen.height * 0.5f;

            // Mirror ApplyComposedRotation exactly to compute where body aim
            // (original forward before tracking) appears in the tracked camera view.
            var gamePitchQ = UnityEngine.Quaternion.Euler(gamePitch, 0f, 0f);
            UnityEngine.Vector3 origFwd = gamePitchQ * UnityEngine.Vector3.forward;
            UnityEngine.Vector3 fwd = origFwd;
            UnityEngine.Vector3 up = gamePitchQ * UnityEngine.Vector3.up;

            // 1. Yaw around world Y (horizon-locked)
            if (UnityEngine.Mathf.Abs(yaw) > 0.001f)
            {
                var yawQ = UnityEngine.Quaternion.AngleAxis(yaw, UnityEngine.Vector3.up);
                fwd = yawQ * fwd;
                up = yawQ * up;
            }

            // 2. Pitch around camera right
            if (UnityEngine.Mathf.Abs(pitch) > 0.001f)
            {
                UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(up, fwd).normalized;
                fwd = UnityEngine.Quaternion.AngleAxis(-pitch, right) * fwd;
            }

            // 3. Re-derive up
            float dot = UnityEngine.Vector3.Dot(fwd, up);
            UnityEngine.Vector3 newUp = (up - fwd * dot).normalized;

            // 4. Roll
            if (UnityEngine.Mathf.Abs(roll) > 0.001f)
            {
                newUp = UnityEngine.Quaternion.AngleAxis(roll, fwd) * newUp;
            }

            // Project body aim direction into tracked camera frame
            UnityEngine.Vector3 newRight = UnityEngine.Vector3.Cross(newUp, fwd);

            float bDepth = UnityEngine.Vector3.Dot(origFwd, fwd);
            float bRight = UnityEngine.Vector3.Dot(origFwd, newRight);
            float bUp = UnityEngine.Vector3.Dot(origFwd, newUp);

            if (bDepth < 0.01f) bDepth = 0.01f;

            float offsetX = (bRight / bDepth) / tanHalfHFov * halfWidth;
            float offsetY = (bUp / bDepth) / tanHalfVFov * halfHeight;

            if (float.IsNaN(offsetX) || float.IsNaN(offsetY))
            {
                return UnityEngine.Vector2.zero;
            }

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
