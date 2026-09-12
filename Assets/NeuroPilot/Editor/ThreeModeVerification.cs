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
            Require(Mathf.Approximately(gate.dwellSeconds, .3f) && Mathf.Approximately(gate.leaveHysteresisSeconds, .5f) &&
                Mathf.Approximately(gate.rampSeconds, .4f) && gate.maxSimultaneous == 3, "Gate parameters wrong");
            Require(multi.gaze != null && !multi.UsesGaze && multi.director.ActiveCount == 0, "L2 room must idle until a target is fixated");
            gate.enabled = false; // 用 StepGaze 注入确定的时间步长，避免真实帧再叠加
            var events = new List<string>(); multi.OutgoingEvent += events.Add;
            var anchor = multi.Targets[0];
            gate.StepGaze(true, To(anchor), .35f);
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
            // 换邻域：从"全停"重新注视 T6（右下）→ 3 个在闪，仍两两不同频
            var swap = multi.Targets[5];
            gate.StepGaze(true, To(swap), .35f);
            var swapped = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(swapped.Count == 3 && swapped.Contains(swap), "Neighbourhood swap did not include the new anchor");
            Require(swapped.Select(t => t.Frequency).Distinct().Count() == 3, "Swapped subset reuses a frequency");
            // 抢占：池满时注视集合外的 T4（左下）→ 停掉离它最远的在闪球、纳入它，仍 ≤3 且不同频
            int offsetBefore = events.Count(e => e.Contains("stimulus_offset"));
            var preempt = multi.Targets[3];
            gate.StepGaze(true, To(preempt), .35f);
            var swapped2 = multi.Targets.Where(t => t.Stimulating).ToList();
            Require(swapped2.Count == 3 && swapped2.Contains(preempt) && swapped2.Count(t => swapped.Contains(t)) == 1,
                "Pool-full preemption did not retire the two far targets");
            Require(swapped2.Select(t => t.Frequency).Distinct().Count() == 3 &&
                events.Count(e => e.Contains("stimulus_offset")) == offsetBefore + 2, "Preemption did not release exactly the retired frequencies");
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
            yield return .65f; Room(multi, 6);
            Require(first.Id == null && first.Frequency == 0f && GameObject.Find("Success Star Burst") == null, "Replacement or reward cleanup failed");
            float spot = MinSpacing(multi, first);
            Require(Mathf.Abs(first.transform.position.z - (multi.entry.TrainingOrigin.z + multi.multiSpawnDepth)) < .05f &&
                spot >= multi.multiMinSpacing - .01f, "Respawn left the region or overlaps another target");
            Require(!multi.TryReceiveCommand(Fire(oldId, 4)), "Stale ID accepted");
            Require(events.Count(e => e.Contains("stimulus_offset") && e.Contains(oldId)) == 1, "Duplicate offset");
            anchor = first;
            gate.StepGaze(true, To(anchor), .35f);
            Require(first.Stimulating && multi.TryReceiveCommand(Fire(first.Id, 9)), "Respawned target cannot be re-armed and confirmed");
            // 眼动失效降级：超时转齐闪，恢复后自动切回门控
            gate.gazeLossFallbackSeconds = .5f;
            gate.StepGaze(false, default, .6f);
            Require(gate.FallbackActive && multi.director.ActiveCount == 3 &&
                multi.Targets.Where(t => t.Stimulating).Select(t => t.Frequency).Distinct().Count() == 3, "Gaze loss did not degrade to always-on flicker");
            gate.StepGaze(true, To(first), .25f);
            Require(!gate.FallbackActive, "Gaze recovery did not return to gating");
            multi.TestFinish(); yield return .1f; Capture("MultiResult"); Click("RestartPracticeButton");
            while (!multi.Running) yield return .1f;
            Require(multi.Hits == 0 && multi.Targets.Count == 6 && multi.director.ActiveCount == 0, "Restart failed");
            checks.Add("Six targets, two fixed rows at start and random non-overlapping respawns; gaze dwell lights only the <=3 neighbours with distinct pool frequencies; leave hysteresis keeps and then releases the frequency; re-arm keeps frequency with fresh ids; gaze cannot score, one validated EEG message scores once; gaze loss degrades to always-on and recovers.");
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
        private static void Room(TargetPracticeSession session, int count)
        {
            Require(session.Targets.Count == count && UnityEngine.Object.FindObjectOfType<SessionManager>() == null && UnityEngine.Object.FindObjectsOfType<Camera>().Length == 1, "Scene isolation/count failed");
            for (int i = 0; i < count; i++)
            {
                var target = session.Targets[i]; Require(target.transform.position.z > session.entry.TrainingOrigin.z + 3, "Not in front");
                if (!session.UsesGaze)
                {
                    var corners = new Vector3[4]; session.view.footerRoot.GetComponent<RectTransform>().GetWorldCorners(corners);
                    Require(Camera.main.WorldToScreenPoint(target.transform.position - Camera.main.transform.up * .16f).y > Camera.main.WorldToScreenPoint(corners[1]).y + 4, "Target overlaps footer");
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
        private static void Click(string name) { var obj = GameObject.Find(name); Require(obj != null, "Missing button " + name); var b = obj.GetComponent<Button>(); Require(b.IsInteractable(), "Blocked " + name); b.onClick.Invoke(); }
        private static void Capture(string name) => TrainingRoomVerification.Capture(name + "_v2.1.png");
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
