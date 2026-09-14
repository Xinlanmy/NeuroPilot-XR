using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    [CreateAssetMenu(menuName = "NeuroPilot/Multi-target frequency mapping")]
    public sealed class MultiFrequencyConfig : ScriptableObject
    {
        [Tooltip("Three simultaneous targets use entries 1, 2, 3 respectively. Prototype frequencies must match the algorithm.")]
        public float[] frequencies = new float[0];
        [UnityEngine.Serialization.FormerlySerializedAs("mappingConfirmed")]
        public bool prototypeEnabled;
        [Range(.05f, .95f)] public float dutyCycle = .5f;

        /// <summary>池可用（不写死目标个数：L2 同闪上限由 GazeSubsetGate 定）：已启用、非空、每条合法、两两不同频。</summary>
        public bool HasUsablePool
        {
            get
            {
                if (!prototypeEnabled || frequencies == null || frequencies.Length == 0) return false;
                for (int i = 0; i < frequencies.Length; i++)
                {
                    if (float.IsNaN(frequencies[i]) || float.IsInfinity(frequencies[i]) || frequencies[i] <= 0) return false;
                    for (int j = 0; j < i; j++)
                        if (Mathf.Abs(frequencies[i] - frequencies[j]) < .01f) return false;
                }
                return true;
            }
        }

        /// <summary>固定目标数的严格校验：在 HasUsablePool 之上再加"池内禁 f/2f 对"。</summary>
        public bool IsValid(int count)
        {
            if (!HasUsablePool || frequencies.Length != count) return false;
            for (int i = 0; i < frequencies.Length; i++)
                for (int j = 0; j < i; j++)
                    if (Mathf.Abs(frequencies[i] - frequencies[j] * 2) < .05f ||
                        Mathf.Abs(frequencies[j] - frequencies[i] * 2) < .05f) return false;
            return true;
        }

        /// <summary>
        /// 门控取号：从池内未被在册频率（在闪 + 迟滞待停）占用的候选里，取"与在册集合最小间距最大"者；
        /// 禁选与在册频率构成 f/2f 关系者；平局按帧误差择小、再按池内声明顺序（= 实测优先级，12 已验证）。
        /// 与 app/alg/freq_pool.py 的 assign_next 是同一规则的两份实现，改动必须同步。
        /// </summary>
        public bool TryAssign(float[] taken, out float hz, float refreshHz = 90f)
        {
            hz = 0f;
            if (!HasUsablePool) return false;
            bool found = false;
            float bestGap = 0f, bestErr = 0f;
            for (int i = 0; i < frequencies.Length; i++)
            {
                float f = frequencies[i];
                if (InUse(taken, f) || HalfDoubleOf(taken, f)) continue;
                float gap = MinGap(taken, f);
                float err = FrameError(f, refreshHz);
                bool better = !found || gap > bestGap + 1e-4f ||
                    (Mathf.Abs(gap - bestGap) <= 1e-4f && err < bestErr - 1e-4f);
                if (!better) continue;
                found = true; hz = f; bestGap = gap; bestErr = err;
            }
            return found;
        }

        private static bool InUse(float[] taken, float f)
        {
            if (taken == null) return false;
            foreach (float t in taken) if (Mathf.Abs(f - t) < .01f) return true;
            return false;
        }

        private static bool HalfDoubleOf(float[] taken, float f)
        {
            if (taken == null) return false;
            foreach (float t in taken)
                if (Mathf.Abs(f - t * 2f) < .05f || Mathf.Abs(t - f * 2f) < .05f) return true;
            return false;
        }

        private static float MinGap(float[] taken, float f)
        {
            if (taken == null || taken.Length == 0) return 1e9f;
            float gap = float.MaxValue;
            foreach (float t in taken) gap = Mathf.Min(gap, Mathf.Abs(f - t));
            return gap;
        }

        /// <summary>半周期帧数的小数误差（0 = 整帧精确），与 freq_pool.frame_error 同口径。</summary>
        private static float FrameError(float f, float refreshHz)
        {
            if (refreshHz <= 0f) return 0f;
            float halfPeriodFrames = refreshHz / (2f * f);
            return Mathf.Abs(halfPeriodFrames - Mathf.Round(halfPeriodFrames));
        }
    }
}
