using CameraUnlock.Core.Data;
using CameraUnlock.Core.Math;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Unity.Tracking;
using CameraUnlock.Core.Unity.Utilities;
using UnityEngine;

namespace HeadTracking.Camera
{
    /// <summary>
    /// Applies head tracking rotation to the game camera during gameplay.
    /// Mouse controls player body rotation, head tracking adds a local look offset on the camera.
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
        private float _lastGamePitch;
        private Vector3 _lastWorldUp = Vector3.up;
        private Vec3 _lastTrackingPosition;
        private bool _wasApplyingTracking;
        private bool _isTransitioningOut;
        private float _transitionProgress;

        // Transition-in state (for smooth resume after prop interactions or other interruptions)
        private bool _isTransitioningIn;
        private float _transitionInProgress;
        private const float TransitionInDurationSeconds = 0.5f;

        // Frame-level cache: avoids repeated reflection + GetComponent per frame
        private readonly PerFrameCache<UnityEngine.Camera> _gameplayCamera =
            new PerFrameCache<UnityEngine.Camera>(PlayerReflection.GetMainCamera);

        // Position offset is stored here and applied by HeadMotionPatch postfix,
        // which fires after HeadMotion.LateUpdate writes its final localPosition.
        private Vec3 _pendingPositionOffset;
        private bool _hasPendingPosition;

        // What the mod last wrote to the camera, read back after the write, and the game's own
        // value it was built on. A transform that still holds the write has not been touched by
        // the game since, so reading the game's value off it would take the head offset for the
        // game's and compound it every frame.
        private bool _hasWrittenRotation;
        private Quaternion _writtenLocalRotation;
        private bool _hasWrittenPosition;
        private Vector3 _writtenLocalPosition;
        private Vector3 _gameLocalPosition;

        // 6DOF auto-detection: latches true once we see non-zero position data
        private bool _detected6DOF;

        /// <summary>
        /// Whether positional tracking is enabled.
        /// </summary>
        public bool PositionEnabled { get; set; } = true;

        /// <summary>
        /// Whether rotational tracking is enabled.
        /// </summary>
        public bool RotationEnabled { get; set; } = true;

        /// <summary>
        /// true: head yaw turns the view about the world's up axis, so it stays level while the
        /// game pitches the camera. false: about the camera's own up axis, tilted with the pitch.
        /// </summary>
        public bool WorldSpaceYaw { get; set; } = true;

        /// <summary>
        /// Whether tracking is currently being applied.
        /// </summary>
        public bool IsApplyingTracking => _wasApplyingTracking && !_isTransitioningOut;

        /// <summary>
        /// Where the game aims, the camera's forward before head tracking, in the frame of the
        /// camera as last tracked: x right, y up, z forward.
        /// </summary>
        public Vector3 AimInTrackedView()
        {
            Quaternion tracked = Compose(_lastGamePitch, _lastWorldUp, _lastTrackingYaw, _lastTrackingPitch, _lastTrackingRoll);
            return Quaternion.Inverse(tracked) * (Quaternion.Euler(_lastGamePitch, 0f, 0f) * Vector3.forward);
        }

        /// <summary>
        /// Gets the resolved gameplay camera (Player.instance.mainCamera, accounts for zoom FOV).
        /// Returns null if not available. Cached per frame.
        /// </summary>
        public UnityEngine.Camera GameplayCamera => _gameplayCamera.Get();

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
        public void ApplyPendingPosition()
        {
            if (!_hasPendingPosition)
            {
                return;
            }

            // Apply to the gameplay camera transform (same one we apply rotation to),
            // NOT HeadMotion's transform - HeadMotion may not be in the camera's hierarchy.
            var cameraTransform = GetGameplayCameraTransform();
            if (cameraTransform == null)
            {
                return;
            }

            Vector3 current = cameraTransform.localPosition;
            if (!_hasWrittenPosition || !BitwiseEqual(current, _writtenLocalPosition))
            {
                _gameLocalPosition = current;
            }

            // Negative z is the forward lean throughout the pipeline and the asymmetric
            // clamp is built on that; Unity's transform +z is forward, so the flip belongs
            // here rather than in InvertZ, which inverts ahead of the clamp.
            cameraTransform.localPosition = _gameLocalPosition + new Vector3(
                _pendingPositionOffset.X, _pendingPositionOffset.Y, -_pendingPositionOffset.Z);
            _writtenLocalPosition = cameraTransform.localPosition;
            _hasWrittenPosition = true;

            _hasPendingPosition = false;
        }

