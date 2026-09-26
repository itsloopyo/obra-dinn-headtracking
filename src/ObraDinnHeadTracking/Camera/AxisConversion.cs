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
    }
}
