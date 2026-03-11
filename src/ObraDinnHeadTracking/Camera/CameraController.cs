using CameraUnlock.Core.Data;
using CameraUnlock.Core.Math;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using HeadTracking.Patches;
using UnityEngine;

namespace HeadTracking.Camera
{
    /// <summary>
    /// Applies head tracking rotation to the game camera during gameplay.
    /// Mouse controls player body rotation, head tracking adds a local look offset on the camera.
    /// Works in conjunction with MouseLook Harmony patch for proper timing.
    /// </summary>
    public class CameraController
    {
        private const float DisableTransitionDuration = 0.3f;
        private readonly OpenTrackReceiver _receiver;
        private readonly TrackingProcessor _processor;
        private readonly PoseInterpolator _interpolator;
        private readonly PositionProcessor _positionProcessor;
        private readonly PositionInterpolator _positionInterpolator;

        private UnityEngine.Camera _camera;
        private float _lastTrackingYaw;
        private float _lastTrackingPitch;
        private float _lastTrackingRoll;
        private Vec3 _lastTrackingPosition;
        private bool _wasApplyingTracking;
        private bool _isTransitioningOut;
        private float _transitionProgress;

        // Transition-in state (for smooth resume after prop interactions or other interruptions)
        private bool _isTransitioningIn;
        private float _transitionInProgress;
        private const float TransitionInDurationSeconds = 0.5f;

        // Frame-level cache: avoids repeated reflection + GetComponent per frame
        private int _resolvedFrame = -1;
        private Transform _resolvedTransform;
        private UnityEngine.Camera _resolvedCamera;

        // Position offset is stored here and applied by HeadMotionPatch postfix,
        // which fires after HeadMotion.LateUpdate writes its final localPosition.
        private Vec3 _pendingPositionOffset;
        private bool _hasPendingPosition;

        // 6DOF auto-detection: latches true once we see non-zero position data
        private bool _detected6DOF;

        // Auto-recenter on first valid tracking frame
        private bool _hasCentered;

        /// <summary>
        /// Whether positional tracking (lean/neck model) is enabled.
        /// </summary>
        public bool PositionEnabled { get; set; } = true;

        /// <summary>
        /// Neck model settings for rotation-only positional parallax.
        /// </summary>
        public NeckModelSettings NeckModelSettings { get; set; } = NeckModelSettings.Default;

        /// <summary>
        /// Whether tracking is currently being applied.
        /// </summary>
        public bool IsApplyingTracking => _wasApplyingTracking && !_isTransitioningOut;

        /// <summary>The last applied tracking yaw in degrees.</summary>
        public float LastTrackingYaw => _lastTrackingYaw;

        /// <summary>The last applied tracking pitch in degrees.</summary>
        public float LastTrackingPitch => _lastTrackingPitch;

        /// <summary>The last applied tracking roll in degrees.</summary>
        public float LastTrackingRoll => _lastTrackingRoll;

        /// <summary>
        /// The game camera pitch (from mouse) before head tracking is applied, in ±180° range.
        /// </summary>
        public float GameCameraPitch { get; private set; }

        /// <summary>
        /// Gets the gameplay camera's current FOV (accounts for zoom).
        /// Returns null if camera is not available.
        /// </summary>
        public float? GameplayCameraFov
        {
            get
            {
                ResolveGameplayCamera();
                return _resolvedCamera?.fieldOfView;
            }
        }

        public CameraController(
            OpenTrackReceiver receiver, TrackingProcessor processor, PoseInterpolator interpolator,
            PositionProcessor positionProcessor, PositionInterpolator positionInterpolator)
        {
            _receiver = receiver;
            _processor = processor;
            _interpolator = interpolator;
            _positionProcessor = positionProcessor;
            _positionInterpolator = positionInterpolator;
        }

        /// <summary>
        /// Called by HeadMotionPatch postfix after HeadMotion.LateUpdate writes its final localPosition.
        /// Adds our tracking position offset on top of the game's head bob / wave / crouch offsets.
        /// The offset is in body-local space (tracker axes align with parent's local axes).
        /// </summary>
        public void ApplyPendingPosition(Transform headMotionTransform)
        {
            if (!_hasPendingPosition)
            {
                return;
            }

            // Apply to the gameplay camera transform (same one we apply rotation to),
            // NOT HeadMotion's transform — HeadMotion may not be in the camera's hierarchy.
            var cameraTransform = GetGameplayCameraTransform();
            if (cameraTransform == null)
            {
                return;
            }

            var gamePos = cameraTransform.localPosition;
            cameraTransform.localPosition = gamePos + new Vector3(
                _pendingPositionOffset.X, _pendingPositionOffset.Y, _pendingPositionOffset.Z);

            _hasPendingPosition = false;
        }

