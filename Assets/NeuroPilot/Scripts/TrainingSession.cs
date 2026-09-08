namespace NeuroPilotXR.Navigation
{
    public enum TrainingMode { EyeTracking, SingleTarget, MultiTarget }
    public static class TrainingSession
    {
        public static DifficultyLevel SelectedDifficulty { get; private set; } = DifficultyLevel.Standard;
        public static TrainingMode SelectedMode = TrainingMode.SingleTarget;
        public static int TargetCount = 4;
        // Retained for the multi-target rule buttons in existing saved scenes.
        public static bool MultiColorGaze;
        public static bool EyeColorGaze;
        public static int EyeTargetCount = 4;
        public static bool ReturnToModes;

        public static void SetDifficulty(DifficultyLevel difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }
}
