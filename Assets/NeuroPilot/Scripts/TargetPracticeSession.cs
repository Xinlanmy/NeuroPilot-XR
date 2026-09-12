using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

namespace NeuroPilotXR.Navigation
{
    /// <summary>Independent eye/multi sessions. Shares room art, not the single-ball state machine.</summary>
    public sealed class TargetPracticeSession : MonoBehaviour
    {
        public TrainingMode mode;
        public TrainingRoomEntry entry;
        public EyeGazeProvider gaze;
        public SessionConfig area;
        public TrainingHUD view; // Reuse authored UI references; legacy HUD behaviour is disabled.
        public Material targetMaterial;
        public SuccessReward reward;
        public Button restartButton;
        public SsvepTargetGroup director;
        public GazeSubsetGate gate; // L2 门控（多球房）；眼动房为 null
        public string serverHost => CommunicationSettings.Host;
        public int serverPort => CommunicationSettings.Port;
        private bool leaving;
        public float duration = 180f;
        public float dwellSeconds = 0.8f;
        // Real eye trackers jitter by 1-2 degrees and drop out for a few frames on every blink, so a
        // fixation window that resets on a single bad sample is unreachable on hardware. Off-target
        // time first spends a small grace budget, then drains the accumulated dwell.
        public float dwellGraceSeconds = 0.3f;
        public float offTargetDecayPerSecond = 2f;
        // The visible dot stays precise; its slightly larger invisible collider absorbs eye-tracker jitter.
        public float eyeTargetDiameter = 0.24f;
        public float eyeHitDiameter = 0.36f;
        public float eyeTargetDepth = 3.6f;
        // 多球房（L2）：6 球初始两行三列，命中后在限定区域内随机复现、不与其它球重叠
        public int multiTargetCount = 6;
        public float multiSpawnHalfWidth = 1.9f;
        public Vector2 multiSpawnYRange = new Vector2(-0.4f, 1.0f); // 相对入场原点的高低偏移
        public float multiSpawnDepth = 4.1f;
        public float multiMinSpacing = 0.9f;
        public bool acceptExternalConfirm; // Reserved adapter; no network connection or EEG classifier.
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public float Remaining { get; private set; }
        public float DwellProgress => dwell / Mathf.Max(0.1f, dwellSeconds);
        public bool Running { get; private set; }
        public bool ColorGaze { get; private set; }
        public bool UsesGaze => mode == TrainingMode.EyeTracking;
        public IReadOnlyList<PracticeTarget> Targets => targets;
        public event Action<string> OutgoingEvent;
        public Func<long> SequenceProvider { get; set; }

        [Serializable] private sealed class Envelope
        {
            public string type;
            public long ts;
            public long seq = -1;
            public Payload payload;
        }
        [Serializable] private sealed class Payload
        {
            public string mode, target_id, kind;
            public float freq_hz;
            public Vector3 position;
            public int slot, level = 2, wave = 1;
        }

        private readonly List<PracticeTarget> targets = new List<PracticeTarget>();
        private float dwell, graceLeft, validEyeTime, onTargetTime;
        private bool hasEyeSample, focused = true, started, finished;
        private float focusLostAt;
        private long eventSequence, externalSequence = -1;
        private int mainThread;
        private readonly Color cyan = new Color(0.04f, 0.8f, 0.95f);
        private readonly Color[] palette = { new Color(.08f, .38f, 1f), new Color(1f, .12f, .23f),
            new Color(.1f, .9f, .38f), new Color(1f, .73f, .08f), new Color(.68f, .25f, 1f) };
        // L2 多球初始布局：两行三列（相邻角距 21°/15°，满足"同屏任两目标 ≥10–15°"的生成约束）
        private static readonly Vector2[] MultiSlots =
        {
            new Vector2(-1.6f, .85f), new Vector2(0f, .85f), new Vector2(1.6f, .85f),
            new Vector2(-1.6f, -.25f), new Vector2(0f, -.25f), new Vector2(1.6f, -.25f),
        };

