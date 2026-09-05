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
            File.WriteAllText("Logs/v1.1-verification.txt", report);
            Debug.Log("[TrainingRoomVerification] " + report);
            SessionState.SetInt(Pending + ".exit", success ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        private static IEnumerator<float> Verify()
        {
            yield return 1f;
            GameObject.Find("EnterTrainingButton").GetComponent<Button>().onClick.Invoke();
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
            Require(Mathf.Approximately(session.config.roundDuration, 180f) && Mathf.Approximately(session.config.flickerHz, 15f), "Source configuration altered");
            Checks.Add("Navigation buttons -> imported TrainingRoom; one camera/rig; passthrough/overlay removed; difficulty retained; source parameters preserved.");

            while (session.CurrentState != SessionManager.State.AwaitHit) yield return 0.1f;
            Require(session.spawner.ball.gameObject.activeInHierarchy, "Ball inactive");
            Require(session.spawner.ballRenderer.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/"), "Ball shader not URP");
            Debug.Log("[TrainingRoomVerification] Camera=" + Camera.main.transform.position + " forward=" + Camera.main.transform.forward + " hud=" + session.hud.transform.position + " ball=" + session.spawner.ball.position);
            Require(GameObject.Find("TunnelingVignette") == null, "Locomotion vignette must not obscure training");
            Capture("TrainingRoom_v1.1.png");
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return 0.1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(session.HitCount == 1, "Space input did not register a hit");
            Checks.Add("Active URP stimulus ball; Input System Space event increments hit count.");

            // Change only a runtime clone to exercise misses and timeout without a three-minute test.
            var config = UnityEngine.Object.Instantiate(session.config);
            session.config = config; session.spawner.config = config;
            config.maxExposeTime = 0.2f; config.hitFeedbackTime = 0.05f;
            config.missFeedbackTime = 0.05f; config.cooldownTime = 0.05f;
            while (session.MissCount == 0) yield return 0.1f;
            Checks.Add("Unanswered ball increments miss count.");
            config.roundDuration = 0.5f; config.readyCountdown = 0.1f;
            session.RestartRound();
            while (session.CurrentState != SessionManager.State.GameOver) yield return 0.1f;
            Require(session.hud.resultPanel.activeSelf && session.hud.resultText.text.Contains("本轮结束"), "Result missing");
            Require(!session.hud.modeText.gameObject.activeSelf && !session.hud.hintText.gameObject.activeSelf, "Training labels overlap result");
            Capture("TrainingRoom_Result_v1.1.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            yield return 0.03f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(session.CurrentState != SessionManager.State.GameOver && !session.hud.resultPanel.activeSelf && session.HitCount == 0, "R restart failed");
            Checks.Add("Round timeout -> result panel; Input System R event restarts and clears result/counters. No physical headset/EEG claim.");
        }

        private static void Capture(string fileName)
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
