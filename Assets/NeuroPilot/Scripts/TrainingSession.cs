namespace NeuroPilotXR.Navigation
{
    public static class TrainingSession
    {
        public static DifficultyLevel SelectedDifficulty { get; private set; } = DifficultyLevel.Standard;

        public static void SetDifficulty(DifficultyLevel difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }
}