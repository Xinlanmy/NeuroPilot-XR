using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeuroPilotXR.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeuroPilotXR.Editor
{
    [InitializeOnLoad]
    public static class ThreeModeVerification
    {
        private const string Key = "NeuroPilot.FinalModes";
        private static IEnumerator<float> steps;
        private static double next, deadline;
        private static int frame;
        private static readonly List<string> checks = new List<string>();
        static ThreeModeVerification() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run()
        {
            TrainingRoomIntegration.Prepare(); SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                CommunicationSettings.TestKeySuffix = ".verification";
                CommunicationSettings.ClearTestOverride();
                Application.runInBackground = true;
                InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                deadline = EditorApplication.timeSinceStartup + 120; steps = Verify(); EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false); EditorSceneManager.OpenScene(TrainingRoomIntegration.NavigationPath);
                EditorApplication.Exit(SessionState.GetInt(Key + ".exit", 1));
            }
        }
        private static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Timeout");
                if (Time.frameCount < frame || EditorApplication.timeSinceStartup < next) return;
                if (steps.MoveNext()) { next = EditorApplication.timeSinceStartup + steps.Current; frame = Time.frameCount + 1; }
                else Finish(true, "Final mode rules passed; Editor-injected gaze/EEG messages, no hardware validation claim.");
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }
        private static void Finish(bool success, string message)
        {
            CommunicationSettings.ClearTestOverride();
            CommunicationSettings.TestKeySuffix = "";
            EditorApplication.update -= Tick;
            string report = (success ? "PASS " : "FAIL ") + message + "\n" + string.Join("\n", checks);
            File.WriteAllText("Logs/three-modes-verification.txt", report); Debug.Log(report);
            SessionState.SetInt(Key + ".exit", success ? 0 : 1); EditorApplication.isPlaying = false;
        }
        private static IEnumerator<float> Verify()
        {
            yield return 1;
            Require(CommunicationSettings.Host == "192.168.1.131", "Default endpoint incorrect");
            Capture("WelcomeSettings"); Click("SettingsButton"); yield return .5f;
            var settingsPage = UnityEngine.Object.FindObjectOfType<CommunicationSettingsPage>();
            Capture("CommunicationSettings");
            Click("IpClear");
            foreach (char digit in "192.168.1.132")
            {
                int index = digit == '.' ? 9 : digit == '0' ? 10 : digit - '1';
                Click("IpKey" + index);
            }
            Click("SaveIpButton"); Require(CommunicationSettings.Host == "192.168.1.132", "Keypad save failed");
            settingsPage.Clear(); settingsPage.Append("999.168.1.1"); Click("SaveIpButton");
            Require(CommunicationSettings.Host == "192.168.1.132", "Invalid address overwritten saved value");
            foreach (string invalid in new[] { "", "1.2.3", "http://1.2.3.4", "1.2.3.-1", "0.0.0.0", "255.255.255.255", "1.2.3.4:8765" })
                Require(!CommunicationSettings.TrySave(invalid), "Invalid IPv4 accepted: " + invalid);
            settingsPage.Clear(); settingsPage.Append("10.0.0.2");
            Click("BackFromSettingsButton"); yield return .5f; Click("SettingsButton"); yield return .5f;
            Require(settingsPage.address.text == "192.168.1.132", "Return did not cancel unsaved draft / reload saved IP");
            var probe = new GameObject("EndpointProbe");
            var port = probe.AddComponent<TrainingEegPort>();
            Require(port.serverHost == "192.168.1.132" && port.serverPort == 8765, "Single EEG endpoint not shared");
            UnityEngine.Object.Destroy(probe);
            Click("BackFromSettingsButton"); yield return .5f;
            checks.Add("Settings: default IP, controller keypad, persistence reload, invalid rejection, cancel, single shared endpoint.");
            Click("EnterTrainingButton"); yield return .5f; Capture("Modes");
            Click("BackToWelcomeButton"); yield return .5f; Click("EnterTrainingButton"); yield return .5f;
            Click("EyeModeButton"); yield return .5f;
            Require(GameObject.Find("EyeSingleRuleButton") == null, "Obsolete 1s single-eye mode remains");
            Click("EyeFiveButton"); Capture("EyeSetup"); Click("BackFromEyeButton"); yield return .5f;
            Click("SingleModeButton"); yield return .5f;
            UnityEngine.Object.FindObjectOfType<DifficultySelector>().Select(DifficultyLevel.Advanced); Capture("SingleSetup");
            Click("BackFromSingleButton"); yield return .5f; Click("MultiModeButton"); yield return .5f;
            Require(GameObject.Find("FiveTargetsButton") == null, "EEG room offers five targets");
            Click("BackFromMultiButton"); yield return .5f; Click("MultiModeButton"); yield return .5f;
            Require(TrainingSession.EyeTargetCount == 5 && TrainingSession.SelectedDifficulty == DifficultyLevel.Advanced, "Back lost selections");
            Capture("MultiSetup"); Click("StartMultiButton");
            while (SceneManager.GetActiveScene().name != "MultiTargetRoom") yield return .1f;
            var multi = UnityEngine.Object.FindObjectOfType<TargetPracticeSession>();
            Require(multi.serverHost == "192.168.1.132" && multi.serverPort == 8765, "Multi EEG endpoint not shared across scenes");
            while (!multi.Running) yield return .1f;
            Room(multi, 6);
            var gate = multi.gate;
            Require(gate != null && gate.session == multi && gate.gaze == multi.gaze && gate.director == multi.director, "L2 gate not wired to the multi room");
            Require(Mathf.Approximately(gate.dwellSeconds, .1f) && Mathf.Approximately(gate.leaveHysteresisSeconds, .5f) &&
                Mathf.Approximately(gate.rampSeconds, .4f) && gate.maxSimultaneous == 3 &&
                Mathf.Approximately(gate.gazeRegionRadiusMeters, 1.2f), "Gate parameters wrong");
            Require(multi.gaze != null && !multi.UsesGaze && multi.director.ActiveCount == 0, "L2 room must idle until a target is fixated");
            gate.enabled = false; // 用 StepGaze 注入确定的时间步长，避免真实帧再叠加
            var anchor = multi.Targets[0];
            // ① 随机初始布局上的成员制语义：区域内有几颗闪几颗（1~3），锚点必在内、频率互异。
            //    期望值按真实位置现算，等于把 GazeSubsetGate 的选点规则在验证器里独立复算一遍。
            var expected = ExpectedRegion(multi, anchor);
            gate.StepGaze(true, To(anchor), .2f);
            var probed = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(probed.Count == expected && probed.Count >= 1 && probed.Contains(anchor),
                "Membership rule did not light exactly the gaze-region targets on the random layout (expected " + expected + ", got " + probed.Count + ")");
            Require(probed.Select(t => t.Frequency).Distinct().Count() == probed.Count, "Random-layout subset reuses a frequency");
            gate.StepGaze(false, default, .2f);
            gate.StepGaze(false, default, gate.leaveHysteresisSeconds + .1f);   // 迟滞排期在离场那一步才写入，必须再走一步才到期
            Require(multi.director.ActiveCount == 0, "Hysteresis did not release the probed subset");
            // ② 钉确定性布局后精确验证状态机（随机布局下"恰好几颗"会随布局变化，无法写死断言）
            PinClusterLayout(multi);
            Room(multi, 6, false);   // 钉住的测试布局刻意超出采样区域，只校验通用规则
            var events = new List<string>(); multi.OutgoingEvent += events.Add;
            gate.StepGaze(true, To(anchor), .2f);
            var burning = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(burning.Count == 3 && burning.Contains(anchor), "Fixation dwell did not light a 3-target subset around the anchor");
            Require(burning.Select(t => t.Frequency).Distinct().Count() == 3 &&
                burning.All(t => new[] { 12f, 10f, 8f }.Contains(t.Frequency)), "Subset frequencies are not distinct pool entries");
            Require(events.Count(e => Matches(e, "stimulus_onset", burning)) == 3, "Onset target_id/freq_hz mismatch");
            Require(events.Count(e => e.Contains("stimulus_offset")) == 0, "Spurious offset while fixating");
            Capture("MultiGated");
            // 迟滞：视线移开 0.2s 仍在闪（不还牌）；迟滞内回头不产生事件；跑满 0.5s 才停闪
            gate.StepGaze(false, default, .2f);
            Require(multi.director.ActiveCount == 3 && events.Count(e => e.Contains("stimulus_offset")) == 0, "Leave hysteresis stopped targets too early");
            var freqsBefore = burning.Select(t => t.Frequency).OrderBy(f => f).ToArray();
            gate.StepGaze(true, To(anchor), .35f);
            Require(multi.director.ActiveCount == 3 && events.Count(e => e.Contains("stimulus_offset")) == 0 &&
                multi.Targets.Where(t => t.Stimulating).Select(t => t.Frequency).OrderBy(f => f).SequenceEqual(freqsBefore), "Re-entering inside the hysteresis window changed the subset");
            gate.StepGaze(false, default, 1.2f);
            Require(multi.director.ActiveCount == 3, "Hysteresis released the frequencies before it expired");
            gate.StepGaze(false, default, .6f);
            Require(multi.director.ActiveCount == 0 && events.Count(e => e.Contains("stimulus_offset")) == 3, "Expired hysteresis did not stop every target exactly once");
            // 换簇：从"全停"重新注视簇 B 的 T6 → 3 个在闪，仍两两不同频
            var swap = multi.Targets[5];
            gate.StepGaze(true, To(swap), .2f);
            var swapped = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(swapped.Count == 3 && swapped.Contains(swap), "Neighbourhood swap did not include the new anchor");
            Require(swapped.Select(t => t.Frequency).Distinct().Count() == 3, "Swapped subset reuses a frequency");
            // 抢占：池满时整簇切换（注视簇 A 的 T1，当前在闪是簇 B 的 3 颗）
            // → 每纳入一颗都要踢掉一颗腾频率，最终 3 颗、频率互异、旧 3 颗全部收到 offset
            int offsetBefore = events.Count(e => e.Contains("stimulus_offset"));
            var preempt = multi.Targets[0];
            gate.StepGaze(true, To(preempt), .2f);
            var swapped2 = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(swapped2.Count == 3 && swapped2.Contains(preempt) && swapped2.Count(t => swapped.Contains(t)) == 0,
                "Pool-full switch did not retire the whole previous subset");
            Require(swapped2.Select(t => t.Frequency).Distinct().Count() == 3 &&
                events.Count(e => e.Contains("stimulus_offset")) == offsetBefore + 3, "Pool-full switch did not release exactly the retired frequencies (3 evictions expected)");
            // 再武装：持续在闪满 rearmSeconds → 同频、新 id（算法侧重开判定窗）
            var idsBefore = swapped2.Select(t => t.Id).ToArray();
            var freqBefore = swapped2.Select(t => t.Frequency).ToArray();
            int onsetBefore = events.Count(e => e.Contains("stimulus_onset"));
            offsetBefore = events.Count(e => e.Contains("stimulus_offset"));
            gate.StepGaze(true, To(preempt), gate.rearmSeconds + .1f);
            Require(swapped2.All(t => t.Stimulating) && swapped2.Select(t => t.Frequency).SequenceEqual(freqBefore), "Rearm changed frequency or stopped a target");
            Require(swapped2.All(t => !idsBefore.Contains(t.Id)), "Rearm did not issue fresh target ids");
            Require(events.Count(e => e.Contains("stimulus_onset")) == onsetBefore + 3 &&
                events.Count(e => e.Contains("stimulus_offset")) == offsetBefore + 3, "Rearm did not emit one offset/onset pair per target");
            // 脑电命令：眼动不得分；合法 command_fire 只计一次
            multi.TestGaze(true, To(anchor), 6);
            Require(multi.Hits == 0 && multi.reward.PlayCount == 0, "Gaze rewarded EEG target");
            Require(!multi.TryReceiveCommand("{") && !multi.TryReceiveCommand(Fire("wrong", 1)), "Invalid command accepted");
            var first = swapped2[0]; string oldId = first.Id;
            Require(multi.TryReceiveCommand(Fire(oldId, 2)), "Valid EEG message rejected");
            Require(multi.Hits == 1 && multi.reward.PlayCount == 1 && !first.Surface.enabled && !first.GetComponent<Collider>().enabled, "Reward/hide failed");
            Require(!multi.TryReceiveCommand(Fire(oldId, 3)), "Duplicate reward");
            yield return .12f; Capture("StarReward");
            var source = GameObject.Find("Success Star Burst").GetComponent<AudioSource>();
            Require(source.clip != null && source.clip.length < .5f && source.volume <= .3f, "Audio not short/quiet");
            yield return .65f; Room(multi, 6, false);   // 5 颗仍是钉住的测试球，只校验通用规则
            // 编辑器失焦时 Unity 会节流帧循环（Time.deltaTime 还受 maximumDeltaTime 限制），
            // 复现与星爆都是游戏时间驱动，不能只按墙钟时间假设它们已经发生——按状态轮询。
            float respawnDeadline = Time.realtimeSinceStartup + 3f;
            while (first.FeedbackUntil > 0f && Time.realtimeSinceStartup < respawnDeadline) yield return .1f;
            Require(first.FeedbackUntil <= 0f, "Respawn did not happen within 3s after the hit");
            Require(first.Id == null && first.Frequency == 0f && !first.Stimulating,
                "Respawned target kept id/frequency (id=" + first.Id + " freq=" + first.Frequency + " stimulating=" + first.Stimulating + ")");
            float burstDeadline = Time.realtimeSinceStartup + 2.5f;
            while (GameObject.Find("Success Star Burst") != null && Time.realtimeSinceStartup < burstDeadline) yield return .1f;
            Require(GameObject.Find("Success Star Burst") == null, "Star burst was not cleaned up within 2.5s");
            float spot = MinSpacing(multi, first);
            float originY = multi.entry.TrainingOrigin.y;
            float bandMin = originY + Mathf.Max(multi.multiSpawnYRange.x, 0.7f - originY);
            float bandMax = originY + Mathf.Min(multi.multiSpawnYRange.y, 2.8f - originY);
            Require(Mathf.Abs(first.transform.position.z - (multi.entry.TrainingOrigin.z + multi.multiSpawnDepth)) < .05f &&
                Mathf.Abs(first.transform.position.x - multi.entry.TrainingOrigin.x) <= multi.multiSpawnHalfWidth + .01f &&
                first.transform.position.y >= bandMin - .01f && first.transform.position.y <= bandMax + .01f &&
                spot >= multi.multiMinSpacing - .01f, "Respawn left the region or overlaps another target");
            Require(!multi.TryReceiveCommand(Fire(oldId, 4)), "Stale ID accepted");
            Require(events.Count(e => e.Contains("stimulus_offset") && e.Contains(oldId)) == 1, "Duplicate offset");
            anchor = first;
            gate.StepGaze(true, To(anchor), .2f);
            Require(first.Stimulating && multi.TryReceiveCommand(Fire(first.Id, 9)), "Respawned target cannot be re-armed and confirmed");
            // 眼动失效降级：超时转齐闪，恢复后自动切回门控
            gate.gazeLossFallbackSeconds = .5f;
            gate.StepGaze(false, default, .6f);
            Require(gate.FallbackActive && multi.director.ActiveCount == 3 &&
                multi.Targets.Where(t => t.Stimulating).Select(t => t.Frequency).Distinct().Count() == 3, "Gaze loss did not degrade to always-on flicker");
            var fallbackTargets = multi.Targets.Where(t => t.Stimulating).ToList();
            var fallbackIds = fallbackTargets.Select(t => t.Id).ToArray();
            var fallbackFrequencies = fallbackTargets.Select(t => t.Frequency).ToArray();
            onsetBefore = events.Count(e => e.Contains("stimulus_onset"));
            offsetBefore = events.Count(e => e.Contains("stimulus_offset"));
            gate.StepGaze(false, default, gate.rearmSeconds + .1f);
            Require(fallbackTargets.All(t => t.Stimulating) &&
                fallbackTargets.Select(t => t.Frequency).SequenceEqual(fallbackFrequencies) &&
                fallbackTargets.All(t => !fallbackIds.Contains(t.Id)) &&
                events.Count(e => e.Contains("stimulus_onset")) == onsetBefore + fallbackTargets.Count &&
                events.Count(e => e.Contains("stimulus_offset")) == offsetBefore + fallbackTargets.Count,
                "Fallback flicker did not re-arm expired episodes");
            gate.StepGaze(true, To(first), .25f);
            Require(!gate.FallbackActive, "Gaze recovery did not return to gating");
            multi.TestFinish(); yield return .1f; Capture("MultiResult"); Click("RestartPracticeButton");
            while (!multi.Running) yield return .1f;
            Require(multi.Hits == 0 && multi.Targets.Count == 6 && multi.director.ActiveCount == 0, "Restart failed");
            checks.Add("Six targets spawn randomly inside the bounded region (no overlap, hard floor 0.41 m); a 0.1 s fixation lights every target within 1.2 m of the gaze ray (1..3, anchor included, distinct pool frequencies); leave hysteresis keeps and then releases the frequency; re-arm keeps frequency with fresh ids; gaze cannot score, one validated EEG message scores once; gaze loss degrades to always-on and recovers.");
            Click("ReturnToModesButton"); while (SceneManager.GetActiveScene().name != "NeuroPilotNavigation") yield return .1f;
            yield return .8f; Click("EyeModeButton"); yield return .5f; Click("EyeFourButton"); Click("StartEyeButton");
            while (SceneManager.GetActiveScene().name != "EyeTrackingRoom") yield return .1f;
            var eye = UnityEngine.Object.FindObjectOfType<TargetPracticeSession>();
            while (!eye.entry.IsReady) yield return .1f;
            yield return .2f; Require(!eye.Running && eye.Targets.Count == 0, "Missing eye tracking fell back to head gaze"); Capture("EyeUnavailable");
            eye.gaze.TestOverride = true; eye.gaze.TestValid = true; eye.gaze.TestRay = new Ray(Camera.main.transform.position, Vector3.back);
            while (!eye.Running) yield return .1f;
            Room(eye, 4); Physics.SyncTransforms();
            Require(eye.UsesGaze && eye.ColorGaze && eye.director == null && Mathf.Approximately(eye.dwellSeconds, .8f), "Visual mode rules wrong");
            var blue = eye.Targets[0]; var ray = To(blue); int slot = blue.Slot;
            Require(Mathf.Approximately(blue.transform.localScale.x, .24f) &&
                Mathf.Approximately(blue.GetComponent<SphereCollider>().radius * 2f * blue.transform.localScale.x, .36f), "Visible dot or eye jitter tolerance wrong");
            var away = new Ray(Camera.main.transform.position, Vector3.back);
            eye.TestGaze(true, To(eye.Targets[1]), 6); Require(eye.Hits == 0, "Wrong color scored");
            eye.TestGaze(true, ray, .35f); eye.TestGaze(false, default, .1f);
            Require(eye.DwellProgress >= .4f, "Brief tracking loss discarded the whole fixation");
            eye.TestGaze(false, default, 4); Require(eye.DwellProgress <= 0f, "Sustained tracking loss failed to drain dwell");
            eye.TestGaze(true, away, 1); Require(eye.DwellProgress <= 0f, "Off-target gaze accumulated dwell");
            eye.TestGaze(true, ray, .7f); Require(eye.Hits == 0, "Reward before 0.8 seconds"); Capture("ColorGaze");
            eye.TestGaze(true, ray, .11f); Require(eye.Hits == 1 && eye.reward.PlayCount == 1, "Visual reward failed");
            yield return .05f;
            var flash = GameObject.Find("Gaze Confirmation Flash");
            Require(flash != null && flash.GetComponent<AudioSource>() != null && flash.GetComponent<AudioSource>().isPlaying, "Gaze flash or confirmation sound missing");
            eye.gaze.TestValid = false; float remaining = eye.Remaining;
            yield return .3f; Require(Mathf.Approximately(eye.Remaining, remaining), "Eye loss did not pause timer");
            yield return .3f; Room(eye, 4); Require(blue.Slot != slot && !eye.TryReceiveCommand(Fire(blue.Id, 1)), "Visual mode accepts EEG or failed replacement");
            eye.gaze.TestValid = true; eye.TestFinish(); yield return .1f; Capture("ColorResult");
            checks.Add("Visual room: four 0.24 m colored dots with 0.36 m gaze hit areas; real-eye readiness; blue dwell 0.8s; wrong color drains, brief eye loss tolerated, sustained loss clears, off-target adds nothing; timer pauses, no EEG confirmation; flash, sound and replacement.");
            Click("ReturnToModesButton"); while (SceneManager.GetActiveScene().name != "NeuroPilotNavigation") yield return .1f;
            yield return .8f; Click("EyeModeButton"); yield return .5f; Click("EyeFiveButton"); Click("StartEyeButton");
            while (SceneManager.GetActiveScene().name != "EyeTrackingRoom") yield return .1f;
            var five = UnityEngine.Object.FindObjectOfType<TargetPracticeSession>();
            while (!five.entry.IsReady) yield return .1f;
            five.gaze.TestOverride = true; five.gaze.TestValid = true; five.gaze.TestRay = new Ray(Camera.main.transform.position, Vector3.back);
            while (!five.Running) yield return .1f;
            Room(five, 5); Capture("FiveColorGaze"); checks.Add("Five-ball option exists only in visual mode. Single EEG regression runs separately.");
        }
        private static void Room(TargetPracticeSession session, int count, bool checkSpawnBounds = true)
        {
            Require(session.Targets.Count == count && UnityEngine.Object.FindObjectOfType<SessionManager>() == null && UnityEngine.Object.FindObjectsOfType<Camera>().Length == 1, "Scene isolation/count failed");
            float yMin = session.entry.TrainingOrigin.y + Mathf.Max(session.multiSpawnYRange.x, 0.7f - session.entry.TrainingOrigin.y) - .01f;
            float yMax = session.entry.TrainingOrigin.y + Mathf.Min(session.multiSpawnYRange.y, 2.8f - session.entry.TrainingOrigin.y) + .01f;
            for (int i = 0; i < count; i++)
            {
                var target = session.Targets[i]; Require(target.transform.position.z > session.entry.TrainingOrigin.z + 3, "Not in front");
                if (!session.UsesGaze)
                {
                    if (checkSpawnBounds)
                    {
                        Require(Mathf.Abs(target.transform.position.x - session.entry.TrainingOrigin.x) <= session.multiSpawnHalfWidth + .01f, "Target left the spawn region horizontally");
                        Require(target.transform.position.y >= yMin && target.transform.position.y <= yMax, "Target left the spawn region vertically");
                    }
                    var corners = new Vector3[4]; session.view.footerRoot.GetComponent<RectTransform>().GetWorldCorners(corners);
                    float footerTop = Camera.main.WorldToScreenPoint(corners[1]).y;
                    float ballY = Camera.main.WorldToScreenPoint(target.transform.position - Camera.main.transform.up * .16f).y;
                    Require(ballY > footerTop + 4f, "Target overlaps footer (ball screen y=" + ballY.ToString("0") +
                        ", footer top=" + footerTop.ToString("0") + ", pos=" + target.transform.position + ")");
                }
                for (int j = i + 1; j < count; j++) Require(Vector3.Distance(target.transform.position, session.Targets[j].transform.position) > .55f, "Targets overlap");
            }
        }
        private static string Fire(string id, int seq) => "{\"type\":\"command_fire\",\"ts\":1,\"seq\":" + seq + ",\"payload\":{\"target_id\":\"" + id + "\"}}";

        /// <summary>事件是否为指定类型、且 payload 的 target_id/freq_hz 与某个在闪球完全一致（Python 就靠这两个字段）。</summary>
        private static bool Matches(string json, string type, IEnumerable<PracticeTarget> targets)
        {
            if (!NeuroPilotXR.Training.FusionJson.TryParseEnvelope(json, out string parsed, out var envelope) || parsed != type) return false;
            var payload = envelope.Obj("payload");
            if (payload == null) return false;
            string id = payload.Str("target_id");
            float freq = (float)payload.Num("freq_hz", -1);
            foreach (var target in targets)
                if (target != null && target.Id == id && Mathf.Abs(freq - target.Frequency) < .01f) return true;
            return false;
        }

        /// <summary>该球到其它球的最小距离：复现位置不得与已有球重叠。</summary>
        private static float MinSpacing(TargetPracticeSession session, PracticeTarget self)
        {
            float gap = float.MaxValue;
            foreach (var other in session.Targets)
            {
                if (other == null || other == self || other.transform == null) continue;
                gap = Mathf.Min(gap, Vector3.Distance(self.transform.position, other.transform.position));
            }
            return gap;
        }
        private static Ray To(PracticeTarget target) => new Ray(Camera.main.transform.position, (target.transform.position - Camera.main.transform.position).normalized);

        /// <summary>随机布局下，按成员制规则推算锚点注视后应在闪的球数：min(3, 视线射线 1.2m 内的可用球数)。</summary>
        private static int ExpectedRegion(TargetPracticeSession session, PracticeTarget anchor)
        {
            int count = 1;   // 锚点必在内
            foreach (var target in session.Targets)
            {
                if (target == null || target == anchor || !target.Available) continue;
                Vector3 offset = target.transform.position - Camera.main.transform.position;
                Vector3 dir = (anchor.transform.position - Camera.main.transform.position).normalized;
                if (Vector3.Dot(offset, dir) <= 0f) continue;
                if (Vector3.Cross(dir, offset).magnitude <= session.gate.gazeRegionRadiusMeters) count++;
            }
            return Mathf.Min(3, count);
        }

        /// <summary>把 6 球钉成两个相距 3m 的 2x3 簇：簇内间距 0.6m（Room 的 pairwise 下限是 0.55m），
        /// 簇内最远两点 0.85m、两簇相距 3m——均满足/远离 1.2m 视线半径，"恰好 3 颗/换簇/抢占"可精确计算。
        /// z 在训练原点前 4.1m，y=1.0。</summary>
        private static void PinClusterLayout(TargetPracticeSession session)
        {
            Vector3 origin = session.entry.TrainingOrigin;
            for (int i = 0; i < session.Targets.Count && i < 6; i++)
            {
                bool clusterB = i >= 3;
                int k = i % 3;
                float x = clusterB ? 3f + (k % 2) * 0.6f : (k % 2) * 0.6f;
                float y = 1f + (k / 2) * 0.6f;
                session.Targets[i].transform.position = origin + new Vector3(x, y, session.multiSpawnDepth);
            }
            Physics.SyncTransforms();
        }
        private static void Click(string name) { var obj = GameObject.Find(name); Require(obj != null, "Missing button " + name); var b = obj.GetComponent<Button>(); Require(b.IsInteractable(), "Blocked " + name); b.onClick.Invoke(); }
        private static void Capture(string name) => TrainingRoomVerification.Capture(name + "_v2.1.png");
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