        /// <summary>
        /// Process a frame of head tracking. Call from LateUpdate.
        /// </summary>
        /// <param name="enabled">Whether tracking should be active.</param>
        /// <returns>True if tracking was applied this frame.</returns>
        public bool ProcessFrame(bool enabled)
        {
            // Use cached camera or get fresh
            var cam = CameraPatches.CachedMainCamera ?? UnityEngine.Camera.main;
            if (cam == null)
            {
                return false;
            }

            // Detect camera change (scene transition)
            if (_camera != cam)
            {
                _camera = cam;
                _isTransitioningOut = false;
                _wasApplyingTracking = false;
            }

            if (enabled && _receiver.IsReceiving)
            {
                _isTransitioningOut = false;

                // Detect transition from not-tracking to tracking
                if (!_wasApplyingTracking)
                {
                    // Auto-recenter on first valid tracking frame
                    if (!_hasCentered)
                    {
                        _hasCentered = true;
                        var rawPose = _receiver.GetLatestPose();
                        _processor.RecenterTo(rawPose);
                        _positionProcessor.SetCenter(_receiver.GetLatestPosition());
                    }

                    // Starting to track - begin transition in
                    _isTransitioningIn = true;
                    _transitionInProgress = 0f;
                    _detected6DOF = false;
                    _interpolator.Reset();
                    _processor.ResetSmoothing();
                    _positionInterpolator.Reset();
                    _positionProcessor.ResetSmoothing();
                }

                // Apply tracking with transition-in scaling if needed
                float trackingScale = 1f;
                if (_isTransitioningIn)
                {
                    _transitionInProgress += Time.deltaTime / TransitionInDurationSeconds;
                    if (_transitionInProgress >= 1f)
                    {
                        _transitionInProgress = 1f;
                        _isTransitioningIn = false;

                        // Re-recenter with stabilized tracker values.
                        // Initial auto-recenter may capture unstabilized face tracker data
                        // (warm-up Y drift). After 0.5s the tracker has settled.
                        if (_receiver.IsReceiving)
                        {
                            var rawPose = _receiver.GetLatestPose();
                            _processor.RecenterTo(rawPose);
                            _positionProcessor.SetCenter(_receiver.GetLatestPosition());
                        }
                    }
                    // Smooth ease-in curve
                    trackingScale = _transitionInProgress * _transitionInProgress;
                }

                ApplyTracking(trackingScale);
                _wasApplyingTracking = true;
                return true;
            }

            // Tracking disabled or no data
            if (_isTransitioningOut)
            {
                ProcessTransitionOut();
            }
            else if (_wasApplyingTracking)
            {
                // Was tracking, now stopped - start smooth transition out
                StartTransitionOut();
                ProcessTransitionOut();
            }

            return false;
        }

        /// <summary>
        /// Called when tracking is enabled.
        /// </summary>
        public void OnTrackingEnabled()
        {
            _processor.ResetSmoothing();
            _interpolator.Reset();
            _positionProcessor.ResetSmoothing();
            _positionInterpolator.Reset();
            _isTransitioningOut = false;
            // Don't reset tracking values - allows smooth re-enable if head hasn't moved
        }

        /// <summary>
        /// Called when tracking is disabled.
        /// </summary>
        public void OnTrackingDisabled()
        {
            if (_wasApplyingTracking)
            {
                StartTransitionOut();
            }
        }

        /// <summary>
        /// Reset all camera state. Called on scene transitions.
        /// </summary>
        public void ResetState()
        {
            _camera = null;
            _isTransitioningOut = false;
            _isTransitioningIn = false;
            _transitionInProgress = 0f;
            _wasApplyingTracking = false;
            _lastTrackingYaw = 0f;
            _lastTrackingPitch = 0f;
            _lastTrackingRoll = 0f;
            _lastTrackingPosition = Vec3.Zero;
            _pendingPositionOffset = Vec3.Zero;
            _hasPendingPosition = false;
            _detected6DOF = false;
            _hasCentered = false;
            _processor.ResetSmoothing();
            _interpolator.Reset();
            _positionProcessor.ResetSmoothing();
            _positionInterpolator.Reset();
            _resolvedFrame = -1;
        }

