using System.Collections.Generic;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    /// <summary>门控策略：GazeGated = 眼动门控子集（眼动失效自动降级齐闪）；AlwaysOn = 定死齐闪。</summary>
    public enum GatePolicy { GazeGated, AlwaysOn }

    /// <summary>
    /// L2 门控子集闪烁（2026-09-12 立项，openspec design.md 决策 10）：眼动决定"谁闪"，SSVEP 决定"打谁"。
    ///
    /// 规则：注视任一球 dwell 0.1s → 以"视线射线"为中心、半径 gazeRegionRadiusMeters 内的可用球全部同闪
    /// （区域内有几颗闪几颗；超过 maxSimultaneous 才裁到离视线最近的几颗；锚点必在内）；
    /// 频率从池内贪心 max-min 取号（同闪两两不同频）、episode 内冻结；视线离开 0.5s 迟滞后才停闪
    /// （迟滞期内不还频率，回来不产生事件抖动）；在闪超时未命中则原地再武装（换新 id，同频）；
    /// 眼动连续失效超时 → 自动降级为齐闪，恢复后自动切回。
    ///
    /// 参数权威值在本组件（场景序列化）；`configs/eeg_w64.toml` 的 [ssvep.gate] 是跨仓库镜像。
    /// 与算法侧的分工：这里只负责"谁在闪 + freq_hz 随 onset 下发"，判定仍在 Python（契约零新增字段）。
    /// </summary>
    public sealed class GazeSubsetGate : MonoBehaviour
    {
        public GatePolicy policy = GatePolicy.GazeGated;
        public TargetPracticeSession session;
        public EyeGazeProvider gaze;
        public SsvepTargetGroup director;
        /// <summary>视线参照（相机）；留空则回退 Camera.main。</summary>
        public Transform view;

        [Header("门控参数（权威值；TOML [ssvep.gate] 是镜像）")]
        public float dwellSeconds = 0.10f;
        public float leaveHysteresisSeconds = 0.50f;
        public float rampSeconds = 0.40f;
        public int maxSimultaneous = 3;
        public float gazeRegionRadiusMeters = 1.2f;  // 球心到视线射线的垂直距离 ≤ 此值即进入在闪区域
        public float gazeLossFallbackSeconds = 3f;   // 0 = 关闭自动降级
        public float rearmSeconds = 6f;              // 0 = 关闭超时再武装；必须 < 闭环 --deadline（8s）

        /// <summary>给 HUD/验证器的可读状态。</summary>
        public string Status { get; private set; } = "";
        public bool FallbackActive { get; private set; }
        public PracticeTarget Anchor { get; private set; }
        public int ActiveCount => director != null ? director.ActiveCount : 0;

        private const float FallbackExitSeconds = 0.2f;
        private float clock, fixTime, lostTime, validTime;
        private Ray gazeRay;   // 最近一次有效视线：区域选点与抢占度量都以它为中心
        private readonly List<PracticeTarget> desired = new List<PracticeTarget>();
        private readonly Dictionary<PracticeTarget, float> leaveAt = new Dictionary<PracticeTarget, float>();
        private readonly Dictionary<PracticeTarget, float> armedAt = new Dictionary<PracticeTarget, float>();
        private readonly List<PracticeTarget> scratch = new List<PracticeTarget>();

        private Transform View => view != null ? view : (Camera.main != null ? Camera.main.transform : null);
        private MultiFrequencyConfig Pool => director != null ? director.config : null;

        private void Update()
        {
            if (session == null || director == null || session.mode != TrainingMode.MultiTarget) return;
            Ray ray = default;
            bool valid = gaze != null && gaze.TryGetRay(out ray);
            StepGaze(valid, ray, Time.deltaTime);
        }

        /// <summary>状态机步进。公开以便验证器注入眼动与时间步长（不依赖真实帧）。</summary>
        public void StepGaze(bool valid, Ray ray, float delta)
        {
            if (session == null || director == null) return;
            delta = Mathf.Max(0f, delta);
            clock += delta;
            if (!session.Running) { ResetAll(); Status = ""; return; }
            Prune();
            if (Pool == null || !Pool.HasUsablePool) { Status = "频率池尚未配置或无效：已禁止闪烁"; return; }

            bool inputValid = valid && policy == GatePolicy.GazeGated;
            if (policy == GatePolicy.AlwaysOn)
            {
                FallbackActive = true; validTime = 0f;
            }
            else if (FallbackActive)
            {
                validTime = inputValid ? validTime + delta : 0f;
                if (validTime >= FallbackExitSeconds) { FallbackActive = false; validTime = 0f; lostTime = 0f; }
            }
            else
            {
                lostTime = inputValid ? 0f : lostTime + delta;
                if (gazeLossFallbackSeconds > 0f && lostTime > gazeLossFallbackSeconds) FallbackActive = true;
            }

            if (FallbackActive)
            {
                StepAlwaysOn();
                RearmDue();
                Status = policy == GatePolicy.AlwaysOn ? "齐闪模式（场景定死）"
                    : "眼动不可用 · 已切换备用齐闪";
                return;
            }

            PracticeTarget hit = valid ? session.ResolveGazeTarget(ray) : null;
            if (hit != Anchor) { Anchor = hit; fixTime = 0f; }
            if (Anchor != null) fixTime += delta;
            if (valid) gazeRay = ray;
            desired.Clear();
            if (Anchor != null && fixTime >= dwellSeconds) SelectSubset(Anchor, gazeRay);
            Apply();
            RearmDue();
            Status = Anchor == null
                ? "注视任意小球 " + dwellSeconds.ToString("0.0") + " 秒，视线附近的球开始闪烁"
                : "门控中：在闪 " + ActiveCount + " / " + Mathf.Max(1, maxSimultaneous);
        }

        /// <summary>
        /// 成员制：球心到视线射线的垂直距离 ≤ gazeRegionRadiusMeters（射线后方不算）即入选，
        /// 锚点必在内；超出 maxSimultaneous 才裁到离视线最近的几颗。
        /// </summary>
        private void SelectSubset(PracticeTarget anchor, Ray ray)
        {
            desired.Add(anchor);
            scratch.Clear();
            int limit = Mathf.Max(1, maxSimultaneous);
            if (limit == 1) return;
            foreach (var target in session.Targets)
            {
                if (target == null || target == anchor || !target.Available) continue;
                float distance = DistanceToRay(ray, target.transform.position);
                if (distance < 0f || distance > gazeRegionRadiusMeters) continue;   // 负值 = 在射线后方
                scratch.Add(target);
            }
            scratch.Sort((a, b) => DistanceToRay(ray, a.transform.position)
                .CompareTo(DistanceToRay(ray, b.transform.position)));
            for (int i = 0; i < scratch.Count && desired.Count < limit; i++) desired.Add(scratch[i]);
        }

        /// <summary>点到射线距离；投影 t<0（目标在视线后方）返回 -1。</summary>
        private static float DistanceToRay(Ray ray, Vector3 point)
        {
            Vector3 offset = point - ray.origin;
            if (Vector3.Dot(offset, ray.direction) <= 0f) return -1f;
            return Vector3.Cross(ray.direction, offset).magnitude;
        }

        /// <summary>把 desired 落到实际闪烁：离场排期（迟滞）、入场取号、池满则抢占。</summary>
        private void Apply()
        {
            // 迟滞内回头 → 撤销排期，频率与视觉都不动（"不还牌"）
            foreach (var target in desired) leaveAt.Remove(target);
            foreach (var target in session.Targets)
            {
                if (target == null || !target.Stimulating || desired.Contains(target)) continue;
                if (!leaveAt.ContainsKey(target)) leaveAt[target] = clock + leaveHysteresisSeconds;
            }
            scratch.Clear();
            foreach (var pair in leaveAt) if (pair.Value <= clock) scratch.Add(pair.Key);
            foreach (var target in scratch)
            {
                leaveAt.Remove(target);
                armedAt.Remove(target);
                director.Stop(target, rampSeconds);
            }

            foreach (var target in desired)
            {
                if (target == null || target.Stimulating || !target.Available) continue;
                if (!Pool.TryAssign(director.TakenFrequencies(), out float hz))
                {
                    // 池被占满：抢掉离视线最远的在闪球腾出频率（成员制：视线区域内优先）
                    PracticeTarget evict = Farthest();
                    if (evict == null || !director.Stop(evict, rampSeconds)) continue;
                    leaveAt.Remove(evict); armedAt.Remove(evict);
                    if (!Pool.TryAssign(director.TakenFrequencies(), out hz)) continue;
                }
                if (director.Begin(target, hz, rampSeconds)) armedAt[target] = clock;
            }
        }

        /// <summary>齐闪（降级）：可用球里 Slot 最小的 maxSimultaneous 个常闪，各自不同频；无迟滞。</summary>
        private void StepAlwaysOn()
        {
            scratch.Clear();
            foreach (var target in session.Targets) if (target != null && target.Available) scratch.Add(target);
            scratch.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            desired.Clear();
            for (int i = 0; i < scratch.Count && desired.Count < Mathf.Max(1, maxSimultaneous); i++) desired.Add(scratch[i]);
            leaveAt.Clear();
            foreach (var target in session.Targets)
            {
                if (target == null || !target.Stimulating || desired.Contains(target)) continue;
                armedAt.Remove(target);
                director.Stop(target, rampSeconds);
            }
            foreach (var target in desired)
            {
                if (target.Stimulating || !target.Available) continue;
                if (!Pool.TryAssign(director.TakenFrequencies(), out float hz)) continue;
                if (director.Begin(target, hz, rampSeconds)) armedAt[target] = clock;
            }
        }

        /// <summary>超时再武装：仍在闪但本 episode 已跑满 rearmSeconds → 原地换新 id，算法侧重开判定窗。</summary>
        private void RearmDue()
        {
            if (rearmSeconds <= 0f) return;
            foreach (var target in session.Targets)
            {
                if (target == null || !target.Stimulating) { if (target != null) armedAt.Remove(target); continue; }
                if (!armedAt.TryGetValue(target, out float armed)) { armedAt[target] = clock; continue; }
                if (clock - armed < rearmSeconds) continue;
                if (director.Rearm(target)) armedAt[target] = clock;
            }
        }

        /// <summary>离视线射线最远的在闪球（不在 desired 里的）；用于池满抢占。视线无效时退回离原点最远。</summary>
        private PracticeTarget Farthest()
        {
            PracticeTarget worst = null;
            float worstDistance = -1f;
            foreach (var target in session.Targets)
            {
                if (target == null || !target.Stimulating || desired.Contains(target)) continue;
                float distance = DistanceToRay(gazeRay, target.transform.position);
                if (distance < 0f) distance = 0f;   // 射线后方的在闪球视为"最近"，优先留到最后
                if (distance <= worstDistance) continue;
                worst = target; worstDistance = distance;
            }
            return worst;
        }

        /// <summary>丢掉已销毁/刚命中/停止闪烁的目标，避免登记表留下悬空引用。</summary>
        private void Prune()
        {
            if (Anchor != null && (Anchor.transform == null || !Anchor.Available)) { Anchor = null; fixTime = 0f; }
            for (int i = desired.Count - 1; i >= 0; i--)
                if (desired[i] == null || desired[i].transform == null) desired.RemoveAt(i);
            PruneKeys(leaveAt);
            PruneKeys(armedAt);
            foreach (var target in session.Targets)
            {
                if (target == null || target.Stimulating) continue;
                leaveAt.Remove(target);
                armedAt.Remove(target);
            }
        }

        private void PruneKeys(Dictionary<PracticeTarget, float> table)
        {
            scratch.Clear();
            foreach (var pair in table)
                if (pair.Key == null || pair.Key.transform == null) scratch.Add(pair.Key);
            foreach (var key in scratch) table.Remove(key);
        }

        private void ResetAll()
        {
            desired.Clear(); leaveAt.Clear(); armedAt.Clear();
            Anchor = null; FallbackActive = policy == GatePolicy.AlwaysOn;
            clock = fixTime = lostTime = validTime = 0f;
            gazeRay = default;
        }
    }
}
