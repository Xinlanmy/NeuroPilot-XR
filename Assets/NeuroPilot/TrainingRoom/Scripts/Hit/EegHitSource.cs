using UnityEngine;

/// <summary>
/// EEG 命中源（预留占位）。
/// TODO(接融合层后实现)：
///   1. 由外部(冯周杰的融合层/TCP 服务)在"识别到 SSVEP 频率"时调用 NotifyHit();
///   2. 每轮出球时 SessionManager 会调 Reset()，请在此清空 _hit 标志；
///   3. 命中需带 t_ms 时间戳，可在 NotifyHit 里记录，供离线对齐验证。
/// 本轮不挂载，仅保留接口形态。
/// </summary>
public class EegHitSource : MonoBehaviour, IHitSource
{
    private bool _hit;

    /// <summary>外部 EEG 识别回调入口（融合层调用）</summary>
    public void NotifyHit()
    {
        _hit = true;
    }

    public bool HitPressed()
    {
        bool v = _hit;
        _hit = false;
        return v;
    }

    public void Reset()
    {
        _hit = false;
    }
}
