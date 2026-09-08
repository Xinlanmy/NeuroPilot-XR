using System;
using System.Linq;
using NeuroPilotXR.Navigation;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace NeuroPilotXR.Editor
{
    public static class ThreeModeSetup
    {
        public const string EyePath = "Assets/NeuroPilot/TrainingRoom/Scenes/EyeTrackingRoom.unity";
        public const string MultiPath = "Assets/NeuroPilot/TrainingRoom/Scenes/MultiTargetRoom.unity";
        public static string[] ScenePaths => new[] { TrainingRoomIntegration.NavigationPath, TrainingRoomIntegration.ScenePath, EyePath, MultiPath };
        private static TMP_FontAsset font;
        private static Sprite rounded, buttonSprite;
        private static MultiFrequencyConfig frequencyPool;

        public static void Apply()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NeuroPilot/Fonts/NotoSansSC SDF.asset");
            rounded = AssetDatabase.LoadAllAssetsAtPath("Assets/NeuroPilot/Art/RoundedRectangle.asset").OfType<Sprite>().First();
            buttonSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/NeuroPilot/Art/ButtonGradient.asset").OfType<Sprite>().First();
            const string frequencyPath = "Assets/NeuroPilot/TrainingRoom/Config/MultiFrequencyPool.asset";
            frequencyPool = AssetDatabase.LoadAssetAtPath<MultiFrequencyConfig>(frequencyPath);
            if (frequencyPool == null)
            {
                frequencyPool = ScriptableObject.CreateInstance<MultiFrequencyConfig>();
                // Prototype candidates from publieople configs/eeg_w64.toml at a85327d; NOT a final validated set.
                frequencyPool.frequencies = new[] { 12f, 10f, 8f };
                frequencyPool.prototypeEnabled = true;
                AssetDatabase.CreateAsset(frequencyPool, frequencyPath);
            }
            var singleScene = EditorSceneManager.OpenScene(TrainingRoomIntegration.ScenePath);
            var single = UnityEngine.Object.FindObjectOfType<SessionManager>();
            single.GetComponent<TrainingEegPort>().eegInputEnabled = true;
            AddReward(single.gameObject);
            var menu = single.GetComponent<TrainingRoomMenu>() ?? single.gameObject.AddComponent<TrainingRoomMenu>();
            if (single.hud.transform.Find("ReturnToModesButton") == null)
            {
                if (single.hud.GetComponent<TrackedDeviceGraphicRaycaster>() == null) single.hud.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                Button(single.hud.transform, "ReturnToModesButton", "返回模式选择", new Vector2(-350, -490), new Vector2(360, 72), menu.ReturnToModes);
            }
            EditorSceneManager.SaveScene(singleScene);
            PrepareRoom(EyePath, TrainingMode.EyeTracking);
            PrepareRoom(MultiPath, TrainingMode.MultiTarget);
            PrepareNavigation();
            EditorBuildSettings.scenes = ScenePaths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            foreach (var target in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
                var feature = settings != null ? settings.GetFeature<EyeGazeInteraction>() : null;
                if (feature == null) throw new InvalidOperationException("Missing OpenXR Eye Gaze feature for " + target);
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
            AssetDatabase.SaveAssets();
        }

        private static void PrepareNavigation()
        {
            var scene = EditorSceneManager.OpenScene(TrainingRoomIntegration.NavigationPath);
            var nav = UnityEngine.Object.FindObjectOfType<NavigationController>();
            var panel = GameObject.Find("GlassNavigationPanel").transform;
            // This builder owns only the new pages; preserve the hand-authored welcome/difficulty/drag UI.
            foreach (string name in new[] { "ModePage", "EyePage", "MultiPage", "SettingsPage" })
                if (panel.Find(name) != null) UnityEngine.Object.DestroyImmediate(panel.Find(name).gameObject);
            nav.modePage = Page(panel, "ModePage", "选择训练模式", "三个独立训练空间 · 按当前目标选择");
            Card(nav.modePage.transform, "EyeModeButton", -340, "01 / EYE GAZE", "视线追踪", "视觉 · 多彩球消除", "多个彩球同时出现\n持续注视指定目标 5 秒\n不需要脑电确认", nav.ShowEye);
            Card(nav.modePage.transform, "SingleModeButton", 0, "02 / SINGLE TARGET", "单球追踪", "脑电 · 单目标 SSVEP", "一个小球持续闪烁\n由脑电结果确认消除\n不依赖眼动或扳机", nav.ShowDifficulty);
            Card(nav.modePage.transform, "MultiModeButton", 340, "03 / MULTI TARGET", "多球定位", "脑电 · 三目标 SSVEP", "三个小球不同频率同闪\n由脑电结果确认消除\n不依赖眼动或扳机", nav.ShowMulti);
            Button(nav.modePage.transform, "BackToWelcomeButton", "返回首页", new Vector2(-370, -355), new Vector2(260, 80), nav.ShowWelcome);

            nav.eyePage = Page(panel, "EyePage", "视线追踪", "先在头显设置中开启眼动并校准 · 无需扣动扳机");
            Panel(nav.eyePage.transform, "Instructions", new Vector2(0, 95), new Vector2(1000, 330));
            Text(nav.eyePage.transform, "EyeInstructions", "场景内同时出现多个彩色小球\n\n按提示持续观察蓝色目标 5 秒，将它消去\n\n移开视线重新计时；眼动数据丢失时暂停\n\n成功：星星碎裂消失 + 短暂奖励音", new Vector2(0, 95), new Vector2(920, 310), 30);
            nav.eyeRuleLabel = null;
            Button(nav.eyePage.transform, "EyeFourButton", "4 个彩球", new Vector2(-200, -165), new Vector2(310, 82), nav.EyeFour);
            Button(nav.eyePage.transform, "EyeFiveButton", "5 个彩球", new Vector2(200, -165), new Vector2(310, 82), nav.EyeFive);
            nav.eyeCountLabel = Text(nav.eyePage.transform, "EyeCountLabel", "当前选择：同时 4 个彩球", new Vector2(0, -255), new Vector2(1060, 55), 30);
            Button(nav.eyePage.transform, "BackFromEyeButton", "返回模式选择", new Vector2(-355, -355), new Vector2(290, 80), nav.ShowModes);
            nav.eyeStart = Button(nav.eyePage.transform, "StartEyeButton", "进入视线训练", new Vector2(220, -355), new Vector2(440, 92), nav.StartEye);

            nav.multiPage = Page(panel, "MultiPage", "多球定位", "SSVEP 频率编码 · 脑电确认 · 每轮 3 分钟");
            Panel(nav.multiPage.transform, "Instructions", new Vector2(0, 80), new Vector2(1000, 280));
            Text(nav.multiPage.transform, "MultiInstructions", "三个小球同时以不同频率闪烁\n\n脑电识别出哪个目标，就消去哪一个\n\n无需眼动触发；手柄仅用于导航操作", new Vector2(0, 80), new Vector2(920, 250), 29);
            Text(nav.multiPage.transform, "FrequencyNote", "目标 1 / 2 / 3：" + string.Join(" / ", frequencyPool.frequencies.Select(f => f.ToString("0.#"))) + " Hz\n来自仓库候选配置 · 正式频率仍需脑电联调验证", new Vector2(0, -165), new Vector2(1080, 110), 27);
            Text(nav.multiPage.transform, "EegNote", "已预留脑电消息接口；当前尚未连接算法服务", new Vector2(0, -260), new Vector2(1080, 55), 25);
            nav.countLabel = null;
            nav.multiRuleLabel = null;
            nav.frequencyConfig = frequencyPool;
            Button(nav.multiPage.transform, "BackFromMultiButton", "返回模式选择", new Vector2(-355, -355), new Vector2(290, 80), nav.ShowModes);
            nav.multiStart = Button(nav.multiPage.transform, "StartMultiButton", "进入多球训练", new Vector2(220, -355), new Vector2(440, 92), nav.StartMulti);

            var difficulty = panel.Find("DifficultyPage");
            var content = difficulty.Find("DifficultyContent");
            if (content.Find("BackFromSingleButton") != null) UnityEngine.Object.DestroyImmediate(content.Find("BackFromSingleButton").gameObject);
            Button(content, "BackFromSingleButton", "返回模式选择", new Vector2(-385, -355), new Vector2(280, 80), nav.ShowModes);
            nav.singleStart = content.Find("StartTrainingButton").GetComponent<Button>();
            SetListener(nav.singleStart, nav.StartSingle);
            nav.singleContent = content.GetComponent<CanvasGroup>();
            nav.singleStatus = difficulty.Find("PreparingText").GetComponent<TMP_Text>();
            // Shift the original start button and its border/glow together to leave room for Back.
            foreach (string name in new[] { "StartTrainingButton", "StartButtonBorder", "StartButtonGlow" })
            {
                var rect = content.Find(name).GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(200, -356);
            }
            content.Find("PageSubtitle").GetComponent<TMP_Text>().text = "单球脑电 SSVEP / 选择训练等级，不使用眼动确认";
            nav.sceneTransition = UnityEngine.Object.FindObjectOfType<SceneTransitionManager>();
            nav.eyeStatus = nav.multiStatus = null; // Their page already explains readiness; room shows live countdown.
            SetListener(panel.Find("WelcomePage/EnterTrainingButton").GetComponent<Button>(), nav.ShowModes);
            PrepareSettings(panel, nav);
            nav.modePage.gameObject.SetActive(false); nav.eyePage.gameObject.SetActive(false); nav.multiPage.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PrepareSettings(Transform panel, NavigationController nav)
        {
            var welcome = panel.Find("WelcomePage");
            if (welcome.Find("SettingsButton") != null) UnityEngine.Object.DestroyImmediate(welcome.Find("SettingsButton").gameObject);
            Button(welcome, "SettingsButton", "设置", new Vector2(455, 310), new Vector2(170, 64), nav.ShowSettings);
            foreach (var label in welcome.GetComponentsInChildren<TMP_Text>(true))
                if (label.text.Contains("NEUROPILOT") || label.text.Contains("NeuroPilot XR"))
                { label.rectTransform.sizeDelta = new Vector2(700, label.rectTransform.sizeDelta.y); label.rectTransform.anchoredPosition = new Vector2(-80, 300); }
            nav.settingsPage = Page(panel, "SettingsPage", "通信设置", "脑电数据端 / WSL 通信设备");
            var page = nav.settingsPage.gameObject.AddComponent<CommunicationSettingsPage>();
            Panel(page.transform, "AddressField", new Vector2(0, 170), new Vector2(900, 95));
            page.address = Text(page.transform, "AddressText", "", new Vector2(0, 170), new Vector2(860, 82), 46);
            page.status = Text(page.transform, "SettingsStatus", "", new Vector2(0, 92), new Vector2(1080, 50), 24);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0" };
            for (int i = 0; i < keys.Length; i++)
            {
                var key = Button(page.transform, "IpKey" + i, keys[i], new Vector2(-210 + (i % 3) * 210, 15 - (i / 3) * 77), new Vector2(190, 65), page.Clear);
                key.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddStringPersistentListener(key.onClick, page.Append, keys[i]);
            }
            Button(page.transform, "IpBackspace", "退格", new Vector2(210, -216), new Vector2(190, 65), page.Backspace);
            Button(page.transform, "IpClear", "清空", new Vector2(435, -24), new Vector2(175, 70), page.Clear);
            Button(page.transform, "IpDefault", "默认地址", new Vector2(435, -120), new Vector2(175, 70), page.RestoreDefault);
            Text(page.transform, "ConnectionNotice", "地址保存不代表已连接 · 脑电网络适配器尚待接入", new Vector2(0, -285), new Vector2(1080, 48), 23);
            Button(page.transform, "BackFromSettingsButton", "返回首页", new Vector2(-320, -365), new Vector2(300, 80), nav.ShowWelcome);
            Button(page.transform, "SaveIpButton", "保存地址", new Vector2(260, -365), new Vector2(360, 80), page.Save);
            nav.settingsPage.gameObject.SetActive(false);
        }

        private static void PrepareRoom(string path, TrainingMode mode)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null && !AssetDatabase.CopyAsset(TrainingRoomIntegration.ScenePath, path))
                throw new InvalidOperationException("Could not copy white room: " + path);
            var scene = EditorSceneManager.OpenScene(path);
            var practice = UnityEngine.Object.FindObjectOfType<TargetPracticeSession>();
            if (practice == null)
            {
                var session = UnityEngine.Object.FindObjectOfType<SessionManager>();
                var hud = session.hud;
                var entry = session.GetComponent<TrainingRoomEntry>();
                var area = session.config;
                var spawner = session.spawner;
                var host = session.gameObject;
                if (hud.transform.Find("ReturnToModesButton") != null)
                    UnityEngine.Object.DestroyImmediate(hud.transform.Find("ReturnToModesButton").gameObject);
                if (host.GetComponent<TrainingRoomMenu>() != null) UnityEngine.Object.DestroyImmediate(host.GetComponent<TrainingRoomMenu>());
                if (spawner.ball != null) UnityEngine.Object.DestroyImmediate(spawner.ball.gameObject);
                UnityEngine.Object.DestroyImmediate(spawner);
                if (host.GetComponent<TrainingEegPort>() != null) UnityEngine.Object.DestroyImmediate(host.GetComponent<TrainingEegPort>());
                UnityEngine.Object.DestroyImmediate(session);
                entry.session = null; hud.session = null; hud.enabled = false;
                practice = host.AddComponent<TargetPracticeSession>();
                practice.mode = mode; practice.entry = entry; practice.view = hud;
                string configPath = "Assets/NeuroPilot/TrainingRoom/Config/" + mode + "_Area.asset";
                if (AssetDatabase.LoadAssetAtPath<SessionConfig>(configPath) == null)
                    AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(area), configPath);
                practice.area = AssetDatabase.LoadAssetAtPath<SessionConfig>(configPath);
                if (mode == TrainingMode.EyeTracking)
                {
                    practice.gaze = host.AddComponent<EyeGazeProvider>();
                    practice.gaze.origin = UnityEngine.Object.FindObjectOfType<XROrigin>();
                }
                string materialPath = "Assets/NeuroPilot/TrainingRoom/Materials/" + mode + "_Target.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(.04f, .8f, .95f); mat.SetFloat("_Smoothness", .4f);
                    AssetDatabase.CreateAsset(mat, materialPath);
                }
                practice.targetMaterial = mat;
                var canvas = hud.GetComponent<Canvas>();
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null) canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                Button(hud.transform, "ReturnToModesButton", "返回模式选择", new Vector2(-350, -490), new Vector2(360, 72), practice.ReturnToModes);
                Button(hud.transform, "RestartPracticeButton", "重新训练", new Vector2(350, -490), new Vector2(320, 72), practice.Restart);
                hud.accuracyText.transform.parent.Find("Caption").GetComponent<UnityEngine.UI.Text>().text = mode == TrainingMode.EyeTracking ? "注视占比" : "命中率";
                hud.modeText.text = mode == TrainingMode.EyeTracking ? "视线追踪" : "多球定位";
            }
            practice.reward = AddReward(practice.gameObject);
            if (mode == TrainingMode.MultiTarget)
            {
                practice.director = practice.GetComponent<SsvepTargetGroup>() ?? practice.gameObject.AddComponent<SsvepTargetGroup>();
                practice.director.config = frequencyPool;
                practice.acceptExternalConfirm = true;
            }
            if (mode == TrainingMode.EyeTracking && practice.gaze == null)
            {
                practice.gaze = practice.gameObject.AddComponent<EyeGazeProvider>();
                practice.gaze.origin = UnityEngine.Object.FindObjectOfType<XROrigin>();
            }
            if (mode == TrainingMode.MultiTarget && practice.gaze != null)
            {
                UnityEngine.Object.DestroyImmediate(practice.gaze);
                practice.gaze = null;
            }
            practice.restartButton = practice.view.transform.Find("RestartPracticeButton").GetComponent<Button>();
            practice.restartButton.interactable = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                        throw new InvalidOperationException("Missing script: " + item.name);
            EditorSceneManager.SaveScene(scene);
        }

        private static CanvasGroup Page(Transform parent, string name, string title, string subtitle)
        {
            var rect = Rect(parent, name, Vector2.zero, new Vector2(1200, 900));
            Text(rect, "Title", title, new Vector2(0, 335), new Vector2(1040, 88), 60);
            Text(rect, "Subtitle", subtitle, new Vector2(0, 265), new Vector2(1080, 60), 27);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }
        private static SuccessReward AddReward(GameObject host)
        {
            var reward = host.GetComponent<SuccessReward>() ?? host.AddComponent<SuccessReward>();
            const string path = "Assets/NeuroPilot/TrainingRoom/Materials/RewardStars.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.color = new Color(1f, .85f, .3f);
                AssetDatabase.CreateAsset(material, path);
            }
            reward.starMaterial = material;
            return reward;
        }
        private static void Card(Transform parent, string name, float x, string index, string title, string subtitle, string detail, UnityAction action)
        {
            var rect = Panel(parent, name, new Vector2(x, 0), new Vector2(312, 410));
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var b = rect.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = new Color(.3f, .85f, 1); colors.pressedColor = new Color(.2f, .5f, .85f); b.colors = colors;
            SetListener(b, action);
            Text(rect, "Index", index, new Vector2(0, 158), new Vector2(288, 40), 20);
            Text(rect, "Name", title, new Vector2(0, 85), new Vector2(290, 65), 45);
            Text(rect, "Summary", subtitle, new Vector2(0, 20), new Vector2(285, 45), 25);
            Text(rect, "Detail", detail, new Vector2(0, -89), new Vector2(285, 125), 23);
            Text(rect, "Open", "选择模式  >", new Vector2(0, -176), new Vector2(285, 40), 24);
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced;
            image.color = new Color(.025f, .11f, .28f, .97f); image.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.08f, .6f, 1f, .85f); outline.effectDistance = new Vector2(2, -2);
            return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string content, Vector2 position, Vector2 size, float pixels)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = pixels; text.text = content; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = buttonSprite;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            rect.gameObject.AddComponent<UIInteractionFeedback>();
            Text(rect, "Label", label, Vector2.zero, size - new Vector2(24, 8), 30);
            SetListener(button, action); return button;
        }
        private static void SetListener(Button button, UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
    }
}
