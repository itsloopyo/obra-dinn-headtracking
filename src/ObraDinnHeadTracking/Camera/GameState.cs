namespace HeadTracking.Camera
{
    /// <summary>
    /// Enum representing detected game states for context-aware tracking activation.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// State cannot be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// In main menu or splash screens. Tracking disabled.
        /// </summary>
        MainMenu,

        /// <summary>
        /// Loading screen active. Tracking disabled.
        /// </summary>
        Loading,

        /// <summary>
        /// Active gameplay - tracking allowed.
        /// </summary>
        Gameplay,

        /// <summary>
        /// Game paused. Tracking disabled.
        /// </summary>
        Paused
    }
}