        /// <summary>
        /// Gets the real gameplay camera transform via Player.instance.mainCamera.
        /// Result is cached per frame to avoid repeated reflection.
        /// </summary>
        private Transform GetGameplayCameraTransform()
        {
            ResolveGameplayCamera();
            return _resolvedTransform;
        }

        /// <summary>
        /// Resolves Player.instance.mainCamera via reflection, cached per frame.
        /// Populates _resolvedTransform and _resolvedCamera.
        /// </summary>
        private void ResolveGameplayCamera()
        {
            int frame = Time.frameCount;
            if (_resolvedFrame == frame)
            {
                return;
            }
            _resolvedFrame = frame;
            _resolvedTransform = null;
            _resolvedCamera = null;

            _resolvedCamera = PlayerReflection.GetMainCamera();
            _resolvedTransform = _resolvedCamera?.transform;
        }

        /// <summary>
        /// Composes tracking rotation with the game's pitch and applies it to the camera.
        /// Uses axis-rotation sequence matching DL2/Green Hell:
        ///   1. Yaw around world Y (horizon-locked — pure horizontal motion regardless of game pitch)
        ///   2. Pitch around camera right
        ///   3. Re-derive up perpendicular to new forward (prevents coupled roll artifacts)
        ///   4. Roll around forward
        /// </summary>
        private static void ApplyComposedRotation(
            Transform cameraTransform, float gamePitchDeg,
            float yaw, float pitch, float roll)
        {
            var gamePitchQ = Quaternion.Euler(gamePitchDeg, 0f, 0f);

            Vector3 fwd = gamePitchQ * Vector3.forward;
            Vector3 up = gamePitchQ * Vector3.up;

            // 1. Yaw: rotate fwd and up around world Y (horizon-locked)
            if (Mathf.Abs(yaw) > 0.001f)
            {
                var yawQ = Quaternion.AngleAxis(yaw, Vector3.up);
                fwd = yawQ * fwd;
                up = yawQ * up;
            }

            // 2. Pitch: rotate fwd around camera right
            if (Mathf.Abs(pitch) > 0.001f)
            {
                Vector3 right = Vector3.Cross(up, fwd).normalized;
                fwd = Quaternion.AngleAxis(-pitch, right) * fwd;
            }

            // 3. Re-derive up perpendicular to new forward
            float dot = Vector3.Dot(fwd, up);
            Vector3 newUp = (up - fwd * dot).normalized;

            // 4. Roll: rotate up around forward
            if (Mathf.Abs(roll) > 0.001f)
            {
                newUp = Quaternion.AngleAxis(roll, fwd) * newUp;
            }

            cameraTransform.localRotation = Quaternion.LookRotation(fwd, newUp);
        }

