namespace NeuroPilotXR.Training
{
    /// <summary>命中源抽象：每轮 AwaitHit 开始前由 SessionManager 调 ResetHit()。</summary>
    public interface IHitSource
    {
        /// <summary>本帧是否命中（由 SessionManager 每帧轮询，命中即消费）。</summary>
        bool HitPressed();

        /// <summary>清空待处理命中状态（不叫 Reset 是避免撞上 Unity 编辑器消息）。</summary>
        void ResetHit();
    }
}
