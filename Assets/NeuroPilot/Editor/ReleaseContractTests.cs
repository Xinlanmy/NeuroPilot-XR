#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NeuroPilotXR.Navigation;
using NeuroPilotXR.Training;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using VIVE.OpenXR;

namespace NeuroPilotXR.Editor
{
    public sealed class ReleaseContractTests
    {
        [Test]
        public void AndroidReleaseProfileIsFocusVisionReady()
        {
            ViveFocusVisionConfigurator.ApplyProjectProfile();
            Assert.DoesNotThrow(ViveFocusVisionConfigurator.ValidateProject);
            Assert.AreEqual(11, PlayerSettings.Android.bundleVersionCode);
            Assert.AreEqual("2.2.0", PlayerSettings.bundleVersion);
            Assert.AreEqual("NeuroPilot XR 2.2.0", PlayerSettings.productName);
            Assert.AreEqual("com.neuropilot.xr", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        }

        [Test]
        public void DragFacingRotationKeepsCanvasUprightAndFacingViewer()
        {
            Vector3 viewer = new Vector3(-1.5f, 1.7f, -2f);
            Vector3 window = new Vector3(2f, 0.8f, 3f);
            Assert.IsTrue(SpatialWindowDragController.TryCalculateFacingRotation(window, viewer, out Quaternion rotation));

            Vector3 expectedForward = Vector3.ProjectOnPlane(window - viewer, Vector3.up).normalized;
            Assert.Greater(Vector3.Dot(rotation * Vector3.forward, expectedForward), 0.9999f);
            Assert.Greater(Vector3.Dot(rotation * Vector3.up, Vector3.up), 0.9999f);
            Assert.Greater(Vector3.Dot(-(rotation * Vector3.forward), (viewer - window).normalized), 0.98f);
        }

        [Test]
        public void DragFacingRotationRejectsUndefinedHorizontalDirection()
        {
            Assert.IsFalse(SpatialWindowDragController.TryCalculateFacingRotation(
                new Vector3(1f, 3f, 2f), new Vector3(1f, 1f, 2f), out _));
        }

        [Test]
        public void ViveEyePoseIsConvertedFromOpenXrHandedness()
        {
            float half = Mathf.Sqrt(0.5f);
            var source = new XrPosef(
                new XrQuaternionf(0f, half, 0f, half),
                new XrVector3f(1f, 2f, 3f));

            Pose converted = EyeGazeProvider.ConvertVivePose(source);
            Assert.AreEqual(new Vector3(1f, 2f, -3f), converted.position);
            Assert.Greater(Vector3.Dot(converted.forward, Vector3.left), 0.9999f);
        }

        [Test]
        public void TelemetryPanelClampsInputAndSeparatesLiveAndResultViews()
        {
            var host = new GameObject("TelemetryTestHost");
            try
            {
                var panel = host.AddComponent<VrTelemetryPanel>();
                panel.SetAttention(FusionJson.TryParse("{\"score\":1.4,\"quality\":0.8,\"valid\":1}"));
                Assert.AreEqual(1f, panel.AttentionScore);
                Assert.IsTrue(panel.IsLiveTelemetryVisible);

                panel.ShowSessionResult(3, 1, 75f, float.NaN);
                Assert.IsTrue(panel.IsProfileVisible);
                Assert.IsFalse(panel.IsLiveTelemetryVisible);

                panel.HideProfile();
                Assert.IsFalse(panel.IsProfileVisible);
                Assert.IsTrue(panel.IsLiveTelemetryVisible);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SsvepFrequenciesMatchFbccaContract()
        {
            var single = AssetDatabase.LoadAssetAtPath<SessionConfig>("Assets/NeuroPilot/TrainingRoom/Config/SessionConfig_Default.asset");
            var pool = AssetDatabase.LoadAssetAtPath<MultiFrequencyConfig>("Assets/NeuroPilot/TrainingRoom/Config/MultiFrequencyPool.asset");
            Assert.AreEqual(12f, single.flickerHz);
            CollectionAssert.AreEqual(new[] { 12f, 10f, 8f }, pool.frequencies);
        }

        [TestCase("Assets/NeuroPilot/TrainingRoom/Scenes/TrainingRoom.unity")]
        [TestCase("Assets/NeuroPilot/TrainingRoom/Scenes/MultiTargetRoom.unity")]
        public void EegScenesContainTransportBridge(string path)
        {
            EditorSceneManager.OpenScene(path);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsOfType<FusionEegBridge>(true).Length);
        }

        [Test]
        public void SettingsActionsDoNotOverlap()
        {
            EditorSceneManager.OpenScene(TrainingRoomIntegration.NavigationPath);
            string[] names = { "BackFromSettingsButton", "TestConnButton", "SaveIpButton" };
            var scene = EditorSceneManager.GetActiveScene();
            var rects = names.Select(n => Resources.FindObjectsOfTypeAll<GameObject>()
                .Single(go => go.scene == scene && go.name == n).GetComponent<RectTransform>()).ToArray();
            for (int i = 0; i < rects.Length; i++)
                for (int j = i + 1; j < rects.Length; j++)
                    Assert.Greater(Mathf.Abs(rects[i].anchoredPosition.x - rects[j].anchoredPosition.x),
                        (rects[i].rect.width + rects[j].rect.width) * .5f, names[i] + " overlaps " + names[j]);
        }

        [Test]
        public void CircledAuxiliaryCopyIsAbsent()
        {
            EditorSceneManager.OpenScene(TrainingRoomIntegration.NavigationPath);
            var panel = GameObject.Find("GlassNavigationPanel").transform;
            AssertMissing(panel.Find("WelcomePage"), "BrandText", "Subtitle");
            AssertMissing(panel.Find("ModePage"), "Subtitle");
            foreach (string card in new[] { "EyeModeButton", "SingleModeButton", "MultiModeButton" })
                AssertMissing(panel.Find("ModePage/" + card), "Summary", "Detail");
            AssertMissing(panel.Find("DifficultyPage/DifficultyContent"), "PageSubtitle");
            foreach (string card in new[] { "BeginnerCard", "StandardCard", "AdvancedCard" })
                AssertMissing(panel.Find("DifficultyPage/DifficultyContent/" + card), "Description");

            foreach (string path in new[] { TrainingRoomIntegration.ScenePath, ThreeModeSetup.EyePath, ThreeModeSetup.MultiPath })
            {
                EditorSceneManager.OpenScene(path);
                var hud = UnityEngine.Object.FindObjectsOfType<TrainingHUD>(true).Single();
                AssertHidden(hud.modeText, path + " mode text");
                if (path == ThreeModeSetup.MultiPath) AssertHidden(hud.hintText, path + " hint text");
            }
        }

        private static void AssertMissing(Transform parent, params string[] names)
        {
            if (parent == null) return;
            foreach (string name in names)
                Assert.IsNull(parent.Find(name), parent.name + "/" + name + " should be removed");
        }

        private static void AssertHidden(Text text, string label)
        {
            Assert.NotNull(text, label + " is missing");
            Assert.IsTrue(string.IsNullOrEmpty(text.text), label + " is not empty");
            Assert.IsFalse(text.gameObject.activeSelf, label + " is active");
        }
    }
}
#endif