        private IEnumerator Start()
        {
            mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            ColorGaze = mode == TrainingMode.EyeTracking;
            // Authored in the scene, clamped but never overwritten: the previous hard-coded 5 here is
            // why every dwellSeconds value set in a scene was ignored.
            dwellSeconds = Mathf.Max(0.2f, dwellSeconds);
            dwellGraceSeconds = Mathf.Max(0f, dwellGraceSeconds);
            offTargetDecayPerSecond = Mathf.Max(0f, offTargetDecayPerSecond);
            eyeTargetDiameter = Mathf.Clamp(eyeTargetDiameter, 0.1f, 1f);
            eyeHitDiameter = Mathf.Max(eyeTargetDiameter, eyeHitDiameter);
            eyeTargetDepth = Mathf.Max(0.55f, eyeTargetDepth);
            view.enabled = false;
            view.session = null;
            if (director != null) director.EpisodeChanged += OnEpisode;
            view.resultPanel.SetActive(false);
            view.countdownRoot.SetActive(false);
            view.statText.text = "0"; view.timeText.text = "03:00"; view.accuracyText.text = "—";
            view.modeText.text = "";
            view.modeText.gameObject.SetActive(false);
            if (!UsesGaze) view.accuracyText.transform.parent.Find("Caption").GetComponent<Text>().text = "正在闪烁";
            if (ColorGaze)
            {
                view.accuracyText.transform.parent.Find("Caption").GetComponent<Text>().text = "注视占比";
            }
            else if (gate != null)
            {
                // 门控模式要讲清楚"看哪儿哪儿才闪"，提示行必须开着（向导会把它关掉）
                view.hintText.gameObject.SetActive(true);
                view.hintText.text = "";
            }
            else
            {
                view.hintText.text = "";
                view.hintText.gameObject.SetActive(false);
            }
            while (!entry.IsReady) { view.hintText.text = "正在定位白色训练房间…"; yield return null; }
            started = true;
            ResetRound();
        }

        private void ResetRound()
        {
            ClearTargets();
            var telemetry = GetComponent<NeuroPilotXR.Training.VrTelemetryPanel>();
            if (telemetry != null) telemetry.HideProfile();
            Hits = Misses = 0; dwell = graceLeft = validEyeTime = onTargetTime = 0f;
            Remaining = duration; finished = false; Running = false;
            if (restartButton != null) restartButton.interactable = false;
            view.resultPanel.SetActive(false); view.statsRoot.SetActive(true); view.footerRoot.SetActive(true);
            view.countdownRoot.SetActive(false);
            // Only the visual mode waits for eye data. EEG modes do not depend on eyes.
            StartCoroutine(Ready());
        }

