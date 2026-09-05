using TMPro;
using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 训练 HUD：剩余时间 / 命中漏失统计 / 提示语 / 结算面板。
    /// World Space Canvas 放在被试面前约 2m（Bootstrapper 摆放），避免抢 SSVEP 视觉注意。
    /// </summary>
    public sealed class TrainingHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text summaryText;

        public void UpdateStatus(float remainingSeconds, int hits, int misses)
        {
            if (timeText != null)
            {
                int total = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
                timeText.text = $"{(total / 60):D2}:{(total % 60):D2}";
            }

            if (statsText != null)
            {
                statsText.text = $"命中 {hits}    漏失 {misses}";
            }
        }

        public void SetPrompt(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }
        }

        public void ShowSummary(int hits, int misses, float hitRate, float avgReactionSeconds)
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }

            if (summaryText != null)
            {
                summaryText.text =
                    "训练结束\n\n" +
                    $"命中 {hits}    漏失 {misses}\n" +
                    $"命中率 {hitRate:P0}    平均反应 {avgReactionSeconds:F2}s\n\n" +
                    "按 R 键重新开始";
            }
        }

        public void HideSummary()
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }
        }
    }
}
