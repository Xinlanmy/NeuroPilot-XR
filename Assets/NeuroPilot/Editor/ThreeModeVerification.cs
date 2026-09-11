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
            Room(multi, 3);
            Require(multi.gaze == null && !multi.UsesGaze && multi.director.ActiveCount == 3, "EEG mode depends on gaze or does not start all balls");
            Require(multi.Targets.Select(t => t.Frequency).SequenceEqual(new[] { 12f, 10f, 8f }), "Wrong frequencies");
            var positions = multi.Targets.Select(t => t.transform.position).ToArray(); var rotation = Camera.main.transform.rotation;
            Camera.main.transform.rotation = Quaternion.Euler(0, 180, 0); yield return .1f;
            Require(multi.Targets.Select(t => t.transform.position).SequenceEqual(positions) && multi.director.ActiveCount == 3, "Head turn changes EEG targets");
            Camera.main.transform.rotation = rotation; Physics.SyncTransforms(); Capture("MultiFrequency");
            multi.TestGaze(true, To(multi.Targets[0]), 6);
            Require(multi.Hits == 0 && multi.reward.PlayCount == 0, "Gaze rewarded EEG target");
            Require(!multi.TryReceiveCommand("{") && !multi.TryReceiveCommand(Fire("wrong", 1)), "Invalid command accepted");
            var events = new List<string>(); multi.OutgoingEvent += events.Add;
            var first = multi.Targets[0]; string oldId = first.Id;
            Require(multi.TryReceiveCommand(Fire(oldId, 2)), "Valid EEG message rejected");
            Require(multi.Hits == 1 && multi.reward.PlayCount == 1 && !first.Surface.enabled && !first.GetComponent<Collider>().enabled, "Reward/hide failed");
            Require(!multi.TryReceiveCommand(Fire(oldId, 3)), "Duplicate reward");
            yield return .12f; Capture("StarReward");
            var source = GameObject.Find("Success Star Burst").GetComponent<AudioSource>();
            Require(source.clip != null && source.clip.length < .5f && source.volume <= .3f, "Audio not short/quiet");
            yield return .65f; Room(multi, 3);
            Require(first.Id != oldId && first.Frequency == 12 && multi.director.ActiveCount == 3 && GameObject.Find("Success Star Burst") == null, "Replacement or reward cleanup failed");
            Require(!multi.TryReceiveCommand(Fire(oldId, 4)) && !multi.TryReceiveCommand(Fire(first.Id, 2)), "Stale ID/sequence accepted");
            Require(events.Count(e => e.Contains("stimulus_offset") && e.Contains(oldId)) == 1, "Duplicate offset");
            multi.TestFinish(); yield return .1f; Capture("MultiResult"); Click("RestartPracticeButton");
            while (!multi.Running) yield return .1f;
            Require(multi.Hits == 0 && multi.Targets.Count == 3 && multi.director.ActiveCount == 3, "Restart failed");
            checks.Add("All Back routes and selections; 3 simultaneous 12/10/8 Hz targets without gaze; gaze cannot score; validated EEG message scores once; paired offset, reward, replacement, restart.");
            Click("ReturnToModesButton"); while (SceneManager.GetActiveScene().name != "NeuroPilotNavigation") yield return .1f;
            yield return .8f; Click("EyeModeButton"); yield return .5f; Click("EyeFourButton"); Click("StartEyeButton");
            while (SceneManager.GetActiveScene().name != "EyeTrackingRoom") yield return .1f;
            var eye = UnityEngine.Object.FindObjectOfType<TargetPracticeSession>();
            while (!eye.entry.IsReady) yield return .1f;
            yield return .2f; Require(!eye.Running && eye.Targets.Count == 0, "Missing eye tracking fell back to head gaze"); Capture("EyeUnavailable");
            eye.gaze.TestOverride = true; eye.gaze.TestValid = true; eye.gaze.TestRay = new Ray(Camera.main.transform.position, Vector3.back);
            while (!eye.Running) yield return .1f;
            Room(eye, 4); Physics.SyncTransforms();
            Require(eye.UsesGaze && eye.ColorGaze && eye.director == null && eye.dwellSeconds == 5, "Visual mode rules wrong");
            var blue = eye.Targets[0]; var ray = To(blue); int slot = blue.Slot;
            eye.TestGaze(true, To(eye.Targets[1]), 6); Require(eye.Hits == 0, "Wrong color scored");
            eye.TestGaze(true, ray, 3); eye.TestGaze(false, default, .1f); eye.TestGaze(true, ray, 3); Require(eye.Hits == 0, "Tracking loss failed to reset dwell");
            eye.TestGaze(true, new Ray(Camera.main.transform.position, Vector3.back), .1f);
            eye.TestGaze(true, ray, 4.8f); Require(eye.Hits == 0, "Reward before five seconds"); Capture("ColorGaze");
            eye.TestGaze(true, ray, .21f); Require(eye.Hits == 1 && eye.reward.PlayCount == 1, "Visual reward failed");
            eye.gaze.TestValid = false; float remaining = eye.Remaining;
            yield return .3f; Require(Mathf.Approximately(eye.Remaining, remaining), "Eye loss did not pause timer");
            yield return .3f; Room(eye, 4); Require(blue.Slot != slot && !eye.TryReceiveCommand(Fire(blue.Id, 1)), "Visual mode accepts EEG or failed replacement");
            eye.gaze.TestValid = true; eye.TestFinish(); yield return .1f; Capture("ColorResult");
            checks.Add("Visual room: four colored balls; real-eye readiness; blue dwell 5s; wrong color/interruption reset, data loss pauses; no EEG confirmation; reward and replacement.");
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
        private static Ray To(PracticeTarget target) => new Ray(Camera.main.transform.position, (target.transform.position - Camera.main.transform.position).normalized);
        private static void Click(string name) { var obj = GameObject.Find(name); Require(obj != null, "Missing button " + name); var b = obj.GetComponent<Button>(); Require(b.IsInteractable(), "Blocked " + name); b.onClick.Invoke(); }
        private static void Capture(string name) => TrainingRoomVerification.Capture(name + "_v2.0.1.png");
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
