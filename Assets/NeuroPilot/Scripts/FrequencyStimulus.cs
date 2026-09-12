using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    /// <summary>
    /// 多球模式单球方波：连续相位（onset 起算，不随帧率累积漂移）+ 可配占空比 + 渐入渐出包络。
    /// 相位与幅度分离：渐出期间相位照常推进，只是幅度衰减到 0 后清掉材质属性块。
    /// </summary>
    public sealed class FrequencyStimulus : MonoBehaviour
    {
        public float Frequency { get; private set; }
        private Renderer surface;
        private MaterialPropertyBlock block;
        private double onset;
        private float duty;
        private bool running;
        private float rampSeconds;
        private double stopAt;
        public float Intensity = 1f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public void Begin(float hz, float dutyCycle) => Begin(hz, dutyCycle, 0f);

        public void Begin(float hz, float dutyCycle, float ramp)
        {
            surface = GetComponent<Renderer>(); block = block ?? new MaterialPropertyBlock();
            Frequency = hz; duty = dutyCycle; onset = Time.realtimeSinceStartupAsDouble;
            rampSeconds = Mathf.Max(0f, ramp); stopAt = 0.0; running = true;
            Paint(true, Envelope());
        }

        public void Stop() => Stop(0f);

        /// <summary>fade &gt; 0 时先渐出再清材质；offset 事件在此之前已经发出（判定窗已关）。</summary>
        public void Stop(float fade)
        {
            if (!running) return;
            if (fade > 0f) { stopAt = Time.realtimeSinceStartupAsDouble; rampSeconds = fade; return; }
            running = false;
            if (surface != null) surface.SetPropertyBlock(null);
        }

        private void Update()
        {
            if (!running) return;
            double phase = (Time.realtimeSinceStartupAsDouble - onset) * Frequency;
            float amp = Envelope();
            if (stopAt > 0.0 && amp <= 0f)
            {
                running = false;
                if (surface != null) surface.SetPropertyBlock(null);
                return;
            }
            Paint(phase - System.Math.Floor(phase) < duty, amp);
        }

        /// <summary>渐入（onset 起）/ 渐出（stopAt 起）的幅度包络，× Intensity 保持既有语义。</summary>
        private float Envelope()
        {
            float amp = 1f;
            if (rampSeconds > 0f)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                amp = stopAt > 0.0
                    ? Mathf.Clamp01(1f - (float)((now - stopAt) / rampSeconds))
                    : Mathf.Clamp01((float)((now - onset) / rampSeconds));
            }
            return amp * Intensity;
        }

        private void Paint(bool on, float amp)
        {
            if (surface == null) return;
            block.SetColor(BaseColorId, Color.Lerp(new Color(.35f, .4f, .45f), on ? Color.white : new Color(.07f, .07f, .07f), amp));
            surface.SetPropertyBlock(block);
        }

        private void OnDisable() => Stop();
    }
}
