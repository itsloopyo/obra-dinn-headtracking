namespace HeadTracking.Camera
{
    /// <summary>
    /// The tracker-to-game conversion. Unity-free, so the test project compiles it and holds the
    /// converted startup to it.
    /// </summary>
    internal static class AxisConversion
    {
        /// <summary>
        /// Game units per metre of head movement. Every published build shipped
        /// PositionSensitivityX/Y/Z = 2.0 and applied it on the way in, so a default lean moves the
        /// view as far as it always did.
        /// </summary>
        public const float PositionScale = 2.0f;

        /// <summary>
        /// Metres from the neck pivot forward to the point the tracker follows. Every published
        /// build shipped TrackerPivotForward = 0.08, and the pivot is not a setting now.
        /// </summary>
        public const float TrackerPivotForward = 0.08f;
    }
}
