using UnityEngine;

/// <summary>
/// 注意力训练场 · 全部可调参数
/// Inspector 暴露，运行时只读。算法/玩法要调参数改这里。
/// </summary>
[CreateAssetMenu(fileName = "SessionConfig", menuName = "NeuroPilot/训练场配置")]
public class SessionConfig : ScriptableObject
{
    [Header("本轮时长")]
    [Tooltip("一轮游戏总时长(秒)")] public float roundDuration = 180f;
    [Tooltip("开场准备倒计时(秒)")] public float readyCountdown = 3f;

    [Header("SSVEP 刺激")]
    [Tooltip("闪烁频率(Hz)")] public float flickerHz = 15f;
    [Tooltip("占空比 0~1，默认 0.5")] [Range(0.05f, 0.95f)] public float dutyCycle = 0.5f;
    [Tooltip("亮态颜色")] public Color onColor = Color.white;
    [Tooltip("暗态颜色")] public Color offColor = Color.black;

    [Header("出球规则")]
    [Tooltip("单球最长暴露(秒)，超时未命中计漏失")] public float maxExposeTime = 12f;
    [Tooltip("命中反馈(变绿)时长(秒)")] public float hitFeedbackTime = 0.5f;
    [Tooltip("漏失反馈(变灰淡化)时长(秒)")] public float missFeedbackTime = 0.6f;
    [Tooltip("消失后空档(秒)")] public float cooldownTime = 1f;

    [Header("球出现区域(相对相机视野)")]
    [Tooltip("球距相机前向深度范围(米)，保证球只在正前方视野内出现")]
    public Vector2 spawnZRange = new Vector2(1.5f, 5.5f);
    [Tooltip("水平安全边界(米)，视野锥约束后的额外上限，防球贴墙")]
    public Vector2 spawnXRange = new Vector2(-3f, 3f);
    [Tooltip("垂直高度范围(米)")]
    public Vector2 spawnYRange = new Vector2(1.3f, 2.2f);
    [Tooltip("视野收缩边距(米)，避免球贴视野边缘")]
    public float viewMargin = 0.5f;

    [Header("球体")]
    [Tooltip("球直径(米)")] public float ballDiameter = 0.25f;
    [Tooltip("命中变色")] public Color hitColor = Color.green;
    [Tooltip("漏失变色")] public Color missColor = new Color(0.6f, 0.6f, 0.6f, 1f);
}