        private IEnumerator Ready()
        {
            if (!UsesGaze && (director == null || director.config == null || !director.config.HasUsablePool))
            {
                view.hintText.gameObject.SetActive(true);
                view.hintText.text = "频率池尚未配置或无效：已禁止闪烁，请返回检查配置";
                yield break;
            }
            while (!focused || (UsesGaze && (gaze == null || !gaze.TryGetRay(out _))))
            {
                // Ship the runtime readout with the failure: on device this is the only way to tell a
                // missing extension apart from a calibration problem.
                view.hintText.text = "未获取有效眼动：请在头显设置中开启眼动并完成校准" + GazeDiagnostic();
                yield return null;
            }
            view.countdownRoot.SetActive(true);
            float ready = 3f;
            while (ready > 0f)
            {
                bool valid = focused && (!UsesGaze || (gaze != null && gaze.TryGetRay(out _)));
                view.countdownText.text = valid ? Mathf.CeilToInt(ready).ToString() : "暂停";
                view.countdownText.fontSize = valid ? 104 : 60;
                view.hintText.text = valid ? "小球将在入场时的身前区域出现" :
                    "等待有效眼动数据，倒计时暂停" + GazeDiagnostic();
                if (valid) ready -= Time.deltaTime;
                yield return null;
            }
            view.countdownRoot.SetActive(false);
            int count = UsesGaze ? Mathf.Clamp(TrainingSession.EyeTargetCount, 4, 5)
                : Mathf.Clamp(multiTargetCount, 4, MultiSlots.Length);
            for (int i = 0; i < count; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Practice Target " + (i + 1);
                ball.transform.SetParent(transform, true);
                ball.transform.localScale = Vector3.one * (UsesGaze ? eyeTargetDiameter : 0.32f);
                if (UsesGaze) ball.GetComponent<SphereCollider>().radius = eyeHitDiameter / (2f * eyeTargetDiameter);
                var target = ball.AddComponent<PracticeTarget>();
                target.Slot = i + 1; target.ColorIndex = i; target.Surface = ball.GetComponent<Renderer>();
                target.Surface.sharedMaterial = targetMaterial;
                if (!UsesGaze) target.Stimulus = ball.AddComponent<FrequencyStimulus>();
                targets.Add(target);
                Place(target, true);
            }
            if (director != null) director.SetTargets(targets);
            // 未挂门控的老多球场景：退回 2.1 的"全量齐闪"，避免房间整场不闪
            if (!UsesGaze && gate == null) BeginAllAvailable();
            Running = true;
        }

        /// <summary>兜底齐闪/眼动失效齐闪的共用路径：可用球逐个取号起闪（门控在场时由它驱动）。</summary>
        private void BeginAllAvailable()
        {
            if (director == null || director.config == null || !director.config.HasUsablePool) return;
            foreach (var target in targets)
            {
                if (target == null || target.Stimulating || !target.Available) continue;
                if (director.config.TryAssign(director.TakenFrequencies(), out float hz))
                    director.Begin(target, hz);
            }
        }

        private void Update()
        {
            if (!started) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToModes();
            if (!Running) return;
            if (!focused)
            {
                // Losing focus (headset off, another app in front) is just another gap in the data:
                // grace first, drain after, instead of an instant reset.
                DrainOffTarget(Mathf.Min(Time.deltaTime, .1f));
                return;
            }
            hasEyeSample = !UsesGaze || (gaze != null && gaze.TryGetRay(out _));
            if (hasEyeSample) Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
            if (Remaining <= 0f) { Finish(); return; }
            foreach (var target in targets)
                if (target.FeedbackUntil > 0f && Time.time >= target.FeedbackUntil) Place(target);
            if (UsesGaze)
            {
                Ray ray = default;
                bool valid = gaze != null && gaze.TryGetRay(out ray);
                TickGaze(valid, ray, Mathf.Min(Time.deltaTime, .1f));
                view.hintText.text = !valid
                    ? "眼动暂不可用 · 已暂停计时，请检查佩戴或重新校准" + GazeDiagnostic()
                    : (ColorGaze ? "请持续观察蓝色目标 " : "持续注视小球 ") + dwellSeconds.ToString("0.0") + " 秒  " +
                      Mathf.RoundToInt(DwellProgress * 100f) + "%";
            }
            else if (gate != null) view.hintText.text = gate.Status;
            RefreshStats();
        }

        private void TickGaze(bool valid, Ray ray, float delta)
        {
            if (!Running || !UsesGaze) return;
            PracticeTarget target = valid ? ResolveGazeTarget(ray) : null;
            bool onTarget = target != null && target.Available && (!ColorGaze || target.ColorIndex == 0);
            if (valid) validEyeTime += delta;
            if (onTarget)
            {
                graceLeft = dwellGraceSeconds; // Locked on: refill the forgiveness budget.
                onTargetTime += delta;
                dwell += delta;
                float progress = Mathf.Clamp01(DwellProgress);
                target.transform.localScale = Vector3.one * eyeTargetDiameter * (1f + .18f * progress);
                target.Surface.material.color = Color.Lerp(BaseColor(target), Color.white, progress * .55f);
                if (dwell >= dwellSeconds) Hit(target);
                return;
            }
            DrainOffTarget(delta);
            foreach (var item in targets) if (item.Available)
            {
                item.transform.localScale = Vector3.one * eyeTargetDiameter;
                item.Surface.material.color = BaseColor(item);
            }
        }

