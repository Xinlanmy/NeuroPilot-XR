using System;
using NeuroPilotXR.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroPilotXR.Training
{
    public enum TrialPhase
    {
        Idle,
        Ready,
        AwaitHit,
        HitFeedback,
        MissFeedback,
        GameOver,
    }

    public enum HitSourceMode
    {
        KeyboardSim,
        EegFusion,
    }

    /// <summary>
    /// 幕2「持续注意·锁航」训练会话状态机（9/5 验证机制落地）：
    /// Ready(3s) → Spawn → AwaitHit(12s 超时) → HitFeedback(绿 0.5s) / MissFeedback(灰 0.6s)
    /// → 立即下一球 …；180s 一轮，结束出结算。
    /// 命中源：键盘空格模拟（开发/演示）或融合层 command_fire（判定即执行）。
    /// spawn/onset 等事件经 FusionLink 按契约上行；断线自动降级，训练不中断。
    /// </summary>
    public sealed class TrialSessionManager : MonoBehaviour
    {
        [Header("引用（SpaceTrainingBootstrapper 自动接线）")]
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private TargetSpawner spawner;
        [SerializeField] private TrainingHud hud;
        [SerializeField] private FusionLink fusionLink;
        [SerializeField] private KeyboardHitSource keyboardHitSource;
        [SerializeField] private EegHitSource eegHitSource;
        [SerializeField] private HitSourceMode hitSourceMode = HitSourceMode.KeyboardSim;

        [Header("会话参数")]
        [SerializeField] private float readySeconds = 3f;
        [SerializeField] private float roundSeconds = 180f;
        [SerializeField] private float awaitHitTimeout = 12f;
        [SerializeField] private float hitFeedbackSeconds = 0.5f;
        [SerializeField] private float missFeedbackSeconds = 0.6f;
        [SerializeField] private float defaultFrequencyHz = 15f;

        [Header("反馈配色（绿=命中 / 灰=漏失）")]
        [SerializeField] private Color hitColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color missColor = new Color(0.45f, 0.45f, 0.45f);

        public TrialPhase Phase { get; private set; } = TrialPhase.Idle;
        public int Hits { get; private set; }
        public int Misses { get; private set; }

        private Camera _viewCamera;
        private float _phaseStart;
        private float _roundElapsed;
        private int _targetCounter;
        private readonly int _wave = 1;
        private double _reactionSumSeconds;
        private GameObject _currentBall;
        private SsvepFlicker _currentFlicker;
        private string _currentTargetId;
        private long _onsetMs;

        private string LevelName => TrainingSession.SelectedDifficulty.ToString().ToLowerInvariant();

        private void OnEnable()
        {
            if (fusionLink != null)
            {
                fusionLink.CommandReceived += OnCommand;
            }
        }

        private void OnDisable()
        {
            if (fusionLink != null)
            {
                fusionLink.CommandReceived -= OnCommand;
            }
        }

        private void Start()
        {
            _viewCamera = Camera.main;
            if (_viewCamera == null)
            {
                _viewCamera = FindFirstObjectByType<Camera>();
            }

            BeginReady();
        }

        private void Update()
        {
            switch (Phase)
            {
                case TrialPhase.Ready:
                    if (Elapsed(readySeconds))
                    {
                        BeginSpawn();
                    }

                    break;
                case TrialPhase.AwaitHit:
                    _roundElapsed += Time.deltaTime;
                    if (ResolveHitSource().HitPressed())
                    {
                        OnHit();
                    }
                    else if (Elapsed(awaitHitTimeout))
                    {
                        OnMiss();
                    }

                    break;
                case TrialPhase.HitFeedback:
                    _roundElapsed += Time.deltaTime;
                    if (Elapsed(hitFeedbackSeconds))
                    {
                        BeginSpawn();
                    }

                    break;
                case TrialPhase.MissFeedback:
                    _roundElapsed += Time.deltaTime;
                    if (Elapsed(missFeedbackSeconds))
                    {
                        BeginSpawn();
                    }

                    break;
            }

            if (Phase != TrialPhase.Idle && Phase != TrialPhase.GameOver)
            {
                if (_roundElapsed >= roundSeconds)
                {
                    EndRound();
                }
                else if (hud != null)
                {
                    hud.UpdateStatus(roundSeconds - _roundElapsed, Hits, Misses);
                }
            }

            if (Phase == TrialPhase.GameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                BeginReady();
            }
        }

        /// <summary>外部入口：开始/重新开始一轮（command_reset 也走这里）。</summary>
        public void BeginReady()
        {
            Hits = 0;
            Misses = 0;
            _reactionSumSeconds = 0;
            _roundElapsed = 0;
            _targetCounter = 0;
            ClearCurrentBall();
            ResolveHitSource().ResetHit();
            EnterPhase(TrialPhase.Ready);
            if (hud != null)
            {
                hud.HideSummary();
                hud.UpdateStatus(roundSeconds, 0, 0);
                hud.SetPrompt("准备…盯住出现的球");
            }

            SendSceneEvent("session_start");
        }

        private void BeginSpawn()
        {
            if (spawner == null || targetPrefab == null)
            {
                Debug.LogError("[TrialSession] spawner / targetPrefab 未接线（跑 NeuroPilot → 搭建 SSVEP 训练场景）");
                EnterPhase(TrialPhase.Idle);
                return;
            }

            if (_viewCamera == null)
            {
                Debug.LogError("[TrialSession] 找不到相机（需 tagged MainCamera 的 XR 相机）");
                EnterPhase(TrialPhase.Idle);
                return;
            }

            ClearCurrentBall();
            _targetCounter++;
            _currentTargetId = "t" + _targetCounter;
            Vector3 position = spawner.PickSpawnPosition(_viewCamera);
            _currentBall = Instantiate(targetPrefab, position, Quaternion.identity);
            _currentFlicker = _currentBall.GetComponent<SsvepFlicker>();
            if (_currentFlicker == null)
            {
                Debug.LogError("[TrialSession] targetPrefab 缺少 SsvepFlicker 组件");
                EnterPhase(TrialPhase.Idle);
                return;
            }

            _currentFlicker.StartFlicker(defaultFrequencyHz);
            ResolveHitSource().ResetHit();
            _onsetMs = FusionLink.NowMs();
            SendUpstream("stimulus_onset", new OnsetPayload
            {
                target_id = _currentTargetId,
                freq_hz = defaultFrequencyHz,
                level = LevelName,
                wave = _wave,
            });
            EnterPhase(TrialPhase.AwaitHit);
            if (hud != null)
            {
                hud.SetPrompt("盯住闪烁的球…");
            }
        }

        private void OnHit()
        {
            double reaction = (FusionLink.NowMs() - _onsetMs) / 1000.0;
            _reactionSumSeconds += reaction;
            Hits++;
            if (_currentFlicker != null) _currentFlicker.SetSolid(hitColor);
            SendUpstream("stimulus_offset", new OffsetPayload { target_id = _currentTargetId });
            EnterPhase(TrialPhase.HitFeedback);
        }

        private void OnMiss()
        {
            Misses++;
            if (_currentFlicker != null) _currentFlicker.SetSolid(missColor);
            SendUpstream("stimulus_offset", new OffsetPayload { target_id = _currentTargetId });
            EnterPhase(TrialPhase.MissFeedback);
        }

        /// <summary>兜底（command_auto）：连续判别失败 → 目标自动完成、不计分，闭环不中断。</summary>
        private void OnAutoActivate()
        {
            if (Phase != TrialPhase.AwaitHit)
            {
                return;
            }

            if (_currentFlicker != null) _currentFlicker.SetSolid(hitColor);
            SendUpstream("stimulus_offset", new OffsetPayload { target_id = _currentTargetId });
            SendSceneEvent("auto_activated");
            BeginSpawn();
        }

        private void EndRound()
        {
            ClearCurrentBall();
            EnterPhase(TrialPhase.GameOver);
            int total = Hits + Misses;
            float rate = total > 0 ? (float)Hits / total : 0f;
            float avgReaction = Hits > 0 ? (float)(_reactionSumSeconds / Hits) : 0f;
            if (hud != null)
            {
                hud.ShowSummary(Hits, Misses, rate, avgReaction);
            }

            SendSceneEvent("session_end");
        }

        private void OnCommand(string type, FusionJson payload)
        {
            switch (type)
            {
                case "command_fire":
                    if (hitSourceMode != HitSourceMode.EegFusion || eegHitSource == null)
                    {
                        Debug.LogWarning("[TrialSession] 收到 command_fire，但命中源不是 EegFusion 模式，忽略");
                        return;
                    }

                    eegHitSource.NotifyHit(payload.Str("target_id"));
                    break;
                case "command_auto":
                    OnAutoActivate();
                    break;
                case "command_difficulty":
                    Debug.Log(
                        $"[TrialSession] 难度指令：window_s={payload.Num("window_s")} " +
                        $"tempo_ms={payload.Num("tempo_ms")} targets_n={payload.Num("targets_n")}（L2 起生效）");
                    break;
                case "command_reset":
                    BeginReady();
                    break;
            }
        }

        private IHitSource ResolveHitSource()
        {
            return hitSourceMode == HitSourceMode.EegFusion ? (IHitSource)eegHitSource : keyboardHitSource;
        }

        private bool Elapsed(float duration) => Time.time - _phaseStart >= duration;

        private void EnterPhase(TrialPhase phase)
        {
            Phase = phase;
            _phaseStart = Time.time;
        }

        private void ClearCurrentBall()
        {
            if (_currentBall != null)
            {
                Destroy(_currentBall);
            }

            _currentBall = null;
            _currentFlicker = null;
        }

        [Serializable]
        private sealed class OnsetPayload
        {
            public string target_id;
            public float freq_hz;
            public string level;
            public int wave;
        }

        [Serializable]
        private sealed class OffsetPayload
        {
            public string target_id;
        }

        [Serializable]
        private sealed class SceneEventPayload
        {
            public string level;
            public int wave;
            public string kind;
        }

        private void SendUpstream(string type, object payload)
        {
            if (fusionLink != null)
            {
                fusionLink.SendEvent(type, payload);
            }
        }

        private void SendSceneEvent(string kind)
        {
            SendUpstream("scene_event", new SceneEventPayload { level = LevelName, wave = _wave, kind = kind });
        }
    }
}
