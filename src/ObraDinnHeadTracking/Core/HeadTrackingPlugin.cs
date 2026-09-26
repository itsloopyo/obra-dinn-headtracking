using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Rendering;
using CameraUnlock.Core.Unity.UI;
using HeadTracking.Camera;
using HeadTracking.Config;
using HeadTracking.Legacy;
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
        public const string PluginVersion = "1.3.0";

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
        private ObraDinnConfig _config;
        private ConfigOwner<ObraDinnConfig> _configOwner;
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
        private TrackingMode _trackingMode;
        // Connection state tracking
        private bool _wasReceiving;

        private const float ConfigNotificationSeconds = 8f;

        private const float AimProjectionEpsilon = 1e-6f;


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

            // Built before the config loads, so the owner's status sink can reach the player
            // when the file cannot be read, imported or created.
            _notificationUI = new NotificationUI();
            LoadConfig();

            // Apply framerate unlock patch if enabled
            FrameratePatch.ApplyPatch(_harmony, _config.UnlockFramerate);

            // Initialize components
            _receiver = new OpenTrackReceiver();
            _processor = new TrackingProcessor
            {
                LocalSmoothing = _config.LocalSmoothing,
                RemoteSmoothing = _config.RemoteSmoothing,
                Sensitivity = SensitivitySettings.Default,
                Deadzone = DeadzoneSettings.None
            };
            _interpolator = new PoseInterpolator();
            _positionProcessor = new PositionProcessor
            {
                Settings = new PositionSettings(
                    AxisConversion.PositionScale, AxisConversion.PositionScale, AxisConversion.PositionScale,
                    _config.Position.LimitX,
                    _config.Position.LimitY,
                    _config.Position.LimitYDown,
                    _config.Position.LimitZ,
                    _config.Position.LimitZBack,
                    _config.LocalSmoothing,
                    _config.RemoteSmoothing,
                    invertX: true, invertY: false, invertZ: false
                ),
                TrackerPivotForward = _config.TrackerPivotForward
            };
            _positionInterpolator = new PositionInterpolator();
            _cameraController = new CameraController(
                _receiver, _processor, _interpolator,
                _positionProcessor, _positionInterpolator);
            _cameraController.WorldSpaceYaw = _config.WorldSpaceYaw;
            _gameStateDetector = new GameStateDetector();
            _inputHandler = new InputHandler(_config, msg => Logger.LogWarning(msg));

            // Initialize aim reticle
            _aimReticle = gameObject.AddComponent<IMGUIReticle>();
            _aimReticle.Style = ReticleStyle.Dot;
            _aimReticle.BaseSizeAt1080p = 6;
            _aimReticle.OutlineWidthAt1080p = 2;
            _aimReticle.ReticleColor = UnityEngine.Color.white;
            _aimReticle.OutlineColor = UnityEngine.Color.black;
            _aimReticle.IsVisible = true;
            _aimReticle.InitializeWithOffset(
                getOffset: () => CalculateAimOffset(),
                shouldDraw: () => _gameStateDetector.IsGameplayActive && _cameraController.IsApplyingTracking
            );

            // The pair always names a mode: the table reads a pair that names none as its default.
            _trackingMode = TrackingModeChannels.Decode(_config.RotationEnabled, _config.PositionEnabled).Value;
            ApplyTrackingMode();

            // Subscribe to input events
            _inputHandler.OnTogglePressed += HandleToggle;
            _inputHandler.OnCycleTrackingModePressed += HandleCycleTrackingMode;
            _inputHandler.OnToggleYawModePressed += HandleToggleYawMode;

            // Subscribe to game state changes
            _gameStateDetector.StateChanged += OnGameStateChanged;
            _gameStateDetector.Initialize();

            // Subscribe to Harmony patch events
            CameraPatches.OnSceneLoaded += OnSceneLoadedPatch;
            CameraPatches.OnCameraChanged += OnCameraChangedPatch;

            // Start UDP receiver
            _receiver.Log = msg => Logger.LogInfo(msg);
            _receiver.Start(_config.UdpPort);

            // Set initial tracking state from config
            TrackingEnabled = _config.EnableOnStartup;

            Logger.LogInfo($"{PluginName} initialized. Tracking {(TrackingEnabled ? "enabled" : "disabled")}");

            if (!MouseLookPatches.PatchApplied)
                Logger.LogWarning("MouseLook patch FAILED - head tracking will NOT work");
            Logger.LogInfo($"Listening on UDP port {_config.UdpPort}");

            // A config the owner could not load or create has already put its message up, and the
            // startup toast would replace it.
            if (_config.ShowStartupNotification && !_notificationUI.IsDisplaying)
            {
                string keyInfo = $"[{_config.ToggleKeyName}] Toggle, [{_config.CycleTrackingModeKeyName}] Cycle Mode, [{_config.YawModeKeyName}] Yaw";
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
                // The log line is deliberately outside the notification gate: it is the
                // only evidence in the log that tracker packets ever arrived, and a
                // bug report must not depend on a cosmetic on-screen setting.
                Logger.LogInfo(isReceiving ? "OpenTrack connection established" : "OpenTrack connection lost");

                if (_config.ShowConnectionNotifications)
                {
                    if (isReceiving)
                        _notificationUI.ShowConnectionEstablished();
                    else
                        _notificationUI.ShowConnectionLost();
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
            _inputHandler.OnCycleTrackingModePressed -= HandleCycleTrackingMode;
            _inputHandler.OnToggleYawModePressed -= HandleToggleYawMode;
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

        private void HandleCycleTrackingMode()
        {
            _trackingMode = (TrackingMode)(((int)_trackingMode + 1) % 3);
            ApplyTrackingMode();

            switch (_trackingMode)
            {
                case TrackingMode.RotationAndPosition:
                    _notificationUI.ShowNotification("Tracking: Rotation + Position", NotificationType.Success, 1.5f);
                    break;
                case TrackingMode.RotationOnly:
                    _notificationUI.ShowNotification("Tracking: Rotation only", NotificationType.Info, 1.5f);
                    break;
                case TrackingMode.PositionOnly:
                    _notificationUI.ShowNotification("Tracking: Position only", NotificationType.Info, 1.5f);
                    break;
            }
            Logger.LogInfo($"Tracking mode: {_trackingMode.Description()}");

            bool rotation;
            bool position;
            TrackingModeChannels.Encode(_trackingMode, out rotation, out position);
            SaveConfig(c =>
            {
                c.RotationEnabled = rotation;
                c.PositionEnabled = position;
            });
        }

        private void HandleToggleYawMode()
        {
            _cameraController.WorldSpaceYaw = !_cameraController.WorldSpaceYaw;
            bool worldSpaceYaw = _cameraController.WorldSpaceYaw;
            _notificationUI.ShowNotification(worldSpaceYaw ? "Yaw: World-locked" : "Yaw: Camera-local",
                NotificationType.Info, 1.5f);
            Logger.LogInfo("Yaw mode: " + (worldSpaceYaw ? "world-locked" : "camera-local"));
            SaveConfig(c => c.WorldSpaceYaw = worldSpaceYaw);
        }

        private void ApplyTrackingMode()
        {
            bool rotation;
            bool position;
            TrackingModeChannels.Encode(_trackingMode, out rotation, out position);
            _cameraController.RotationEnabled = rotation;
            _cameraController.PositionEnabled = position;
        }

        /// <summary>
        /// The settings live in BepInEx\config\CameraUnlock.ini, read and written by core's config
        /// owner, with rows set to default following the player's Defaults.ini. Nothing is bound
        /// through BepInEx's ConfigFile at runtime, so ConfigurationManager does not list them.
        /// While CameraUnlock.ini is absent the owner imports the plugin's .cfg, the file every
        /// earlier build read, through the frozen v1.3.0 reader, and never writes that file.
        /// </summary>
        private void LoadConfig()
        {
            _configOwner = new ConfigOwner<ObraDinnConfig>(new ConfigOwnerOptions<ObraDinnConfig>
            {
                Path = ConfigPath,
                Table = ObraDinnConfig.Table(),
                Import = LegacyConfigImport.For(Config),
                LegacySourcePath = Config.ConfigFilePath,
                Header = new RenderHeader(ObraDinnConfig.DisplayName),
                Defaults = DefaultsFile.PerUser(),
                StatusSink = ShowConfigMessage
            });

            _loadMessages = string.Empty;
            ConfigLoadResult<ObraDinnConfig> loaded = _configOwner.Load();
            _loadMessages = null;
            _config = loaded.Config;

            // The owner writes each diagnostic as "<path>: <description>" among lines that only
            // report what it did, so the complaints are picked out by their text.
            var complaints = new HashSet<string>();
            foreach (CanonicalDiagnostic diagnostic in loaded.Diagnostics)
                complaints.Add(ConfigPath + ": " + diagnostic.Describe());
            bool usable = loaded.Status == ConfigLoadStatus.Canonical
                          || loaded.Status == ConfigLoadStatus.Migrated
                          || loaded.Status == ConfigLoadStatus.Created;
            foreach (string line in loaded.Log)
            {
                if (usable && !complaints.Contains(line)) Logger.LogInfo(line);
                else Logger.LogWarning(line);
            }
            Logger.LogInfo("Config " + ConfigPath + ": " + loaded.Status);
        }

        private static string ConfigPath
        {
            get { return Path.Combine(Paths.ConfigPath, "CameraUnlock.ini"); }
        }

        // Non-null while Load runs. Load can hand the sink two messages, the config file's and
        // then one about Defaults.ini, and the notification shows one message at a time, so the
        // second is shown beneath the first rather than in its place.
        private string _loadMessages;

        private void ShowConfigMessage(string message)
        {
            if (_loadMessages != null)
            {
                _loadMessages = _loadMessages.Length == 0 ? message : _loadMessages + "\n" + message;
                message = _loadMessages;
            }
            _notificationUI.ShowNotification(message, NotificationType.Warning, ConfigNotificationSeconds);
        }

        /// <summary>
        /// Called after the new value is already applied. A save that fails is logged, the owner
        /// shows the player why, and the session keeps the new value.
        /// </summary>
        private void SaveConfig(Action<ObraDinnConfig> change)
        {
            ConfigSaveResult saved = _configOwner.Save(change);
            if (saved.Status == ConfigSaveStatus.Saved)
            {
                // A row that held default and now holds a value, so it stops following
                // Defaults.ini in this game.
                foreach (string line in saved.Log) Logger.LogInfo(line);
                return;
            }
            foreach (string line in saved.Log) Logger.LogWarning(line);
            Logger.LogWarning(ConfigPath + ": " + saved.Status + ": " + saved.Reason
                              + " The change applies to this session only.");
        }

        /// <summary>
        /// Calculates the screen offset for the aim reticle based on current head tracking rotation.
        /// The reticle shows where you're aiming (mouse direction) vs where you're looking (head direction).
        /// The aim is projected through the tracked camera's field of view, as ScreenOffsetCalculator
        /// projects it. With world-locked yaw the offset depends on the game's pitch as well, which
        /// the head-pose angles alone cannot give.
        /// </summary>
        private UnityEngine.Vector2 CalculateAimOffset()
        {
            var cam = _cameraController.GameplayCamera;
            if (cam == null)
            {
                return UnityEngine.Vector2.zero;
            }

            UnityEngine.Vector3 aim = _cameraController.AimInTrackedView();
            float tanHalfFovY = UnityEngine.Mathf.Tan(cam.fieldOfView * 0.5f * UnityEngine.Mathf.Deg2Rad);
            float tanHalfFovX = tanHalfFovY * cam.aspect;
            // Behind the view plane the divide mirrors the offset, so the dot stays centred.
            if (aim.z < AimProjectionEpsilon || tanHalfFovY < AimProjectionEpsilon || tanHalfFovX < AimProjectionEpsilon)
            {
                return UnityEngine.Vector2.zero;
            }
            return new UnityEngine.Vector2(
                aim.x / aim.z / tanHalfFovX * UnityEngine.Screen.width * 0.5f,
                aim.y / aim.z / tanHalfFovY * UnityEngine.Screen.height * 0.5f);
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
