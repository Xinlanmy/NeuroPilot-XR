using TMPro;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class TrainingScenePresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text difficultyText;

        public void Configure(TMP_Text label)
        {
            difficultyText = label;
        }

        private void Start()
        {
            if (difficultyText == null)
                return;

            switch (TrainingSession.SelectedDifficulty)
            {
                case DifficultyLevel.Beginner:
                    difficultyText.text = "当前训练强度：轻度";
                    break;
                case DifficultyLevel.Advanced:
                    difficultyText.text = "当前训练强度：挑战";
                    break;
                default:
                    difficultyText.text = "当前训练强度：标准";
                    break;
            }
        }
    }
}