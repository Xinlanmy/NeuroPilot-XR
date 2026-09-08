using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class DifficultySelector : MonoBehaviour
    {
        [SerializeField] private DifficultyCardView[] cards;

        public DifficultyLevel CurrentDifficulty { get; private set; } = DifficultyLevel.Standard;

        public void Configure(DifficultyCardView[] cardViews)
        {
            cards = cardViews;
        }

        private void Awake()
        {
            Select(TrainingSession.SelectedDifficulty);
        }

        public void Select(DifficultyLevel difficulty)
        {
            CurrentDifficulty = difficulty;
            TrainingSession.SetDifficulty(difficulty);

            if (cards == null)
                return;

            foreach (DifficultyCardView card in cards)
            {
                if (card != null)
                    card.SetSelected(card.Level == difficulty);
            }
        }
    }
}
