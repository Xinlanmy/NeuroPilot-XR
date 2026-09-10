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

namespace NeuroPilotXR.Editor
{
    public sealed class ReleaseContractTests
    {
        [Test]
        public void AndroidReleaseProfileIsFocusVisionReady()
        {
            ViveFocusVisionConfigurator.ApplyProjectProfile();
            Assert.DoesNotThrow(ViveFocusVisionConfigurator.ValidateProject);
            Assert.AreEqual(7, PlayerSettings.Android.bundleVersionCode);
            Assert.AreEqual("2.0", PlayerSettings.bundleVersion);
            Assert.AreEqual("NeuroPilot XR 2.0", PlayerSettings.productName);
            Assert.AreEqual("com.neuropilot.xr", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
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
