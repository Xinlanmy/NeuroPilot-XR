/// <summary>
/// 命中源接口 —— 隔离"谁判定命中"。
/// 本轮：KeyboardHitSource（空格模拟）；后续：EegHitSource（融合层 EEG 回调）。
/// </summary>
public interface IHitSource
{
    /// <summary>本轮是否产生命中。由 SessionManager 在 AwaitHit 每帧调用。</summary>
    bool HitPressed();

    /// <summary>球轮开始时重置内部状态（防按键抖动跨轮）。</summary>
    void Reset();
}
