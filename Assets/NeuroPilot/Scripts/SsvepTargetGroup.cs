using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    /// <summary>
    /// 多球 SSVEP 目标组：只管"哪个球在闪、闪什么频率、id 是什么"，**不决定谁该闪**。
    /// 谁闪由 GazeSubsetGate 按眼动子集决定（L2），齐闪降级也走同一条分配路径。
    /// </summary>
    public sealed class SsvepTargetGroup : MonoBehaviour
    {
        public MultiFrequencyConfig config;
        public event Action<PracticeTarget, bool> EpisodeChanged;
        private readonly List<PracticeTarget> targets = new List<PracticeTarget>();
        public int ActiveCount => targets.FindAll(t => t != null && t.Stimulating).Count;
        public IReadOnlyList<PracticeTarget> Targets => targets;

        /// <summary>只登记目标、不起闪——起闪由门控（或齐闪降级）逐球调用 Begin。</summary>
        public void SetTargets(IEnumerable<PracticeTarget> items)
        {
            StopAll(); targets.Clear(); targets.AddRange(items);
        }

        /// <summary>按分配到的频率起闪（门控取号的结果）；rampSeconds &gt; 0 时渐入。</summary>
        public bool Begin(PracticeTarget target, float hz, float rampSeconds = 0f)
        {
            if (target == null || target.Stimulating || !target.Available || target.Stimulus == null) return false;
            if (config == null || !config.HasUsablePool || hz <= 0f) return false;
            target.Frequency = hz;
            target.Id = Guid.NewGuid().ToString("N");
            target.Stimulating = true;
            target.Stimulus.Intensity = 1f;
            target.Stimulus.Begin(hz, config.dutyCycle, rampSeconds);
            EpisodeChanged?.Invoke(target, true);
            return true;
        }

        /// <summary>停闪：offset 立刻发（判定窗关闭），fadeSeconds &gt; 0 时只有视觉渐出。</summary>
        public bool Stop(PracticeTarget target, float fadeSeconds = 0f)
        {
            if (target == null || !target.Stimulating) return false;
            if (target.Stimulus != null) target.Stimulus.Stop(fadeSeconds);
            // Latch before callbacks to prevent duplicate offsets during re-entrant cleanup.
            target.Stimulating = false;
            EpisodeChanged?.Invoke(target, false);
            target.Id = null; target.Frequency = 0;
            return true;
        }

        /// <summary>
        /// 超时再武装：原地换新 episode（旧 id 的 offset → 新 id 的 onset），频率与视觉相位不变。
        /// 给算法侧一次全新的判定窗，避免"注视不动但试次已超时"的死局。
        /// </summary>
        public bool Rearm(PracticeTarget target)
        {
            if (target == null || !target.Stimulating) return false;
            EpisodeChanged?.Invoke(target, false);
            target.Id = Guid.NewGuid().ToString("N");
            EpisodeChanged?.Invoke(target, true);
            return true;
        }

        /// <summary>在册（在闪）频率，供门控取号时排除。</summary>
        public float[] TakenFrequencies()
        {
            var taken = new List<float>();
            foreach (var target in targets)
                if (target != null && target.Stimulating && target.Frequency > 0f) taken.Add(target.Frequency);
            return taken.ToArray();
        }

        public void StopAll() { foreach (var target in targets) Stop(target); }
        private void OnDisable() => StopAll();
    }
}