        /// <summary>
        /// Process a frame of head tracking. Call from LateUpdate.
        /// </summary>
        /// <param name="enabled">Whether tracking should be active.</param>
        /// <returns>True if tracking was applied this frame.</returns>
        public bool ProcessFrame(bool enabled)
        {
            var cam = _gameplayCamera.Get();
            if (cam == null)
            {
                return false;
            }

            if (!ReferenceEquals(_camera, cam))
            {
                _camera = cam;
                _isTransitioningOut = false;
                _wasApplyingTracking = false;
                _hasWrittenRotation = false;
                _hasWrittenPosition = false;
            }

            if (enabled && _receiver.IsReceiving)
            {
                if (_isTransitioningOut)
                {
                    // Resume from the level the fade-out reached: the fade-out is linear and the
                    // fade-in quadratic, so this progress gives the same scale.
                    _isTransitioningOut = false;
                    _isTransitioningIn = true;
                    _transitionInProgress = Mathf.Sqrt(1f - Mathf.Clamp01(_transitionProgress));
                }
                else if (!_wasApplyingTracking)
                {
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
                    }
                    // Smooth ease-in curve
                    trackingScale = _transitionInProgress * _transitionInProgress;
                }

                ApplyTracking(cam.transform, trackingScale);
                _wasApplyingTracking = true;
                return true;
            }

            // Tracking disabled or no data
            if (_isTransitioningOut)
            {
                ProcessTransitionOut(cam.transform);
            }
            else if (_wasApplyingTracking)
            {
                // Was tracking, now stopped - start smooth transition out
                _isTransitioningOut = true;
                _transitionProgress = 0f;
                ProcessTransitionOut(cam.transform);
            }

            return false;
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
            _lastGamePitch = 0f;
            _lastWorldUp = Vector3.up;
            _lastTrackingPosition = Vec3.Zero;
            _pendingPositionOffset = Vec3.Zero;
            _hasPendingPosition = false;
            _hasWrittenRotation = false;
            _hasWrittenPosition = false;
            _detected6DOF = false;

