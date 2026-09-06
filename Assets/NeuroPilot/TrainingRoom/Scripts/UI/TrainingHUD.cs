using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 训练场 HUD（World Space，贴在面前 2m 处，不抢 SSVEP 视觉注意）。
/// 顶部中央：剩余时间大字；左上：命中/漏失；中央下方：当前提示语。
/// 结束后显示结算面板。
/// </summary>
public class TrainingHUD : MonoBehaviour
{
    [Header("运行时引用(由场景构建器注入)")]
    public SessionManager session;

    [Header("UI 控件")]
    public Text timeText;
    public Text statText;
    public Text hintText;
    public GameObject resultPanel;
    public Text resultText;
    public Text modeText;

    private float _roundDuration;

    public void ResetForRound(float duration)
    {
        SetSessionLabelsVisible(true);
        _roundDuration = duration;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintText != null) hintText.text = "";
        UpdateTimeText(duration);
        if (statText != null) statText.text = "命中 0　漏失 0";
        if (modeText != null)
        {
            var difficulty = NeuroPilotXR.Navigation.TrainingSession.SelectedDifficulty;
            string label = difficulty == NeuroPilotXR.Navigation.DifficultyLevel.Beginner ? "轻度" :
                difficulty == NeuroPilotXR.Navigation.DifficultyLevel.Advanced ? "挑战" : "标准";
            modeText.text = "TrainingRoom v1.1.1  ·  训练强度：" + label + "  ·  扳机 / 空格：模拟命中";
        }
    }

    public void SetHint(string s)
    {
        if (hintText != null) hintText.text = s;
    }

    public void OnHit()
    {
        if (statText != null && session != null)
            statText.text = $"命中 {session.HitCount}　漏失 {session.MissCount}";
    }

    public void OnMiss()
    {
        if (statText != null && session != null)
            statText.text = $"命中 {session.HitCount}　漏失 {session.MissCount}";
    }

    public void ShowResult(int hit, int miss, float rate, float avgReaction)
    {
        SetSessionLabelsVisible(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
        {
            resultText.text =
                $"—— 本轮结束 ——\n\n" +
                $"命中　{hit} 次\n" +
                $"漏失　{miss} 次\n" +
                $"命中率　{rate:F1}%\n" +
                $"平均反应时　{avgReaction:F2}s\n\n" +
                $"按侧握键 / R 再来一轮";
        }
    }

    private void SetSessionLabelsVisible(bool visible)
    {
        if (timeText != null) timeText.gameObject.SetActive(visible);
        if (statText != null) statText.gameObject.SetActive(visible);
        if (hintText != null) hintText.gameObject.SetActive(visible);
        if (modeText != null) modeText.gameObject.SetActive(visible);
    }

    void Update()
    {
        if (session == null || timeText == null) return;
        if (session.CurrentState == SessionManager.State.GameOver) return;

        float remain = Mathf.Max(0f, session.RoundRemain);
        UpdateTimeText(remain);
    }

    private void UpdateTimeText(float remain)
    {
        if (timeText == null) return;
        int s = Mathf.CeilToInt(remain);
        timeText.text = $"{(s / 60):00}:{s % 60:00}";
    }
}
