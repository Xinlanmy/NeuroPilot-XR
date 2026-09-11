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
        public string serverHost => CommunicationSettings.Host;
        public int serverPort => CommunicationSettings.Port;
        private bool leaving;
        public float duration = 180f;
        public float dwellSeconds = 1f;
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
        private float dwell, validEyeTime, onTargetTime;
        private bool hasEyeSample, focused = true, started, finished;
        private long eventSequence, externalSequence = -1;
        private int mainThread;
        private readonly Color cyan = new Color(0.04f, 0.8f, 0.95f);
        private readonly Color[] palette = { new Color(.08f, .38f, 1f), new Color(1f, .12f, .23f),
            new Color(.1f, .9f, .38f), new Color(1f, .73f, .08f), new Color(.68f, .25f, 1f) };

        private IEnumerator Start()
        {
            mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            ColorGaze = mode == TrainingMode.EyeTracking;
            dwellSeconds = 5f;
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
            Hits = Misses = 0; dwell = validEyeTime = onTargetTime = 0f;
            Remaining = duration; finished = false; Running = false;
            if (restartButton != null) restartButton.interactable = false;
            view.resultPanel.SetActive(false); view.statsRoot.SetActive(true); view.footerRoot.SetActive(true);
            view.countdownRoot.SetActive(false);
            // Only the visual mode waits for eye data. EEG modes do not depend on eyes.
            StartCoroutine(Ready());
        }

        private IEnumerator Ready()
        {
            if (!UsesGaze && (director == null || director.config == null || !director.config.IsValid(3)))
            {
                view.hintText.text = "频率池尚未配置或无效：已禁止闪烁，请返回检查配置";
                yield break;
            }
            while (!focused || (UsesGaze && !gaze.TryGetRay(out _)))
            {
                view.hintText.text = "未获取有效眼动：请在头显设置中开启眼动并完成校准";
                yield return null;
            }
            view.countdownRoot.SetActive(true);
            float ready = 3f;
            while (ready > 0f)
            {
                bool valid = focused && (!UsesGaze || gaze.TryGetRay(out _));
                view.countdownText.text = valid ? Mathf.CeilToInt(ready).ToString() : "暂停";
                view.countdownText.fontSize = valid ? 104 : 60;
                view.hintText.text = valid ? "小球将在入场时的身前区域出现" : "等待有效眼动数据，倒计时暂停";
                if (valid) ready -= Time.deltaTime;
                yield return null;
            }
            view.countdownRoot.SetActive(false);
            int count = UsesGaze ? Mathf.Clamp(TrainingSession.EyeTargetCount, 4, 5) : 3;
            for (int i = 0; i < count; i++)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Practice Target " + (i + 1);
                ball.transform.SetParent(transform, true);
                ball.transform.localScale = Vector3.one * 0.32f;
                var target = ball.AddComponent<PracticeTarget>();
                target.Slot = i + 1; target.ColorIndex = i; target.Surface = ball.GetComponent<Renderer>();
                target.Surface.sharedMaterial = targetMaterial;
                if (!UsesGaze) target.Stimulus = ball.AddComponent<FrequencyStimulus>();
                targets.Add(target);
                Place(target);
            }
            if (director != null) director.SetTargets(targets);
            Running = true;
        }

        private void Update()
        {
            if (!started) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToModes();
            if (!Running) return;
            if (!focused) { dwell = 0f; return; }
            hasEyeSample = !UsesGaze || gaze.TryGetRay(out _);
            if (hasEyeSample) Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
            if (Remaining <= 0f) { Finish(); return; }
            foreach (var target in targets)
                if (target.FeedbackUntil > 0f && Time.time >= target.FeedbackUntil) Place(target);
            if (UsesGaze)
            {
                bool valid = gaze.TryGetRay(out Ray ray);
                TickGaze(valid, ray, Mathf.Min(Time.deltaTime, .1f));
                view.hintText.text = !valid ? "眼动暂不可用 · 已暂停计时，请检查佩戴或重新校准" :
                    (ColorGaze ? "请持续观察蓝色目标 5 秒  " : "持续注视小球 1 秒  ") + Mathf.RoundToInt(DwellProgress * 100f) + "%";
            }
            RefreshStats();
        }

        private void TickGaze(bool valid, Ray ray, float delta)
        {
            if (!Running || !UsesGaze) return;
            if (!valid) { dwell = 0f; return; }
            validEyeTime += delta;
            PracticeTarget target = Resolve(ray);
            if (target == null || !target.Available || (ColorGaze && target.ColorIndex != 0))
            {
                dwell = 0f;
                foreach (var item in targets) if (item.Available) item.Surface.material.color = BaseColor(item);
                return;
            }
            onTargetTime += delta;
            dwell += delta;
            target.Surface.material.color = Color.Lerp(BaseColor(target), Color.white, DwellProgress * .25f);
            if (dwell >= dwellSeconds) Hit(target);
        }

        private PracticeTarget Resolve(Ray ray)
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
            Hits++; dwell = 0f;
            if (reward != null) reward.Play(target.transform.position);
            target.Surface.enabled = false;
            target.GetComponent<Collider>().enabled = false;
        }

        private void Place(PracticeTarget target)
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
            bool found = false;
            // Fixed-depth grid slots avoid angular overlap even for five targets. Jitter within each slot.
            int slot = target.Slot - 1;
            if (!UsesGaze || ColorGaze)
            {
                Vector2[] slots = { new Vector2(-1.45f, .4f), new Vector2(1.45f, .4f),
                    new Vector2(-.85f, -.3f), new Vector2(.85f, -.3f), new Vector2(0f, .55f) };
                if (!UsesGaze) slots = new[] { new Vector2(-1.35f, .5f), new Vector2(1.35f, .5f), new Vector2(0, -.25f) };
                best = entry.TrainingOrigin + new Vector3(slots[slot].x + UnityEngine.Random.Range(-.12f, .12f),
                    slots[slot].y + UnityEngine.Random.Range(-.04f, .04f), !UsesGaze ? 4.1f : 4.7f);
                best.y = Mathf.Clamp(best.y, 0.7f, 2.8f);
                found = true;
            }
            else found = Spawner.TryPickSpawnPosition(entry.TrainingOrigin, Quaternion.identity, area, old, out best);
            if (!found) { Running = false; view.hintText.text = "靶区配置无可用位置，请返回模式选择"; return; }
            target.transform.position = best;
            target.Surface.enabled = true;
            target.GetComponent<Collider>().enabled = true;
            target.FeedbackUntil = 0f;
            target.Id = UsesGaze ? Guid.NewGuid().ToString("N") : null;
            target.Surface.material.color = BaseColor(target);
            if (UsesGaze) Emit("target_onset", target);
            else if (Running) director.Begin(target);
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
                : (director != null ? director.ActiveCount + " / 3" : "0 / 3");
        }

        private void Finish()
        {
            Running = false; finished = true; ClearTargets(); RefreshStats();
            var telemetry = GetComponent<NeuroPilotXR.Training.VrTelemetryPanel>();
            if (telemetry != null) telemetry.ShowSessionResult(Hits, Misses,
                Hits + Misses > 0 ? 100f * Hits / (Hits + Misses) : 0f, 0f);
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
            if (!value) { dwell = 0f; if (director != null) director.StopAll(); }
            else if (Running && director != null) foreach (var target in targets) director.Begin(target);
        }
        private Color BaseColor(PracticeTarget target) => ColorGaze ? palette[target.ColorIndex] : UsesGaze ? cyan : new Color(.35f, .4f, .45f);
        private void OnDisable() { Running = false; StopAllCoroutines(); ClearTargets(); }
#if UNITY_EDITOR
        public void TestGaze(bool valid, Ray ray, float delta) => TickGaze(valid, ray, delta);
        public void TestFinish() => Finish();
#endif
    }
}