            _processor.ResetSmoothing();
            _interpolator.Reset();
            _positionProcessor.ResetSmoothing();
            _positionInterpolator.Reset();
            _gameplayCamera.Invalidate();
        }

        /// <summary>
        /// Gets the real gameplay camera transform via Player.instance.mainCamera.
        /// Result is cached per frame to avoid repeated reflection.
        /// </summary>
        private Transform GetGameplayCameraTransform()
        {
            var cam = _gameplayCamera.Get();
            return cam != null ? cam.transform : null;
        }

        /// <summary>
        /// Composes tracking rotation with the game's pitch and applies it to the camera.
        /// Camera is a child of the player body (which provides yaw), so local rotation
        /// only contains the game's pitch.
        /// </summary>
        private void ApplyComposedRotation(Transform cameraTransform, float yaw, float pitch, float roll)
        {
            Quaternion local = cameraTransform.localRotation;
            if (!_hasWrittenRotation || !BitwiseEqual(local, _writtenLocalRotation))
            {
                _lastGamePitch = cameraTransform.localEulerAngles.x;
            }

            // World up in the parent's frame, read from the transform so the camera needs no
            // parent lookup: the parent's rotation is the world rotation without the local one.
            Quaternion parentRotation = cameraTransform.rotation * Quaternion.Inverse(local);
            _lastWorldUp = Quaternion.Inverse(parentRotation) * Vector3.up;

            cameraTransform.localRotation = Compose(_lastGamePitch, _lastWorldUp, yaw, pitch, roll);
            _writtenLocalRotation = cameraTransform.localRotation;
            _hasWrittenRotation = true;
        }

        // Exact comparison on purpose: Unity's == treats rotations within about 0.16 degrees as
        // equal, which would swallow slow mouse pitch.
        private static bool BitwiseEqual(Quaternion a, Quaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        private static bool BitwiseEqual(Vector3 a, Vector3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        /// <summary>
        /// The camera's local rotation for a game pitch and a head pose. World-locked yaw turns
        /// about <paramref name="worldUp"/> outside the game's pitch; camera-local yaw turns
        /// inside it, about the pitched camera's up. Head pitch and roll are camera-local in both.
        /// </summary>
        private Quaternion Compose(float gamePitchDeg, Vector3 worldUp, float yaw, float pitch, float roll)
        {
            Quaternion gamePitch = Quaternion.Euler(gamePitchDeg, 0f, 0f);
            if (WorldSpaceYaw)
            {
                return Quaternion.AngleAxis(yaw, worldUp) * gamePitch * Quaternion.Euler(-pitch, 0f, roll);
            }
            return gamePitch * Quaternion.Euler(-pitch, yaw, roll);
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
        /// <param name="cameraTransform">The gameplay camera, Player.instance.mainCamera, not Camera.main's post-processing camera.</param>
        /// <param name="scale">Scale factor for tracking (0-1), used for transition-in smoothing.</param>
        private void ApplyTracking(Transform cameraTransform, float scale)
        {
            // Connection locality picks LocalSmoothing vs RemoteSmoothing. Read it every
            // frame so switching between a local tracker and a phone on WiFi takes effect
            // without restarting the game.
            bool isRemoteConnection = _receiver.IsRemoteConnection;
            _processor.IsRemoteConnection = isRemoteConnection;
            _positionProcessor.IsRemoteConnection = isRemoteConnection;

            // Get raw tracking data, interpolate between samples, then process
            var rawPose = _receiver.GetLatestPose();
            var interpolated = _interpolator.Update(rawPose, Time.deltaTime);
            var processed = _processor.Process(interpolated, Time.deltaTime);

            float trackingYaw = RotationEnabled ? processed.Yaw * scale : 0f;
            float trackingPitch = RotationEnabled ? processed.Pitch * scale : 0f;
            float trackingRoll = RotationEnabled ? processed.Roll * scale : 0f;

            ApplyComposedRotation(cameraTransform, trackingYaw, trackingPitch, trackingRoll);

            // Position: auto-detect 6DOF vs 3DOF.
            // If raw position is non-zero, latch to 6DOF for the session.
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
                    finalPos = _positionProcessor.Process(interpolatedPos, physicalRotQ, Time.deltaTime);
                }
                else
                {
                    // 3DOF: no positional data available
                    finalPos = Vec3.Zero;
                }
            }
            else
            {
                finalPos = Vec3.Zero;
            }

            // Scale position by transition factor - don't apply directly, HeadMotionPatch
            // applies it after HeadMotion.LateUpdate writes the game's position.
            Vec3 scaledPos = finalPos * scale;
            _pendingPositionOffset = scaledPos;
            _hasPendingPosition = true;
            _lastTrackingPosition = scaledPos;

            // Store for fade-out transition
            _lastTrackingYaw = trackingYaw;
            _lastTrackingPitch = trackingPitch;
            _lastTrackingRoll = trackingRoll;
        }

        /// <summary>
        /// Smoothly fade out the tracking offset on camera only.
        /// </summary>
        private void ProcessTransitionOut(Transform cameraTransform)
        {
            // Unscaled, so a pause that sets timeScale to 0 still hands the view back.
            _transitionProgress += Time.unscaledDeltaTime / DisableTransitionDuration;

            if (_transitionProgress >= 1f)
            {
                // Transition complete - camera back on the game's pitch, no position offset
                _isTransitioningOut = false;
                _wasApplyingTracking = false;
                _pendingPositionOffset = Vec3.Zero;
                _hasPendingPosition = true;
                ApplyComposedRotation(cameraTransform, 0f, 0f, 0f);
                return;
            }

            // Fade tracking values toward zero using stored floats directly
            // (avoids Quaternion->euler decomposition round-trip)
            float fadedYaw = Mathf.Lerp(_lastTrackingYaw, 0f, _transitionProgress);
            float fadedPitch = Mathf.Lerp(_lastTrackingPitch, 0f, _transitionProgress);
            float fadedRoll = Mathf.Lerp(_lastTrackingRoll, 0f, _transitionProgress);

            // Queued for HeadMotionPatch, like ApplyTracking's offset
            Vec3 fadedPos = Vec3.Lerp(_lastTrackingPosition, Vec3.Zero, _transitionProgress);
            _pendingPositionOffset = fadedPos;
            _hasPendingPosition = true;

            // Apply faded tracking to camera using quaternion composition (matches ApplyTracking)
            ApplyComposedRotation(cameraTransform, fadedYaw, fadedPitch, fadedRoll);
        }
    }
}
