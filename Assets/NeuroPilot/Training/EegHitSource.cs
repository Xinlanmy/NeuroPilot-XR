using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 融合层命中源：FusionLink 收到 command_fire（SSVEP 判定确认）时 NotifyHit() 置位，
    /// SessionManager 轮询消费——「判定即执行」链路的 Unity 侧入口。
    /// </summary>
    public sealed class EegHitSource : MonoBehaviour, IHitSource
    {
        private string _pendingTargetId;

        /// <summary>最近一次命中的 target_id（调试/打点用）。</summary>
        public string LastHitTargetId { get; private set; }

        /// <summary>融合层判定确认；targetId 为空时按 unknown 处理。</summary>
        public void NotifyHit(string targetId)
        {
            _pendingTargetId = string.IsNullOrEmpty(targetId) ? "unknown" : targetId;
        }

        public bool HitPressed()
        {
            if (_pendingTargetId == null)
            {
                return false;
            }

            LastHitTargetId = _pendingTargetId;
            _pendingTargetId = null;
            return true;
        }

        public void ResetHit()
        {
            _pendingTargetId = null;
        }
    }
}
