using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using NeuroPilotXR.Navigation;

namespace NeuroPilotXR.Editor
{
    // Batch entry: -executeMethod NeuroPilotXR.Editor.TrainingRoomVerification.Run (without -quit).
    [InitializeOnLoad]
    public static class TrainingRoomVerification
    {
        private const string Pending = "NeuroPilot.TrainingRoomVerification";
        private static IEnumerator<float> routine;
        private static double next, deadline;
        private static int nextFrame;
        private static Keyboard keyboard;
        private static readonly List<string> Checks = new List<string>();

        static TrainingRoomVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        public static void Run()
        {
            TrainingRoomIntegration.Prepare();
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.runInBackground = true;
                InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                routine = Verify();
                deadline = EditorApplication.timeSinceStartup + 100;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                EditorSceneManager.OpenScene(TrainingRoomIntegration.NavigationPath);
                EditorApplication.Exit(SessionState.GetInt(Pending + ".exit", 1));
            }
        }

        private static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Timed out");
                if (Time.frameCount < nextFrame || EditorApplication.timeSinceStartup < next) return;
                if (routine.MoveNext())
                {
                    next = EditorApplication.timeSinceStartup + routine.Current;
                    nextFrame = Time.frameCount + 1;
                    return;
                }
                Finish(true, "All checks passed");
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        private static void Finish(bool success, string message)
        {
            EditorApplication.update -= Tick;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            string report = (success ? "PASS" : "FAIL") + ": " + message + "\n" + string.Join("\n", Checks);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/v" + TrainingRoomIntegration.Version + "-verification.txt", report);
            Debug.Log("[TrainingRoomVerification] " + report);
            SessionState.SetInt(Pending + ".exit", success ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        private static IEnumerator<float> Verify()
        {
            yield return 1f;
            GameObject.Find("EnterTrainingButton").GetComponent<Button>().onClick.Invoke();
            yield return 0.6f;
            GameObject.Find("SingleModeButton").GetComponent<Button>().onClick.Invoke();
            yield return 0.6f;
            UnityEngine.Object.FindObjectOfType<DifficultySelector>().Select(DifficultyLevel.Advanced);
            GameObject.Find("StartTrainingButton").GetComponent<Button>().onClick.Invoke();
            while (SceneManager.GetActiveScene().name != "TrainingRoom") yield return 0.1f;
            var session = UnityEngine.Object.FindObjectOfType<SessionManager>();
            Require(session != null, "Imported session missing");
            while (!session.GetComponent<TrainingRoomEntry>().IsReady) yield return 0.1f;
            yield return 1f;
            Require(UnityEngine.Object.FindObjectsOfType<Camera>().Length == 1, "Multiple active cameras");
            Require(UnityEngine.Object.FindObjectsOfType<XROrigin>().Length == 1, "Multiple active rigs");
            Require(UnityEngine.Object.FindObjectOfType<SceneTransitionManager>() == null, "Transition overlay not cleaned up");
            Require(UnityEngine.Object.FindObjectOfType<VivePassthroughManager>() == null, "Navigation passthrough persisted");
            Require(TrainingSession.SelectedDifficulty == DifficultyLevel.Advanced && session.hud.modeText.text.Contains("挑战"), "Difficulty not retained");
            Require(session.hud.modeText.text.Contains("TrainingRoom v" + TrainingRoomIntegration.Version), "Training scene version label missing");
            Require(Mathf.Approximately(session.config.roundDuration, 180f) && Mathf.Approximately(session.config.flickerHz, 12f), "Source configuration altered");
            VerifySpawnPositions(session.config);
            Require(session.spawner.HasTrainingFrame, "Fixed field was not captured on entry");
            Require(session.hud.countdownRoot.activeSelf, "Ready countdown missing");
            Capture("TrainingRoom_Ready_v" + TrainingRoomIntegration.Version + ".png");
            Require(session.hud.countdownText.cachedTextGenerator.vertexCount >= 4, "Ready number produced no text geometry");
            Checks.Add("Navigation buttons -> imported TrainingRoom; one camera/rig; passthrough/overlay removed; difficulty retained; source parameters preserved.");

            while (session.CurrentState != SessionManager.State.AwaitHit) yield return 0.1f;
            Require(session.spawner.ball.gameObject.activeInHierarchy, "Ball inactive");
            Require(session.spawner.ballRenderer.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/"), "Ball shader not URP");
            Debug.Log("[TrainingRoomVerification] Camera=" + Camera.main.transform.position + " forward=" + Camera.main.transform.forward + " hud=" + session.hud.transform.position + " ball=" + session.spawner.ball.position);
            Require(GameObject.Find("TunnelingVignette") == null, "Locomotion vignette must not obscure training");
            Require(!session.hud.countdownRoot.activeSelf && session.hud.accuracyText.text == "—", "Countdown or initial accuracy incorrect");
            var headPose = Camera.main.transform.rotation;
            var field = session.spawner.TrainingOrigin;
            var ballPosition = session.spawner.ball.position;
            Camera.main.transform.rotation = Quaternion.Euler(0, 180, 0);
            yield return 0.05f;
            Require(session.spawner.TrainingOrigin == field && session.spawner.ball.position == ballPosition, "Head turn moved field or existing ball");
            Camera.main.transform.rotation = headPose;
            foreach (var controller in UnityEngine.Object.FindObjectOfType<XROrigin>().GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(true))
                Require(!controller.hideControllerModel, "Training controller model must remain enabled");
            foreach (var visual in UnityEngine.Object.FindObjectOfType<XROrigin>().GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>(true))
                Require(visual.enabled, "Training controller ray must remain enabled");
            Capture("TrainingRoom_v" + TrainingRoomIntegration.Version + ".png");
            Require(session.hud.timeText.cachedTextGenerator.vertexCount >= 4 && session.hud.statText.cachedTextGenerator.vertexCount >= 4 && session.hud.accuracyText.cachedTextGenerator.vertexCount >= 4, "Stats values produced no text geometry");
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return 0.1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(session.HitCount == 0, "Keyboard incorrectly confirmed an EEG target");
            Require(session.EegPort.eegInputEnabled && session.EegPort.TryReceiveCommand(FireJson(session.EegPort.CurrentTargetId, 0)), "Single EEG command rejected");
            yield return .1f;
            Require(session.HitCount == 1, "EEG confirmation did not register a hit");
            Require(session.hud.statText.text == "1" && session.hud.accuracyText.text == "100%", "Stats cards not updated");
            Checks.Add("Single SSVEP: Space cannot confirm; only valid EEG target command increments count and produces reward.");

            // Change only a runtime clone to exercise misses and timeout without a three-minute test.
            var config = UnityEngine.Object.Instantiate(session.config);
            session.config = config; session.spawner.config = config;
            config.readyCountdown = 0.1f;
            var port = session.EegPort;
            Require(port != null && port.eegInputEnabled && port.serverPort == CommunicationSettings.Port && port.serverHost == CommunicationSettings.Host, "EEG shared settings incorrect");
            port.ResetConnectionSequence();
            var events = new List<TrainingEegPort.Envelope>();
            port.OutgoingMessage += json => events.Add(JsonUtility.FromJson<TrainingEegPort.Envelope>(json));
            port.eegInputEnabled = true;
            session.RestartRound();
            while (session.CurrentState != SessionManager.State.AwaitHit) yield return 0.05f;
            string firstTarget = port.CurrentTargetId;
            Require(!port.TryReceiveCommand("invalid JSON"), "Malformed command accepted");
            Require(!port.TryReceiveCommand(FireJson("wrong-target", 0)), "Wrong target accepted");
            Require(!port.TryReceiveCommand("{\"type\":\"command_reset\",\"ts\":1,\"seq\":0,\"payload\":{}}"), "Unimplemented reset changed training");
            Require(port.TryReceiveCommand(FireJson(firstTarget, 0)), "Nested command payload not accepted");
            Require(!port.TryReceiveCommand(FireJson(firstTarget, 0)), "Duplicate accepted");
            yield return 0.05f;
            Require(session.HitCount == 1 && port.CurrentTargetId == null, "EEG adapter hit not closed");
            while (session.CurrentState != SessionManager.State.AwaitHit) yield return 0.05f;
            string secondTarget = port.CurrentTargetId;
            Require(secondTarget != firstTarget, "Target ID reused");
            Require(!port.TryReceiveCommand(FireJson(firstTarget, 1)), "Stale target accepted");
            Require(!port.TryReceiveCommand(FireJson(secondTarget, 0)), "Regressed sequence accepted");
            session.RestartRound();
            foreach (var onset in events.FindAll(e => e.type == "stimulus_onset"))
                Require(events.FindAll(e => e.type == "stimulus_offset" && e.payload.target_id == onset.payload.target_id).Count == 1, "Unpaired stimulus lifecycle");
            port.eegInputEnabled = false;
            session.RestartRound();
            Checks.Add("EEG seam only (no socket): EEG-only input; nested JSON fire; reject malformed/wrong/stale/duplicate/unsupported commands; unique target IDs; onset/offset paired on hit and reset.");
            config.maxExposeTime = 0.2f; config.hitFeedbackTime = 0.05f;
            config.missFeedbackTime = 0.05f; config.cooldownTime = 0.05f;
            while (session.MissCount == 0) yield return 0.1f;
            Require(session.hud.accuracyText.text == "0%", "Miss accuracy incorrect");
            Checks.Add("Unanswered ball increments miss count.");
            config.roundDuration = 0.5f; config.readyCountdown = 0.1f;
            session.RestartRound();
            while (session.CurrentState != SessionManager.State.GameOver) yield return 0.1f;
            Require(session.hud.resultPanel.activeSelf && session.hud.resultText.text.Contains("本轮结束"), "Result missing");
            Require(!session.hud.modeText.gameObject.activeSelf && !session.hud.hintText.gameObject.activeSelf, "Training labels overlap result");
            Capture("TrainingRoom_Result_v" + TrainingRoomIntegration.Version + ".png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            yield return 0.03f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(session.CurrentState != SessionManager.State.GameOver && !session.hud.resultPanel.activeSelf && session.HitCount == 0, "R restart failed");
            Checks.Add("Round timeout -> result panel; Input System R event restarts and clears result/counters. No physical headset/EEG claim.");
        }

        private static string FireJson(string target, long sequence)
        {
            return JsonUtility.ToJson(new TrainingEegPort.Envelope { type = "command_fire", ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), seq = sequence,
                payload = new TrainingEegPort.Payload { target_id = target } });
        }

        private static void VerifySpawnPositions(SessionConfig config)
        {
            var state = UnityEngine.Random.state;
            try
            {
                Require(config.spawnZRange == new Vector2(3.5f, 8.5f), "Updated material depth range missing");
                UnityEngine.Random.InitState(112);
                Vector3 origin = new Vector3(0, 1.6f, 2.5f);
                Vector3 last = Vector3.positiveInfinity;
                float smallest = 99, largest = 0;
                for (int sample = 0; sample < 600; sample++)
                {
                    Require(Spawner.TryPickSpawnPosition(origin, Quaternion.identity, config, last, out var point), "Fixed-field sampling failed");
                    Vector3 local = point - origin;
                    Require(local.z >= 3.5f && local.z <= 8.5f, "Depth outside updated material range");
                    Require(Mathf.Abs(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg) <= 22.01f, "Point outside fixed frontal region");
                    Require(point.x >= -3 && point.x <= 3 && point.y >= 1.3f && point.y <= 2.2f && point.z >= 6 && point.z <= 11, "Sphere outside fixed room field");
                    Require(Vector3.Distance(point, last) > 1.2f, "Repeated target too close");
                    smallest = Mathf.Min(smallest, local.z); largest = Mathf.Max(largest, local.z);
                    last = point;
                }
                Require(smallest < 4 && largest > 8, "Depth sampling collapsed to one point");
                // A real camera can rotate/translate but is deliberately NOT an input
                // to the fixed-field sampler. Repeat a seeded sequence after turning.
                var cam = Camera.main;
                var savedPosition = cam.transform.position;
                var savedRotation = cam.transform.rotation;
                try
                {
                    UnityEngine.Random.InitState(18);
                    Spawner.TryPickSpawnPosition(origin, Quaternion.identity, config, last, out var baseline);
                    for (int yaw = 0; yaw < 360; yaw += 45)
                    {
                        cam.transform.SetPositionAndRotation(savedPosition + Vector3.right * 0.3f, Quaternion.Euler(15, yaw, 0));
                        UnityEngine.Random.InitState(18);
                        Require(Spawner.TryPickSpawnPosition(origin, Quaternion.identity, config, last, out var actual) && actual == baseline, "Head pose affects new ball placement");
                    }
                }
                finally { cam.transform.SetPositionAndRotation(savedPosition, savedRotation); }
                Checks.Add("600 fixed-frontal samples at 3.5–8.5m; room bounds and inter-ball spacing; 8 subsequent head poses produce identical seeded positions (no following head turns).");
            }
            finally { UnityEngine.Random.state = state; }
        }

        internal static void Capture(string fileName)
        {
            Canvas.ForceUpdateCanvases();
            var camera = Camera.main;
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var target = new RenderTexture(1440, 900, 24);
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            try
            {
                // Explicit mono request avoids a stale XR view matrix on a machine without a headset.
                camera.ResetWorldToCameraMatrix();
                camera.ResetProjectionMatrix();
                camera.aspect = 1440f / 900f;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply();
                Directory.CreateDirectory("Assets/NeuroPilot/Previews");
                File.WriteAllBytes("Assets/NeuroPilot/Previews/" + fileName, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
