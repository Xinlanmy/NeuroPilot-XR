using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// SSVEP 频闪：Stopwatch 相位驱动，每帧判定亮/暗，仅状态翻转时刷材质（50% 方波）。
    /// 不用 Time.deltaTime 累加，防止高刷新率头显上频率漂移（9/5 实测方案）。
    /// 频率精度只受刷新率离散化影响：12Hz@120Hz = 10 帧整周期精确；其余频率有亚帧误差，
    /// 由 openspec 5.0 串流闸门实测（记录 FFT 峰值 vs 标称偏差）。
    /// </summary>
    public sealed class SsvepFlicker : MonoBehaviour
    {
        [SerializeField] private float frequencyHz = 12f;
        [SerializeField] private Color onColor = Color.white;
        [SerializeField] private Color offColor = new Color(0.07f, 0.07f, 0.07f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in/Standard

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Stopwatch _stopwatch;
        private bool _running;
        private bool _on;

        public float FrequencyHz => frequencyHz;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _stopwatch = new Stopwatch();
            Apply(onColor);
        }

        /// <summary>开始频闪；可覆盖频率（如从 trial_start / targets 表下发）。</summary>
        public void StartFlicker(float? frequencyOverride = null)
        {
            if (frequencyOverride.HasValue)
            {
                frequencyHz = frequencyOverride.Value;
            }

            if (frequencyHz <= 0f)
            {
                Debug.LogError($"[SsvepFlicker] 非法频率 {frequencyHz}Hz，忽略 StartFlicker");
                return;
            }

            _stopwatch.Restart();
            _on = true;
            Apply(onColor);
            _running = true;
        }

        public void StopFlicker()
        {
            _running = false;
        }

        /// <summary>命中/漏失反馈：停止频闪并固定颜色（绿=命中、灰=漏失）。</summary>
        public void SetSolid(Color color)
        {
            _running = false;
            Apply(color);
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            double phase = _stopwatch.Elapsed.TotalSeconds * frequencyHz % 1.0;
            bool on = phase < 0.5;
            if (on == _on)
            {
                return;
            }

            _on = on;
            Apply(on ? onColor : offColor);
        }

        private void Apply(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
