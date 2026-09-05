using System;
using UnityEngine;

/// <summary>
/// SSVEP 精确方波闪烁 —— 用 Stopwatch 计时防止 Unity 帧率波动导致频率漂移。
/// 逻辑：每半个周期(1/(2*f))切换一次明暗；由 SessionManager 启停。
/// </summary>
public class SSVEPFlicker : MonoBehaviour
{
    [SerializeField] private float _hz = 15f;
    [SerializeField, Range(0.05f, 0.95f)] private float _duty = 0.5f;
    [SerializeField] private Color _onColor = Color.white;
    [SerializeField] private Color _offColor = Color.black;

    private Renderer _rend;
    private Material _mat;
    private System.Diagnostics.Stopwatch _sw;
    private bool _running;
    private float _halfPeriodMs;
    private float _onMs;   // 亮态时长
    private float _offMs;  // 暗态时长

    /// <summary>配置刺激参数（出球前调用一次）</summary>
    public void Configure(float hz, float dutyCycle, Color onColor, Color offColor)
    {
        _hz = Mathf.Max(1f, hz);
        _duty = Mathf.Clamp01(dutyCycle);
        _onColor = onColor;
        _offColor = offColor;
        Recalc();
    }

    private void Recalc()
    {
        float periodMs = 1000f / _hz;
        _onMs = periodMs * _duty;
        _offMs = periodMs * (1f - _duty);
        _halfPeriodMs = periodMs / 2f;
    }

    public float Frequency => _hz;

    private void Awake()
    {
        EnsureInit();
    }

    /// <summary>懒初始化：保证 _sw/_mat 在任意调用前就绪（含球 inactive 时被直接调用的兜底）</summary>
    private void EnsureInit()
    {
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_mat == null && _rend != null) _mat = _rend.material;
        if (_sw == null) _sw = new System.Diagnostics.Stopwatch();
        Recalc();
    }

    public void StartFlicker()
    {
        EnsureInit();
        if (!_running)
        {
            _running = true;
            _sw.Restart();
            SetVisible(true); // 亮态开场
        }
    }

    public void StopFlicker()
    {
        if (_sw == null) _sw = new System.Diagnostics.Stopwatch();
        _running = false;
        _sw.Stop();
        SetVisible(false);
    }

    private void Update()
    {
        if (!_running || _mat == null) return;

        long elapsed = _sw.ElapsedMilliseconds;
        long cycleMs = (long)(_onMs + _offMs);
        long pos = elapsed % cycleMs;

        // 亮态时长内为 on，否则 off（用占空比而非 50% 硬切）
        bool shouldOn = pos < (long)_onMs;
        SetVisible(shouldOn);
    }

    private void SetVisible(bool on)
    {
        if (_mat != null)
        {
            _mat.color = on ? _onColor : _offColor;
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", on ? _onColor * 1.2f : Color.black);
        }
    }

    /// <summary>供反馈阶段直接置色(命中绿/漏失灰)</summary>
    public void ForceColor(Color c)
    {
        if (_mat != null) _mat.color = c;
    }

    public void ResetMaterial()
    {
        // 恢复无自发光，避免残留
        if (_mat != null)
        {
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", Color.black);
        }
    }
}
