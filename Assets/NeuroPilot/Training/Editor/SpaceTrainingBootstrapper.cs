using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 一键搭建 SSVEP 训练场景：目标球预制体 + 训练组件接线 + HUD。
    /// 菜单：NeuroPilot → 搭建 SSVEP 训练场景（在 SpaceTraining 场景中执行）。
    /// 幂等：重复执行会复用已有对象，只补缺失部分。可 Ctrl+Z 撤销。
    /// </summary>
    public static class SpaceTrainingBootstrapper
    {
        private const string OrbPrefabPath = "Assets/NeuroPilot/Prefabs/SSVEP_Target.prefab";
        private const string OrbMaterialPath = "Assets/NeuroPilot/Art/SSVEP_Orb.mat";

        [MenuItem("NeuroPilot/搭建 SSVEP 训练场景 (SpaceTraining)")]
        public static void Bootstrap()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "SpaceTraining" &&
                !EditorUtility.DisplayDialog(
                    "SpaceTraining Bootstrapper",
                    $"当前场景是「{scene.name}」，此工具应在 SpaceTraining 场景中执行。\n仍要继续吗？",
                    "继续", "取消"))
            {
                return;
            }

            Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                EditorUtility.DisplayDialog(
                    "SpaceTraining Bootstrapper",
                    "场景中找不到 Camera。请确认 XR Origin（含 tagged MainCamera 的相机）存在后再运行。",
                    "确定");
                return;
            }

            GameObject orbPrefab = EnsureOrbPrefab();
            TrainingHud hud = EnsureHud(cam);
            EnsureTrainingRoot(orbPrefab, hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            EditorUtility.DisplayDialog(
                "SpaceTraining Bootstrapper",
                "搭建完成：\n" +
                "• Prefabs/SSVEP_Target.prefab（0.25m 球 + SsvepFlicker，默认 15Hz）\n" +
                "• TrainingRoot（状态机/出球器/FusionLink/键盘与 EEG 命中源，已接线）\n" +
                "• TrainingHUD（时间/统计/提示/结算面板）\n\n" +
                "Editor 按 Play 用空格模拟命中；联调先起融合层 ws_server。",
                "确定");
        }

        private static void EnsureTrainingRoot(GameObject orbPrefab, TrainingHud hud)
        {
            var root = GameObject.Find("TrainingRoot");
            if (root == null)
            {
                root = new GameObject("TrainingRoot");
            }

            Undo.RegisterCreatedObjectUndo(root, "SSVEP TrainingRoot");

            var manager = root.GetComponent<TrialSessionManager>();
            if (manager == null) manager = root.AddComponent<TrialSessionManager>();
            var spawner = root.GetComponent<TargetSpawner>();
            if (spawner == null) spawner = root.AddComponent<TargetSpawner>();
            var fusion = root.GetComponent<FusionLink>();
            if (fusion == null) fusion = root.AddComponent<FusionLink>();
            var keyboard = root.GetComponent<KeyboardHitSource>();
            if (keyboard == null) keyboard = root.AddComponent<KeyboardHitSource>();
            var eeg = root.GetComponent<EegHitSource>();
            if (eeg == null) eeg = root.AddComponent<EegHitSource>();

            var so = new SerializedObject(manager);
            so.FindProperty("targetPrefab").objectReferenceValue = orbPrefab;
            so.FindProperty("spawner").objectReferenceValue = spawner;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("fusionLink").objectReferenceValue = fusion;
            so.FindProperty("keyboardHitSource").objectReferenceValue = keyboard;
            so.FindProperty("eegHitSource").objectReferenceValue = eeg;
            so.ApplyModifiedProperties();
        }

        private static GameObject EnsureOrbPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/NeuroPilot", "Prefabs");
            EnsureFolder("Assets/NeuroPilot", "Art");

            var material = AssetDatabase.LoadAssetAtPath<Material>(OrbMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                AssetDatabase.CreateAsset(material, OrbMaterialPath);
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "SSVEP_Target";
            sphere.transform.localScale = Vector3.one * 0.25f; // 直径 0.25m
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            var collider = sphere.GetComponent<Collider>();
            if (collider != null) collider.enabled = false; // 纯视觉刺激，无物理需求
            sphere.AddComponent<SsvepFlicker>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sphere, OrbPrefabPath);
            Object.DestroyImmediate(sphere);
            return prefab;
        }

        private static TrainingHud EnsureHud(Camera cam)
        {
            var existing = Object.FindFirstObjectByType<TrainingHud>();
            if (existing != null)
            {
                return existing;
            }

            var canvasGo = new GameObject("TrainingHUD", typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(canvasGo, "SSVEP TrainingHUD");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(1920, 1080);
            canvasGo.transform.localScale = Vector3.one * 0.001f; // 1920px → 1.92m
            canvasGo.transform.SetPositionAndRotation(
                cam.transform.position + cam.transform.forward * 2f,
                Quaternion.LookRotation(cam.transform.forward, Vector3.up));

            var time = CreateText(canvasGo.transform, "Time", new Vector2(0.5f, 1f), new Vector2(800, 120), new Vector2(0, -90), 72, TextAlignmentOptions.Center, Color.white);
            var stats = CreateText(canvasGo.transform, "Stats", new Vector2(0f, 1f), new Vector2(640, 100), new Vector2(420, -90), 48, TextAlignmentOptions.Left, Color.white);
            var prompt = CreateText(canvasGo.transform, "Prompt", new Vector2(0.5f, 0f), new Vector2(1600, 100), new Vector2(0, 160), 44, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.85f));

            var panel = new GameObject("SummaryPanel", typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "SSVEP SummaryPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);
            image.raycastTarget = false;
            var summary = CreateText(panel.transform, "SummaryText", new Vector2(0.5f, 0.5f), new Vector2(1400, 700), Vector2.zero, 56, TextAlignmentOptions.Center, Color.white);
            panel.SetActive(false);

            var hud = canvasGo.AddComponent<TrainingHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("timeText").objectReferenceValue = time;
            so.FindProperty("statsText").objectReferenceValue = stats;
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("summaryPanel").objectReferenceValue = panel;
            so.FindProperty("summaryText").objectReferenceValue = summary;
            so.ApplyModifiedProperties();
            return hud;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, Vector2 anchorPoint, Vector2 size, Vector2 position,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "SSVEP " + name);
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.text = name;
            var rt = tmp.rectTransform;
            rt.anchorMin = anchorPoint;
            rt.anchorMax = anchorPoint;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return tmp;
        }

        private static void EnsureFolder(string parent, string leaf)
        {
            string path = parent + "/" + leaf;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
