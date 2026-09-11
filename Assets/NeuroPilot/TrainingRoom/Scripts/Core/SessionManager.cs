using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 训练场状态机：Ready → Spawn → AwaitHit → (Hit|Miss) → Cooldown → Spawn … → GameOver
/// 职责：计时、出球/收球、命中判定、计数与结算。
/// 与场景解耦：场景对象(房间/球/UI)由 SceneBuilder 搭好后注入引用。
/// </summary>
public class SessionManager : MonoBehaviour
{
    public enum State { Ready, Spawn, AwaitHit, HitFeedback, MissFeedback, Cooldown, GameOver }

    [Header("运行时引用(由场景构建器注入)")]
    public SessionConfig config;
    public Spawner spawner;
    public TrainingHUD hud;
    public IHitSource hitSource;
    public TrainingEegPort EegPort { get; private set; }

    public State CurrentState { get; private set; }
    public float RoundRemain { get; private set; }
    public int HitCount { get; private set; }
    public int MissCount { get; private set; }

    private float _stateTimer;
    private float _ballSpawnTime;     // 出球时刻(用于反应时)
    private float _totalReaction;     // 累计反应时
    private int _reactionSamples;
    private bool _restartHeld;
    private readonly List<UnityEngine.XR.InputDevice> _controllers = new List<UnityEngine.XR.InputDevice>();

    private bool RestartPressed()
    {
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, _controllers);
        bool held = false;
        foreach (var device in _controllers)
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool grip) && grip)
                held = true;
        bool pressed = held && !_restartHeld;
        _restartHeld = held;
        return pressed || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);
    }

    void Start()
    {
        EegPort = GetComponent<TrainingEegPort>();
        if (config == null) config = ScriptableObject.CreateInstance<SessionConfig>();
        // EEG is the only confirmation source. Controllers remain available for UI, not simulated hits.
        RoundRemain = config.roundDuration;
        ChangeState(State.Ready);
    }

    void Update()
    {
        // 全局倒计时（Ready 不扣，Spawn 起算整轮）
        if (CurrentState != State.GameOver && CurrentState != State.Ready)
        {
            RoundRemain -= Time.deltaTime;
            if (RoundRemain <= 0f)
            {
                RoundRemain = 0f;
                ChangeState(State.GameOver);
                return;
            }
        }

        switch (CurrentState)
        {
            case State.Ready:
                _stateTimer -= Time.deltaTime;
                if (hud != null) hud.SetCountdown(Mathf.Max(0, Mathf.CeilToInt(_stateTimer)));
                if (_stateTimer <= 0f) ChangeState(State.Spawn);
                break;

            case State.Spawn:
                if (!spawner.TrySpawnNext())
                {
                    if (hud != null) hud.SetHint("靶区配置不可用，请检查训练区域设置");
                    break;
                }
                _ballSpawnTime = Time.time;
                ChangeState(State.AwaitHit);
                break;

            case State.AwaitHit:
                if (EegPort != null && EegPort.eegInputEnabled && EegPort.ConsumeHit())
                {
                    _totalReaction += Time.time - _ballSpawnTime;
                    _reactionSamples++;
                    ChangeState(State.HitFeedback);
                }
                else
                {
                    _stateTimer += Time.deltaTime;
                    if (_stateTimer >= config.maxExposeTime)
                        ChangeState(State.MissFeedback);
                }
                break;

            case State.HitFeedback:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) ChangeState(State.Cooldown);
                break;

            case State.MissFeedback:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) ChangeState(State.Cooldown);
                break;

            case State.Cooldown:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) ChangeState(State.Spawn);
                break;

            case State.GameOver:
                if (RestartPressed()) RestartRound();
                break;
        }
    }

    void ChangeState(State s)
    {
        if (CurrentState == State.AwaitHit && s != State.AwaitHit && EegPort != null) EegPort.EndStimulus();
        CurrentState = s;
        _stateTimer = 0f;
        switch (s)
        {
            case State.Ready:
                _stateTimer = config.readyCountdown;
                if (hud != null) hud.ResetForRound(config.roundDuration);
                var telemetryReady = GetComponent<NeuroPilotXR.Training.VrTelemetryPanel>();
                if (telemetryReady != null) telemetryReady.HideProfile();
                break;

            case State.Spawn:
                _stateTimer = 0f;
                break;

            case State.AwaitHit:
                _stateTimer = 0f;
                if (hitSource != null) hitSource.Reset();
                if (EegPort != null) EegPort.BeginStimulus(config.flickerHz);
                if (hud != null) { hud.HideCountdown(); hud.SetHint("注视前方目标  ·  等待命中确认"); }
                break;

            case State.HitFeedback:
                HitCount++;
                _stateTimer = config.hitFeedbackTime;
                if (spawner != null) spawner.PlayHitFeedback(config.hitColor, config.hitFeedbackTime);
                var reward = GetComponent<NeuroPilotXR.Navigation.SuccessReward>();
                if (reward != null && spawner != null && spawner.ball != null)
                {
                    reward.Play(spawner.ball.position);
                    spawner.DespawnCurrent();
                }
                if (hud != null) hud.OnHit();
                break;

            case State.MissFeedback:
                MissCount++;
                _stateTimer = config.missFeedbackTime;
                if (spawner != null) spawner.PlayMissFeedback(config.missColor, config.missFeedbackTime);
                if (hud != null) hud.OnMiss();
                break;

            case State.Cooldown:
                _stateTimer = config.cooldownTime;
                if (spawner != null) spawner.DespawnCurrent();
                break;

            case State.GameOver:
                _restartHeld = true;
                if (spawner != null) spawner.DespawnCurrent();
                if (hud != null)
                {
                    float rate = (HitCount + MissCount) > 0 ? (float)HitCount / (HitCount + MissCount) * 100f : 0f;
                    float avgRt = _reactionSamples > 0 ? _totalReaction / _reactionSamples : 0f;
                    hud.ShowResult(HitCount, MissCount, rate, avgRt);
                }
                var telemetry = GetComponent<NeuroPilotXR.Training.VrTelemetryPanel>();
                if (telemetry != null) telemetry.ShowSessionResult(HitCount, MissCount, rate, avgRt);
                Debug.Log($"[训练场] 结束 命中={HitCount} 漏失={MissCount} " +
                          $"平均反应时={(_reactionSamples > 0 ? _totalReaction / _reactionSamples : 0f):F2}s");
                break;
        }
    }

    /// <summary>供 HUD/外部按钮 重新开始</summary>
    public void RestartRound()
    {
        if (EegPort != null) EegPort.EndStimulus();
        if (spawner != null) spawner.DespawnCurrent();
        HitCount = 0; MissCount = 0;
        _totalReaction = 0f; _reactionSamples = 0;
        RoundRemain = config.roundDuration;
        ChangeState(State.Ready);
    }

    private void OnDisable()
    {
        if (EegPort != null) EegPort.EndStimulus();
        if (spawner != null) spawner.DespawnCurrent();
        if (CurrentState != State.Ready && CurrentState != State.GameOver)
            ChangeState(State.Spawn);
    }
}