        /// <summary>
        /// Apply head tracking offset on top of the current aim rotation.
        ///
        /// SIMPLE APPROACH: Don't touch player body at all. Only modify camera.
        /// - Player body rotation is controlled purely by MouseLook (mouse input)
        /// - Movement system reads player body rotation = pure mouse aim
        /// - Camera is a child of player body, so it inherits parent yaw
        /// - We add tracking as LOCAL rotation on camera = look around without affecting movement
        /// </summary>
        /// <param name="scale">Scale factor for tracking (0-1), used for transition-in smoothing.</param>
        private void ApplyTracking(float scale = 1f)
        {
            // Get the REAL gameplay camera, not the post-processing camera
            // Camera.main returns "Post Camera" which is wrong - we need Player.instance.mainCamera
            var cameraTransform = GetGameplayCameraTransform();
            if (cameraTransform == null)
            {
                return;
            }

            // Get current local rotation (this has the game's pitch from cameraMouseLook)
            var currentLocal = cameraTransform.localEulerAngles;

            // Capture game camera pitch in ±180° range for reticle offset calculation
            float gamePitchRaw = currentLocal.x;
            GameCameraPitch = gamePitchRaw > 180f ? gamePitchRaw - 360f : gamePitchRaw;

            // Get raw tracking data, interpolate between samples, then process
            var rawPose = _receiver.GetLatestPose();
            var interpolated = _interpolator.Update(rawPose, Time.deltaTime);
            bool isRemote = _receiver.IsRemoteConnection;
            var processed = _processor.Process(interpolated, isRemote, Time.deltaTime);

            float trackingYaw = processed.Yaw * scale;
            float trackingPitch = processed.Pitch * scale;
            float trackingRoll = processed.Roll * scale;

            ApplyComposedRotation(cameraTransform, currentLocal.x, trackingYaw, trackingPitch, trackingRoll);

            // Position: auto-detect 6DOF vs 3DOF.
            // If raw position is non-zero, latch to 6DOF for the session.
            // Otherwise fall back to neck model for rotation-only parallax.
            Vec3 finalPos;
            if (PositionEnabled)
            {
                var rawPos = _receiver.GetLatestPosition();

                // Latch: once we see any non-zero position, stay in 6DOF mode
                if (!_detected6DOF && (rawPos.X != 0f || rawPos.Y != 0f || rawPos.Z != 0f))
                {
                    _detected6DOF = true;
                }

                if (_detected6DOF)
                {
                    var interpolatedPos = _positionInterpolator.Update(rawPos, Time.deltaTime);

                    // Physical rotation for pivot compensation inside PositionProcessor
                    var physicalRotQ = QuaternionUtils.FromYawPitchRoll(
                        interpolated.Yaw, -interpolated.Pitch, interpolated.Roll);
                    finalPos = _positionProcessor.Process(interpolatedPos, physicalRotQ, isRemote, Time.deltaTime);
                }
                else
                {
                    // 3DOF: neck model provides positional parallax from rotation
                    var headRotQ = QuaternionUtils.FromYawPitchRoll(trackingYaw, -trackingPitch, trackingRoll);
                    finalPos = NeckModel.ComputeOffset(headRotQ, NeckModelSettings);
                }
            }
            else
            {
                finalPos = Vec3.Zero;
            }

            {
                // Scale position by transition factor — don't apply directly,
                // store for onPreCull which fires after all game LateUpdates
                Vec3 scaledPos = finalPos * scale;
                _pendingPositionOffset = scaledPos;
                _hasPendingPosition = true;
                _lastTrackingPosition = scaledPos;
            }

            // Store for fade-out transition
            _lastTrackingYaw = trackingYaw;
            _lastTrackingPitch = trackingPitch;
            _lastTrackingRoll = trackingRoll;
        }

        private void StartTransitionOut()
        {
            _isTransitioningOut = true;
            _transitionProgress = 0f;
        }

        /// <summary>
        /// Smoothly fade out the tracking offset on camera only.
        /// </summary>
        private void ProcessTransitionOut()
        {
            _transitionProgress += Time.deltaTime / DisableTransitionDuration;

            var cameraTransform = GetGameplayCameraTransform();
            if (cameraTransform == null)
            {
                _isTransitioningOut = false;
                _wasApplyingTracking = false;
                return;
            }

            if (_transitionProgress >= 1f)
            {
                // Transition complete - reset camera local yaw/roll to zero, no position offset
                _isTransitioningOut = false;
                _wasApplyingTracking = false;
                _hasPendingPosition = false;
                var euler = cameraTransform.localEulerAngles;
                cameraTransform.localEulerAngles = new Vector3(euler.x, 0f, 0f);
                return;
            }

            // Fade tracking values toward zero using stored floats directly
            // (avoids Quaternion→euler decomposition round-trip)
            float fadedYaw = Mathf.Lerp(_lastTrackingYaw, 0f, _transitionProgress);
            float fadedPitch = Mathf.Lerp(_lastTrackingPitch, 0f, _transitionProgress);
            float fadedRoll = Mathf.Lerp(_lastTrackingRoll, 0f, _transitionProgress);

            // Queue faded position for onPreCull application
            Vec3 fadedPos = Vec3.Lerp(_lastTrackingPosition, Vec3.Zero, _transitionProgress);
            _pendingPositionOffset = fadedPos;
            _hasPendingPosition = true;

            // Apply faded tracking to camera using quaternion composition (matches ApplyTracking)
            var currentLocal = cameraTransform.localEulerAngles;
            ApplyComposedRotation(cameraTransform, currentLocal.x, fadedYaw, fadedPitch, fadedRoll);
        }
    }
}
