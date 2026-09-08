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
    public Text accuracyText;
    public Text countdownText;
    public GameObject statsRoot;
    public GameObject footerRoot;
    public GameObject countdownRoot;

    private float _roundDuration;

    public void ResetForRound(float duration)
    {
        SetSessionLabelsVisible(true);
        _roundDuration = duration;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintText != null) hintText.text = "";
        UpdateTimeText(duration);
        if (statText != null) statText.text = "0";
        if (accuracyText != null) accuracyText.text = "—";
        SetCountdown(Mathf.CeilToInt(session != null && session.config != null ? session.config.readyCountdown : 3));
        SetHint("面向前方靶区  ·  准备开始");
        if (modeText != null)
        {
            var difficulty = NeuroPilotXR.Navigation.TrainingSession.SelectedDifficulty;
            string label = difficulty == NeuroPilotXR.Navigation.DifficultyLevel.Beginner ? "轻度" :
                difficulty == NeuroPilotXR.Navigation.DifficultyLevel.Advanced ? "挑战" : "标准";
            modeText.text = "TrainingRoom v" + Application.version + "  ·  " + label + "  ·  " +
                (session != null && session.EegPort != null && session.EegPort.eegInputEnabled
                    ? "脑电 SSVEP · 接口待接入" : "脑电确认已暂停");
        }
    }

    public void SetHint(string s)
    {
        if (hintText != null) hintText.text = s;
    }

    public void OnHit()
    {
        UpdateStats();
        SetHint("命中  ·  准备下一个目标");
    }

    public void OnMiss()
    {
        UpdateStats();
        SetHint("本次超时  ·  继续下一个目标");
    }

    private void UpdateStats()
    {
        if (session == null) return;
        if (statText != null) statText.text = session.HitCount.ToString();
        int total = session.HitCount + session.MissCount;
        if (accuracyText != null) accuracyText.text = total == 0 ? "—" : (100f * session.HitCount / total).ToString("F0") + "%";
    }

    public void SetCountdown(int seconds)
    {
        if (countdownRoot != null) countdownRoot.SetActive(true);
        if (countdownText != null) countdownText.text = seconds > 0 ? seconds.ToString() : "开始";
    }

    public void HideCountdown() { if (countdownRoot != null) countdownRoot.SetActive(false); }

    public void ShowResult(int hit, int miss, float rate, float avgReaction)
    {
        SetSessionLabelsVisible(false);
        HideCountdown();
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
        {
            resultText.text =
                "<size=26><color=#8DA9BA>NEUROPILOT  /  SESSION COMPLETE</color></size>\n\n" +
                "<size=64>本轮结束</size>\n\n" +
                $"命中  <color=#40D6ED>{hit}</color>    漏失  {miss}\n" +
                $"命中率  {(hit + miss > 0 ? rate.ToString("F1") + "%" : "—")}\n" +
                $"平均反应时  {(hit > 0 ? avgReaction.ToString("F2") + " 秒" : "—")}\n\n" +
                "<size=30><color=#8DA9BA>按侧握键 / R  再来一轮</color></size>";
        }
    }

    private void SetSessionLabelsVisible(bool visible)
    {
        if (statsRoot != null) statsRoot.SetActive(visible);
        if (footerRoot != null) footerRoot.SetActive(visible);
        if (accuracyText != null) accuracyText.gameObject.SetActive(visible);
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
