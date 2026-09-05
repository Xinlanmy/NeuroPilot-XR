using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Rendering.Universal;
using Unity.XR.CoreUtils;
using NeuroPilotXR.Navigation;

namespace NeuroPilotXR.Editor
{
    public static class TrainingRoomIntegration
    {
        public const string ScenePath = "Assets/NeuroPilot/TrainingRoom/Scenes/TrainingRoom.unity";
        public const string NavigationPath = "Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity";
        private const string Root = "Assets/NeuroPilot/TrainingRoom";
        private const string RigPath = "Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/Prefabs/XR Interaction Setup.prefab";
        private const string ApkPath = "Builds/Android/NeuroPilotXR_1.1.0.apk";

        [MenuItem("NeuroPilot/Training Room/Integrate Imported Scene")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
            var converted = new Dictionary<Material, Material>();
            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                // Only migrate imported room materials, never XR helper shaders on subsequent runs.
                if (renderer.GetComponentInParent<XROrigin>() != null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    if (original == null || original.shader == null) continue;
                    if (original.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    if (!converted.TryGetValue(original, out var material))
                    {
                        bool unlit = original.shader.name.StartsWith("Unlit/");
                        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                        if (shader == null) throw new InvalidOperationException("URP shader unavailable.");
                        material = new Material(shader) { name = "TrainingRoom_" + renderer.name.Replace(" ", "_") };
                        material.SetColor("_BaseColor", original.color);
                        if (!unlit)
                        {
                            material.SetFloat("_Smoothness", original.HasProperty("_Glossiness") ? original.GetFloat("_Glossiness") : 0.15f);
                            material.SetFloat("_Metallic", 0f);
                        }
                        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Root + "/Materials/" + material.name + ".mat"));
                        converted.Add(original, material);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }

            var oldRig = GameObject.Find("PlayerRig");
            if (oldRig != null) UnityEngine.Object.DestroyImmediate(oldRig);
            var rig = UnityEngine.Object.FindObjectOfType<XROrigin>();
            if (rig == null)
            {
                var setup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPath));
                setup.name = "Training XR Interaction Setup";
                rig = setup.GetComponentInChildren<XROrigin>(true);
            }
            if (rig == null || rig.Camera == null) throw new InvalidOperationException("Training XR camera missing.");
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>(true))
            {
                var prefabRenderer = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                if (prefabRenderer != null) renderer.sharedMaterials = prefabRenderer.sharedMaterials;
            }
            rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, 2.5f), Quaternion.identity);
            var camera = rig.Camera;
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null) camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            foreach (var provider in UnityEngine.Object.FindObjectsOfType<LocomotionProvider>(true)) provider.enabled = false;
            foreach (var child in rig.GetComponentsInChildren<Transform>(true))
                if (child.name == "TunnelingVignette") child.gameObject.SetActive(false);

            var session = UnityEngine.Object.FindObjectOfType<SessionManager>();
            var spawner = UnityEngine.Object.FindObjectOfType<Spawner>();
            var hud = UnityEngine.Object.FindObjectOfType<TrainingHUD>();
            if (session == null || spawner == null || hud == null) throw new InvalidOperationException("Imported training dependencies missing.");
            spawner.viewCamera = camera;
            var entry = session.GetComponent<TrainingRoomEntry>();
            if (entry == null) entry = session.gameObject.AddComponent<TrainingRoomEntry>();
            entry.origin = rig; entry.session = session; entry.hud = hud.transform;

            var hudRect = (RectTransform)hud.transform;
            hudRect.sizeDelta = new Vector2(1600f, 900f);
            hudRect.localScale = Vector3.one * 0.001f;
            hudRect.SetPositionAndRotation(new Vector3(0f, 1.6f, 4.6f), Quaternion.identity);
            hud.GetComponent<Canvas>().worldCamera = camera;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/NeuroPilot/Fonts/NotoSansSC-Variable.ttf");
            Format(hud.timeText, font, new Vector2(0f, 330f), new Vector2(500f, 90f), 70, Color.black);
            Format(hud.statText, font, new Vector2(-520f, 315f), new Vector2(500f, 80f), 44, Color.black);
            Format(hud.hintText, font, new Vector2(0f, -280f), new Vector2(1400f, 80f), 44, Color.black);
            if (hud.modeText == null)
            {
                var mode = new GameObject("ModeText", typeof(RectTransform), typeof(Text));
                mode.transform.SetParent(hud.transform, false);
                hud.modeText = mode.GetComponent<Text>();
            }
            Format(hud.modeText, font, new Vector2(0f, 235f), new Vector2(1500f, 70f), 38, Color.black);
            var result = (RectTransform)hud.resultPanel.transform;
            result.anchoredPosition = Vector2.zero; result.sizeDelta = new Vector2(1100f, 700f);
            Format(hud.resultText, font, Vector2.zero, new Vector2(1040f, 650f), 48, Color.white);
            ValidateScene(scene);
            EditorSceneManager.SaveScene(scene);

            scene = EditorSceneManager.OpenScene(NavigationPath, OpenSceneMode.Single);
            var transition = UnityEngine.Object.FindObjectOfType<SceneTransitionManager>();
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("trainingSceneName").stringValue = "TrainingRoom";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(NavigationPath, true), new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.bundleVersion = "1.1.0";
            PlayerSettings.Android.bundleVersionCode = 3;
            AssetDatabase.SaveAssets();
            // Remove only obsolete adapter-generated helper materials; prefab shader references are restored above.
            AssetDatabase.DeleteAsset(Root + "/Materials/TrainingRoom_TunnelingVignette.mat");
            AssetDatabase.DeleteAsset(Root + "/Materials/TrainingRoom_Teleport_Interactor.mat");
            Debug.Log("[TrainingRoomIntegration] Prepared 1.1.0 navigation -> TrainingRoom; source stimulus parameters preserved.");
        }

        private static void Format(Text text, Font font, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            text.font = font; text.fontSize = fontSize; text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.rectTransform.localScale = Vector3.one;
            text.rectTransform.localRotation = Quaternion.identity;
        }

        private static void ValidateScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                        throw new InvalidOperationException("Missing script: " + t.name);
            if (UnityEngine.Object.FindObjectsOfType<Camera>().Length != 1 || UnityEngine.Object.FindObjectsOfType<XROrigin>().Length != 1)
                throw new InvalidOperationException("Training scene must have one active camera and XR Origin.");
        }

        public static void BuildAndroid()
        {
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { NavigationPath, ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.DetailedBuildReport
            });
            Debug.Log("[TrainingRoomIntegration] BUILD " + report.summary.result + " errors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android build failed.");
        }
    }
}