        /// <summary>Spends the grace budget first, then removes accumulated dwell.</summary>
        private void DrainOffTarget(float delta)
        {
            float forgiven = Mathf.Min(graceLeft, delta);
            graceLeft -= forgiven;
            float drain = delta - forgiven;
            if (drain > 0f) dwell = Mathf.Max(0f, dwell - drain * offTargetDecayPerSecond);
        }

        private string GazeDiagnostic()
        {
            return UsesGaze && gaze != null ? "\n" + gaze.Diagnostic : string.Empty;
        }

        /// <summary>射线命中的练习球（眼动 dwell 与 L2 门控共用；门控的锚点也走这里）。</summary>
        public PracticeTarget ResolveGazeTarget(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore)) return null;
            var target = hit.collider.GetComponent<PracticeTarget>();
            return target != null && targets.Contains(target) ? target : null;
        }

        private void Hit(PracticeTarget target)
        {
            if (!Running || !target.Available) return;
            target.FeedbackUntil = Time.time + 0.35f; // Latch before callbacks; reentrant command handlers cannot score twice.
            Emit("target_confirmed", target);
            if (UsesGaze) Emit("target_offset", target); else director.Stop(target);
            target.Id = null;
            Hits++; dwell = 0f; graceLeft = 0f;
            if (reward != null)
            {
                if (UsesGaze) reward.PlayGaze(target.transform.position, BaseColor(target), eyeTargetDiameter);
                else reward.Play(target.transform.position);
            }
            target.Surface.enabled = false;
            target.GetComponent<Collider>().enabled = false;
        }

        /// <summary>摆放/复现：initial = 回合开始的固定两行三列；否则区域内随机且不与其它球重叠。</summary>
        private void Place(PracticeTarget target, bool initial = false)
        {
            if (ColorGaze && target.ColorIndex == 0 && target.FeedbackUntil > 0 && targets.Count > 1)
            {
                var other = targets[UnityEngine.Random.Range(1, targets.Count)];
                int previousSlot = target.Slot; target.Slot = other.Slot; other.Slot = previousSlot;
                Emit("target_offset", other);
                Place(other);
            }
            Vector3 old = target.transform.position;
            Vector3 best = default;
            bool found;
            // Fixed-depth grid slots avoid angular overlap even for five targets. Jitter within each slot.
            int slot = target.Slot - 1;
            if (!UsesGaze)
            {
                if (!initial && TryPickMultiSpot(target, out best)) found = true;
                else
                {
                    Vector2 home = MultiSlots[Mathf.Clamp(slot, 0, MultiSlots.Length - 1)];
                    best = entry.TrainingOrigin + new Vector3(home.x, home.y, multiSpawnDepth);
                    best.y = Mathf.Clamp(best.y, 0.7f, 2.8f);
                    found = true;
                }
            }
            else if (ColorGaze)
            {
                Vector2[] slots = { new Vector2(-1.45f, .4f), new Vector2(1.45f, .4f),
                    new Vector2(-.85f, -.3f), new Vector2(.85f, -.3f), new Vector2(0f, .55f) };
                best = entry.TrainingOrigin + new Vector3(slots[slot].x + UnityEngine.Random.Range(-.12f, .12f),
                    slots[slot].y + UnityEngine.Random.Range(-.04f, .04f), eyeTargetDepth);
                best.y = Mathf.Clamp(best.y, 0.7f, 2.8f);
                found = true;
            }
            else found = Spawner.TryPickSpawnPosition(entry.TrainingOrigin, Quaternion.identity, area, old, out best);
            if (!found) { Running = false; view.hintText.text = "靶区配置无可用位置，请返回模式选择"; return; }
            target.transform.position = best;
            if (UsesGaze) target.transform.localScale = Vector3.one * eyeTargetDiameter;
            target.Surface.enabled = true;
            target.GetComponent<Collider>().enabled = true;
            target.FeedbackUntil = 0f;
            target.Id = UsesGaze ? Guid.NewGuid().ToString("N") : null;
            target.Surface.material.color = BaseColor(target);
            if (UsesGaze) Emit("target_onset", target);
            else if (Running && gate == null) BeginAllAvailable();   // 有门控时由 gate 决定何时再起闪
        }

        /// <summary>
        /// 门控多球复现：限定区域内随机取点，与其它球留出 multiMinSpacing 的不重叠间距；
        /// 随机采样不达标时退到固定槽位里第一个达标者，最后才用"间距最大的随机点"兜底。
        /// </summary>
        private bool TryPickMultiSpot(PracticeTarget target, out Vector3 best)
        {
            best = default;
            Vector3 fallback = default;
            float bestGap = -1f;
            for (int i = 0; i < 40; i++)
            {
                Vector3 candidate = entry.TrainingOrigin + new Vector3(
                    UnityEngine.Random.Range(-multiSpawnHalfWidth, multiSpawnHalfWidth),
                    UnityEngine.Random.Range(multiSpawnYRange.x, multiSpawnYRange.y),
                    multiSpawnDepth);
                candidate.y = Mathf.Clamp(candidate.y, 0.7f, 2.8f);
                float gap = MinGapToOthers(candidate, target);
                if (gap >= multiMinSpacing) { best = candidate; return true; }
                if (gap > bestGap) { bestGap = gap; fallback = candidate; }
            }
            for (int i = 0; i < MultiSlots.Length; i++)
            {
                Vector2 home = MultiSlots[(Mathf.Clamp(target.Slot - 1, 0, MultiSlots.Length - 1) + i) % MultiSlots.Length];
                Vector3 candidate = entry.TrainingOrigin + new Vector3(home.x, home.y, multiSpawnDepth);
                candidate.y = Mathf.Clamp(candidate.y, 0.7f, 2.8f);
                if (MinGapToOthers(candidate, target) >= multiMinSpacing) { best = candidate; return true; }
            }
            if (bestGap <= 0f) return false;
            best = fallback;
            return true;
        }

        private float MinGapToOthers(Vector3 candidate, PracticeTarget self)
        {
            float gap = float.MaxValue;
            foreach (var other in targets)
            {
                if (other == null || other == self || other.transform == null) continue;
                gap = Mathf.Min(gap, Vector3.Distance(candidate, other.transform.position));
            }
            return gap;
        }

        public bool TryConfirmExternalTarget(string targetId, long sequence)
        {
            if (!acceptExternalConfirm || !Running || !focused || mode != TrainingMode.MultiTarget || UsesGaze ||
                System.Threading.Thread.CurrentThread.ManagedThreadId != mainThread || sequence <= externalSequence) return false;
            var target = targets.Find(t => t.Id == targetId && t.Id != null && t.Available && t.Stimulating);
            if (target == null) return false;
            externalSequence = sequence; Hit(target); return true;
        }

        public bool TryReceiveCommand(string json)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != mainThread || string.IsNullOrEmpty(json) || json.Length > 8192) return false;
            Envelope command;
            try { command = JsonUtility.FromJson<Envelope>(json); } catch (ArgumentException) { return false; }
            return command != null && command.type == "command_fire" && command.ts > 0 && command.seq >= 0 && command.payload != null &&
                TryConfirmExternalTarget(command.payload.target_id, command.seq);
        }
        public void ResetConnectionSequence() { if (System.Threading.Thread.CurrentThread.ManagedThreadId == mainThread) externalSequence = -1; }
        private void OnEpisode(PracticeTarget target, bool onset) => Emit(onset ? "stimulus_onset" : "stimulus_offset", target);

        private void Emit(string type, PracticeTarget target)
        {
            if (string.IsNullOrEmpty(target.Id)) return;
            string wireType = !UsesGaze && type == "target_confirmed" ? "scene_event" : type;
            string json = JsonUtility.ToJson(new Envelope { type = wireType,
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), seq = SequenceProvider != null ? SequenceProvider() : eventSequence++, payload = new Payload { mode = mode.ToString(),
                target_id = target.Id, slot = target.Slot, position = target.transform.position,
                freq_hz = target.Frequency, kind = type == "target_confirmed" ? "target_confirmed" : null } });
            if (OutgoingEvent == null) return;
            foreach (Action<string> listener in OutgoingEvent.GetInvocationList())
                try { listener(json); } catch (Exception error) { Debug.LogException(error); }
        }

        private void RefreshStats()
        {
            view.statText.text = Hits.ToString();
            view.timeText.text = TimeSpan.FromSeconds(Mathf.CeilToInt(Remaining)).ToString(@"mm\:ss");
            view.accuracyText.text = UsesGaze
                ? (validEyeTime <= 0 ? "—" : (100f * onTargetTime / validEyeTime).ToString("F0") + "%")
                : (director != null ? director.ActiveCount + " / " + Mathf.Max(1, gate != null ? gate.maxSimultaneous : 3) : "0 / 3");
        }

        private void Finish()
        {
            Running = false; finished = true; ClearTargets(); RefreshStats();
            var telemetry = GetComponent<NeuroPilotXR.Training.VrTelemetryPanel>();
            if (telemetry != null) telemetry.ShowSessionResult(Hits, Misses,
                Hits + Misses > 0 ? 100f * Hits / (Hits + Misses) : 0f, float.NaN);
            if (restartButton != null) restartButton.interactable = true;
            view.resultPanel.SetActive(true); view.countdownRoot.SetActive(false);
            view.statsRoot.SetActive(false); view.footerRoot.SetActive(false);
            view.resultText.text = "训练完成\n\n<size=60>" + (mode == TrainingMode.EyeTracking ? "视线追踪" : "多球定位") + "</size>\n\n确认目标  " + Hits +
                (UsesGaze ? "\n有效眼动中的注视占比  " + view.accuracyText.text : "\n脑电确认次数，不代表分类准确率") +
                "\n\n可重新训练，或返回模式选择";
        }

        public void Restart() { if (started && finished) ResetRound(); }
        public void ReturnToModes()
        {
            if (leaving) return;
            leaving = true;
            Running = false; StopAllCoroutines(); ClearTargets();
            TrainingSession.ReturnToModes = true;
            SceneManager.LoadSceneAsync("NeuroPilotNavigation");
        }
        private void ClearTargets()
        {
            if (director != null) director.StopAll();
            foreach (var target in targets)
            {
                if (target == null) continue;
                if (UsesGaze) Emit("target_offset", target);
                Destroy(target.Surface.material);
                Destroy(target.gameObject);
            }
            targets.Clear();
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value)
            {
                focusLostAt = Time.unscaledTime;
                if (director != null) director.StopAll();
            }
            else
            {
                // A pause long enough that Update stopped advancing is a real interruption and clears
                // the fixation; anything shorter is absorbed by the ordinary grace budget.
                if (Time.unscaledTime - focusLostAt > 0.5f) { dwell = 0f; graceLeft = 0f; }
                // 门控模式下全量补闪会打乱子集与频率分配，交给 gate 下一帧按注视重建
                if (Running && director != null && gate == null) BeginAllAvailable();
            }
        }
        private Color BaseColor(PracticeTarget target) => ColorGaze ? palette[target.ColorIndex] : UsesGaze ? cyan : new Color(.35f, .4f, .45f);
        private void OnDisable() { Running = false; StopAllCoroutines(); ClearTargets(); }
#if UNITY_EDITOR
        public void TestGaze(bool valid, Ray ray, float delta) => TickGaze(valid, ray, delta);
        public void TestFinish() => Finish();
#endif
    }
}
