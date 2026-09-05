using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using NeuroPilotXR.Navigation;

namespace NeuroPilotXR.Editor
{
    public static class NeuroPilotProjectBuilder
    {
        private const string Root = "Assets/NeuroPilot";
        private const string SceneFolder = Root + "/Scenes";
        private const string ArtFolder = Root + "/Art";
        private const string SettingsFolder = Root + "/Settings";
        private const string FontSourcePath = Root + "/Fonts/NotoSansSC-Variable.ttf";
        private const string FontAssetPath = Root + "/Fonts/NotoSansSC SDF.asset";
        private const string RoundedAssetPath = ArtFolder + "/RoundedRectangle.asset";
        private const string PanelGradientAssetPath = ArtFolder + "/PanelGradient.asset";
        private const string ButtonGradientAssetPath = ArtFolder + "/ButtonGradient.asset";
        private const string NotchAssetPath = ArtFolder + "/TopNotch.asset";
        private const string NavigationScenePath = SceneFolder + "/NeuroPilotNavigation.unity";
        private const string TrainingScenePath = SceneFolder + "/SpaceTraining.unity";
        private const string XrSetupPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/Prefabs/XR Interaction Setup.prefab";

        private static TMP_FontAsset fontAsset;
        private static Sprite roundedSprite;
        private static Sprite panelGradientSprite;
        private static Sprite buttonGradientSprite;
        private static Sprite topNotchSprite;
        private static Material floorMaterial;
        private static Material blueOrbMaterial;
        private static Material warmOrbMaterial;

        [MenuItem("NeuroPilot/Build Complete MVP")]
        public static void BuildAll()
        {
            EnsureFolder(Root);
            EnsureFolder(SceneFolder);
            EnsureFolder(ArtFolder);
            EnsureFolder(SettingsFolder);
            ConfigureUrp();
            ConfigureProject();
            fontAsset = CreateOrLoadFont();
            roundedSprite = CreateOrLoadRoundedSprite();
            panelGradientSprite = CreatePanelGradientSprite();
            buttonGradientSprite = CreateButtonGradientSprite();
            topNotchSprite = CreateTopNotchSprite();
            CreateMaterials();
            BuildNavigationScene();
            if (!File.Exists(TrainingRoomIntegration.ScenePath))
                BuildTrainingScene();
            ConfigureBuildSettings();
            ConfigureOpenXR();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(NavigationScenePath, OpenSceneMode.Single);
            Debug.Log("[NeuroPilot] Complete MVP created successfully.");
        }

        private static void ConfigureUrp()
        {
            const string rendererPath = SettingsFolder + "/NeuroPilotRenderer.asset";
            const string pipelinePath = SettingsFolder + "/NeuroPilotURP.asset";

            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "NeuroPilot URP";
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
        }

        private static void ConfigureProject()
        {
            PlayerSettings.productName = "NeuroPilot XR";
            PlayerSettings.companyName = "NeuroPilot";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.neuropilot.xr");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        }

        private static TMP_FontAsset CreateOrLoadFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
                return existing;

            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (source == null)
                throw new InvalidOperationException("Chinese font source is missing at " + FontSourcePath);

            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source);
            created.name = "NotoSansSC SDF";
            created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            created.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(created, FontAssetPath);

            if (created.atlasTexture != null && !AssetDatabase.Contains(created.atlasTexture))
            {
                created.atlasTexture.name = "NotoSansSC SDF Atlas";
                AssetDatabase.AddObjectToAsset(created.atlasTexture, created);
            }

            if (created.material != null && !AssetDatabase.Contains(created.material))
            {
                created.material.name = "NotoSansSC SDF Material";
                AssetDatabase.AddObjectToAsset(created.material, created);
            }

            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static Sprite CreateOrLoadRoundedSprite()
        {
            Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoundedAssetPath);
            if (existingTexture != null)
            {
                Sprite existingSprite = AssetDatabase.LoadAllAssetsAtPath(RoundedAssetPath).OfType<Sprite>().FirstOrDefault();
                if (existingSprite != null)
                    return existingSprite;
            }

            const int size = 96;
            const float radius = 22f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.name = "RoundedRectangleTexture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x - (size - 1) * 0.5f) - ((size - 1) * 0.5f - radius);
                    float py = Mathf.Abs(y - (size - 1) * 0.5f) - ((size - 1) * 0.5f - radius);
                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(0.5f - outside));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, RoundedAssetPath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(28f, 28f, 28f, 28f));
            sprite.name = "RoundedRectangle";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static Sprite CreatePanelGradientSprite()
        {
            const int width = 512;
            const int height = 384;
            const float radius = 28f;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (width - 1f);
                    Color color = Color.Lerp(
                        new Color(0.001f, 0.005f, 0.022f, 0.98f),
                        new Color(0.002f, 0.024f, 0.075f, 0.98f),
                        Mathf.SmoothStep(0f, 1f, v));

                    float radialDistance = new Vector2((u - 0.50f) * 1.1f, (v - 0.58f) * 0.9f).magnitude;
                    float radial = Mathf.Pow(Mathf.Clamp01(1f - radialDistance * 1.55f), 2f);
                    color.r += 0.002f * radial;
                    color.g += 0.012f * radial;
                    color.b += 0.038f * radial;

                    float diagonal = Mathf.Clamp01(1f - Mathf.Abs((u - 0.12f) - v * 0.55f) * 7f);
                    color.r += 0.001f * diagonal;
                    color.g += 0.005f * diagonal;
                    color.b += 0.015f * diagonal;
                    color.a *= RoundedAlpha(x, y, width, height, radius);
                    pixels[y * width + x] = color;
                }
            }

            return CreateSpriteAsset(PanelGradientAssetPath, "PanelGradient", width, height, pixels);
        }

        private static Sprite CreateButtonGradientSprite()
        {
            const int width = 512;
            const int height = 128;
            const float radius = 26f;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (width - 1f);
                    Color color = Color.Lerp(
                        new Color(0.004f, 0.045f, 0.34f, 0.98f),
                        new Color(0.055f, 0.48f, 1f, 0.98f),
                        Mathf.Pow(v, 0.8f));

                    float centerGlow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 1.7f), 2f);
                    color.r += 0.05f * centerGlow * v;
                    color.g += 0.08f * centerGlow * v;
                    color.b += 0.08f * centerGlow * v;

                    float leftFacet = u < 0.18f ? Mathf.Clamp01(1f - Mathf.Abs(v - (0.12f + u * 2.6f)) * 10f) : 0f;
                    float rightFacet = u > 0.82f ? Mathf.Clamp01(1f - Mathf.Abs(v - (0.88f - (u - 0.82f) * 2.6f)) * 10f) : 0f;
                    float facet = Mathf.Max(leftFacet, rightFacet);
                    color += new Color(0.10f, 0.18f, 0.22f, 0f) * facet;
                    color.a *= RoundedAlpha(x, y, width, height, radius);
                    pixels[y * width + x] = color;
                }
            }

            return CreateSpriteAsset(ButtonGradientAssetPath, "ButtonGradient", width, height, pixels);
        }

        private static Sprite CreateTopNotchSprite()
        {
            const int width = 320;
            const int height = 32;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (width - 1f);
                    float edge = Mathf.Min(u, 1f - u);
                    float taper = edge < 0.08f ? (0.08f - edge) * 2.8f : 0f;
                    bool inside = v > 0.22f + taper && v < 0.72f;
                    float core = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 3.2f), 3f);
                    Color color = Color.Lerp(
                        new Color(0.01f, 0.24f, 0.85f, 1f),
                        new Color(0.35f, 0.88f, 1f, 1f),
                        core);
                    color.a = inside ? 1f : 0f;
                    pixels[y * width + x] = color;
                }
            }

            return CreateSpriteAsset(NotchAssetPath, "TopNotch", width, height, pixels);
        }

        private static Sprite CreateSpriteAsset(string path, string name, int width, int height, Color[] pixels)
        {
            AssetDatabase.DeleteAsset(path);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.name = name + " Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static float RoundedAlpha(int x, int y, int width, int height, float radius)
        {
            float px = Mathf.Abs(x - (width - 1) * 0.5f) - ((width - 1) * 0.5f - radius);
            float py = Mathf.Abs(y - (height - 1) * 0.5f) - ((height - 1) * 0.5f - radius);
            float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
            return Mathf.Clamp01(0.5f - outside);
        }

        private static void CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
                throw new InvalidOperationException("URP Lit shader was not found.");

            floorMaterial = GetOrCreateMaterial(ArtFolder + "/Floor.mat", lit, new Color(0.025f, 0.035f, 0.055f, 1f), 0.15f, 0.72f);
            blueOrbMaterial = GetOrCreateMaterial(ArtFolder + "/BlueOrb.mat", lit, new Color(0.08f, 0.42f, 0.92f, 1f), 0f, 0.82f);
            warmOrbMaterial = GetOrCreateMaterial(ArtFolder + "/WarmOrb.mat", lit, new Color(0.92f, 0.32f, 0.12f, 1f), 0f, 0.78f);
            SetEmission(blueOrbMaterial, new Color(0.05f, 0.35f, 1.15f, 1f));
            SetEmission(warmOrbMaterial, new Color(1.1f, 0.22f, 0.04f, 1f));
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color color, float metallic, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetEmission(Material material, Color color)
        {
            if (!material.HasProperty("_EmissionColor"))
                return;

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            EditorUtility.SetDirty(material);
        }

        private static void BuildNavigationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "NeuroPilotNavigation";
            SetupSharedScene(out Camera camera);
            camera.backgroundColor = Color.clear;

            Light navigationLight = UnityEngine.Object.FindObjectOfType<Light>();
            if (navigationLight != null)
                UnityEngine.Object.DestroyImmediate(navigationLight.gameObject);

            GameObject backdrop = CreateNavigationBackdrop();
            backdrop.SetActive(false);

            GameObject passthroughObject = new GameObject("PassthroughManager");
            VivePassthroughManager passthrough = passthroughObject.AddComponent<VivePassthroughManager>();

            GameObject root = new GameObject("MRNavigationRoot");
            GameObject panel = new GameObject(
                "GlassNavigationPanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster),
                typeof(Rigidbody),
                typeof(XRGrabInteractable),
                typeof(SpatialWindowDragController));
            panel.transform.SetParent(root.transform, false);
            SetLayerRecursively(panel, LayerMask.NameToLayer("UI"));

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(1200f, 900f);
            panelRect.localScale = Vector3.one * 0.001f;
            panelRect.position = new Vector3(0f, 1.55f, 1.5f);
            panelRect.rotation = Quaternion.identity;

            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            scaler.referencePixelsPerUnit = 100f;

            Rigidbody body = panel.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            CreateImage(panel.transform, "SoftShadow", new Vector2(1252f, 952f), new Vector2(0f, -20f), new Color(0f, 0f, 0f, 0.72f), false);
            CreateImage(panel.transform, "OuterGlowWide", new Vector2(1244f, 944f), Vector2.zero, new Color(0.02f, 0.30f, 1f, 0.08f), false);
            CreateImage(panel.transform, "OuterGlow", new Vector2(1230f, 930f), Vector2.zero, new Color(0.08f, 0.50f, 1f, 0.20f), false);
            CreateImage(panel.transform, "OuterBorder", new Vector2(1218f, 918f), Vector2.zero, new Color(0.60f, 0.86f, 1f, 0.96f), false);
            CreateImage(panel.transform, "OuterFrame", new Vector2(1208f, 908f), Vector2.zero, new Color(0.004f, 0.025f, 0.075f, 1f), false);
            CreateImage(panel.transform, "InnerBorder", new Vector2(1188f, 888f), Vector2.zero, new Color(0.02f, 0.44f, 1f, 0.98f), false);
            CreateSpriteImage(panel.transform, "GlassBackground", panelGradientSprite, new Vector2(1178f, 878f), Vector2.zero, Color.white, false);
            CreateImage(panel.transform, "TopEdgeSheen", new Vector2(1060f, 3f), new Vector2(0f, 432f), new Color(0.36f, 0.74f, 1f, 0.38f), false);

            CanvasGroup welcome = CreatePage(panel.transform, "WelcomePage");
            CreateText(welcome.transform, "BrandText", "NEUROPILOT XR", 26f, new Vector2(900f, 44f), new Vector2(0f, 300f), new Color(0.35f, 0.72f, 1f, 0.72f), FontStyles.Normal);
            CreateText(welcome.transform, "TitleGlow", "注意力训练中心", 118f, new Vector2(1080f, 170f), new Vector2(0f, 52f), new Color(0.06f, 0.40f, 1f, 0.22f), FontStyles.Bold);
            TMP_Text mainTitle = CreateText(welcome.transform, "MainTitle", "注意力训练中心", 114f, new Vector2(1080f, 170f), new Vector2(0f, 58f), Color.white, FontStyles.Bold);
            ApplyTitleGradient(mainTitle);
            CreateImage(welcome.transform, "TitleGlowLineWide", new Vector2(760f, 30f), new Vector2(0f, -45f), new Color(0.02f, 0.30f, 1f, 0.06f), false);
            CreateImage(welcome.transform, "TitleGlowLine", new Vector2(650f, 5f), new Vector2(0f, -45f), new Color(0.18f, 0.68f, 1f, 0.86f), false);
            CreateText(welcome.transform, "Subtitle", "沉浸式脑机注意力训练系统", 26f, new Vector2(900f, 50f), new Vector2(0f, -92f), new Color(0.58f, 0.76f, 0.94f, 0.82f), FontStyles.Normal);
            Vector2 enterPosition = new Vector2(0f, -286f);
            CreateImage(welcome.transform, "EnterButtonGlowWide", new Vector2(560f, 154f), enterPosition, new Color(0.02f, 0.36f, 1f, 0.08f), false);
            CreateImage(welcome.transform, "EnterButtonGlow", new Vector2(530f, 136f), enterPosition, new Color(0.04f, 0.54f, 1f, 0.20f), false);
            CreateImage(welcome.transform, "EnterButtonBorder", new Vector2(510f, 120f), enterPosition, new Color(0.70f, 0.94f, 1f, 1f), false);
            Button enterButton = CreateButton(welcome.transform, "EnterTrainingButton", "进入训练", new Vector2(498f, 108f), enterPosition);

            CanvasGroup difficulty = CreatePage(panel.transform, "DifficultyPage");
            CreateImage(difficulty.transform, "DifficultyBackdropTint", new Vector2(1172f, 872f), Vector2.zero, new Color(0.001f, 0.008f, 0.032f, 0.56f), false);
            GameObject difficultyContentObject = CreateRectObject("DifficultyContent", difficulty.transform, new Vector2(1200f, 900f), Vector2.zero);
            CanvasGroup difficultyContent = difficultyContentObject.AddComponent<CanvasGroup>();

            TMP_Text pageTitle = CreateText(difficultyContent.transform, "PageTitle", "选择训练强度", 62f, new Vector2(1000f, 90f), new Vector2(0f, 345f), Color.white, FontStyles.Bold);
            ApplyTitleGradient(pageTitle);
            CreateText(difficultyContent.transform, "PageSubtitle", "根据当前状态选择合适的训练等级", 27f, new Vector2(1000f, 55f), new Vector2(0f, 278f), new Color(0.82f, 0.87f, 0.93f, 0.88f), FontStyles.Normal);

            DifficultySelector selector = root.AddComponent<DifficultySelector>();
            DifficultyCardView beginner = CreateDifficultyCard(difficultyContent.transform, selector, DifficultyLevel.Beginner, -340f, "Level 01", "轻度", "Beginner", "适合首次体验\n训练节奏舒缓\n刺激负荷较低");
            DifficultyCardView standard = CreateDifficultyCard(difficultyContent.transform, selector, DifficultyLevel.Standard, 0f, "Level 02", "标准", "Standard", "推荐训练模式\n节奏适中\n保持持续专注");
            DifficultyCardView advanced = CreateDifficultyCard(difficultyContent.transform, selector, DifficultyLevel.Advanced, 340f, "Level 03", "挑战", "Advanced", "高强度训练\n任务节奏更快\n注意力要求更高");
            selector.Configure(new[] { beginner, standard, advanced });

            Vector2 startPosition = new Vector2(0f, -356f);
            CreateImage(difficultyContent.transform, "StartButtonGlow", new Vector2(430f, 116f), startPosition, new Color(0.04f, 0.48f, 1f, 0.14f), false);
            CreateImage(difficultyContent.transform, "StartButtonBorder", new Vector2(398f, 102f), startPosition, new Color(0.58f, 0.90f, 1f, 0.92f), false);
            Button startButton = CreateButton(difficultyContent.transform, "StartTrainingButton", "开始训练", new Vector2(388f, 92f), startPosition);
            TMP_Text preparing = CreateText(difficulty.transform, "PreparingText", "正在准备训练...\n3", 48f, new Vector2(900f, 190f), Vector2.zero, Color.white, FontStyles.Normal);
            preparing.gameObject.SetActive(false);

            GameObject dragArea = CreateRectObject("DragArea", panel.transform, new Vector2(1100f, 74f), new Vector2(0f, 402f));
            BoxCollider dragCollider = dragArea.AddComponent<BoxCollider>();
            dragCollider.center = Vector3.zero;
            dragCollider.size = new Vector3(1100f, 74f, 16f);
            CreateSpriteImage(dragArea.transform, "DragHandleGlow", topNotchSprite, new Vector2(340f, 36f), Vector2.zero, new Color(0.10f, 0.55f, 1f, 0.20f), false);
            Image handle = CreateSpriteImage(dragArea.transform, "DragHandle", topNotchSprite, new Vector2(310f, 30f), Vector2.zero, Color.white, false);

            XRGrabInteractable grab = panel.GetComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(dragCollider);
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.trackPosition = true;
            grab.smoothPosition = true;
            grab.smoothPositionAmount = 8f;
            grab.tightenPosition = 0.5f;
            grab.trackRotation = false;
            grab.trackScale = false;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.attachEaseInTime = 0.12f;
            grab.retainTransformParent = true;
            panel.GetComponent<SpatialWindowDragController>().Configure(grab, handle);

            NavigationController navigation = root.AddComponent<NavigationController>();
            navigation.Configure(welcome, difficulty);
            UnityEventTools.AddPersistentListener(enterButton.onClick, navigation.ShowDifficulty);

            GameObject transitionRoot = new GameObject("SceneTransitionManager");
            SceneTransitionManager transition = transitionRoot.AddComponent<SceneTransitionManager>();
            Canvas fadeCanvas;
            CanvasGroup fadeGroup = CreateFadeCanvas(transitionRoot.transform, camera, out fadeCanvas);
            transition.Configure(difficultyContent, preparing, fadeGroup, fadeCanvas, startButton, passthrough,
                File.Exists(TrainingRoomIntegration.ScenePath) ? "TrainingRoom" : "SpaceTraining");
            UnityEventTools.AddPersistentListener(startButton.onClick, transition.BeginTraining);

            difficulty.gameObject.SetActive(false);
            ConfigureSpatialWindow(panel, camera);
            EditorSceneManager.SaveScene(scene, NavigationScenePath);
        }

        [MenuItem("NeuroPilot/Repair Window Drag and Startup Position")]
        public static void RepairWindowInteraction()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before repairing the navigation scene.");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != NavigationScenePath)
                throw new InvalidOperationException("Open NeuroPilotNavigation before running this repair.");

            GameObject panel = GameObject.Find("GlassNavigationPanel");
            if (panel == null)
                throw new InvalidOperationException("Navigation panel was not found.");

            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            ConfigureSpatialWindow(panel, Camera.main);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NeuroPilot] Window trigger drag and viewer-relative startup placement configured.");
        }

        private static void ConfigureSpatialWindow(GameObject panel, Camera camera)
        {
            XRGrabInteractable grab = panel.GetComponent<XRGrabInteractable>();
            Transform dragArea = panel.transform.Find("DragArea");
            BoxCollider collider = dragArea.GetComponent<BoxCollider>();
            collider.size = new Vector3(1100f, 110f, 24f);
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.selectMode = InteractableSelectMode.Single;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = false;
            grab.trackScale = false;
            grab.smoothPosition = true;
            grab.smoothPositionAmount = 12f;
            grab.tightenPosition = 0.5f;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.snapToColliderVolume = true;
            grab.addDefaultGrabTransformers = true;
            grab.throwOnDetach = false;
            grab.retainTransformParent = true;
            panel.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;

            // Only the navigation scene uses the trigger for both UI clicks and window grabs.
            // The top collider is separate from the buttons, so a button click cannot grab the panel.
            foreach (ActionBasedController controller in UnityEngine.Object.FindObjectsOfType<ActionBasedController>(true))
            {
                if (controller.uiPressAction.action == null || controller.uiPressAction.action.bindings.Count == 0)
                    continue;
                controller.selectAction = controller.uiPressAction;
                controller.selectActionValue = controller.uiPressActionValue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            }
            foreach (XRRayInteractor ray in UnityEngine.Object.FindObjectsOfType<XRRayInteractor>(true))
            {
                if (ray.name != "Ray Interactor")
                    continue;
                ray.useForceGrab = false;
                ray.selectActionTrigger = XRBaseControllerInteractor.InputTriggerType.StateChange;
                ray.allowAnchorControl = false;
                ray.keepSelectedTargetValid = true;
                ray.blockUIOnInteractableSelection = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(ray);
            }

            if (dragArea.Find("DragInstruction") == null)
                CreateText(dragArea, "DragInstruction", "按住扳机拖动", 22f,
                    new Vector2(300f, 36f), new Vector2(365f, 0f),
                    new Color(0.55f, 0.78f, 1f, 0.85f), FontStyles.Normal);

            SpatialWindowInitialPlacement placement = panel.GetComponent<SpatialWindowInitialPlacement>();
            if (placement == null)
                placement = panel.AddComponent<SpatialWindowInitialPlacement>();
            placement.Configure(camera);
        }

        private static void BuildTrainingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "SpaceTraining";
            SetupSharedScene(out Camera camera);
            CreateSpaceEnvironment();

            GameObject canvasObject = new GameObject("TrainingStatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));
            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 420f);
            rect.localScale = Vector3.one * 0.001f;
            rect.position = new Vector3(0f, 1.55f, 2.1f);
            rect.rotation = Quaternion.identity;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            CreateImage(canvasObject.transform, "Panel", new Vector2(900f, 420f), Vector2.zero, new Color(0.12f, 0.16f, 0.23f, 0.86f), false);
            CreateText(canvasObject.transform, "Title", "星际训练场景", 62f, new Vector2(780f, 100f), new Vector2(0f, 90f), Color.white, FontStyles.Normal);
            TMP_Text difficulty = CreateText(canvasObject.transform, "Difficulty", "当前训练强度：标准", 34f, new Vector2(780f, 70f), new Vector2(0f, -10f), new Color(0.83f, 0.9f, 0.98f, 1f), FontStyles.Normal);
            CreateText(canvasObject.transform, "Status", "场景转场验证成功", 25f, new Vector2(780f, 60f), new Vector2(0f, -104f), new Color(0.63f, 0.72f, 0.82f, 1f), FontStyles.Normal);

            TrainingScenePresenter presenter = canvasObject.AddComponent<TrainingScenePresenter>();
            presenter.Configure(difficulty);
            EditorSceneManager.SaveScene(scene, TrainingScenePath);
        }

        private static void SetupSharedScene(out Camera camera)
        {
            Camera defaultCamera = Camera.main;
            if (defaultCamera != null)
                UnityEngine.Object.DestroyImmediate(defaultCamera.gameObject);

            GameObject setupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrSetupPrefabPath);
            if (setupPrefab == null)
                throw new InvalidOperationException("XR Interaction Setup prefab is missing. Import XRI Starter Assets first.");

            GameObject setup = (GameObject)PrefabUtility.InstantiatePrefab(setupPrefab);
            setup.name = "XR Interaction Setup";

            camera = setup.GetComponentsInChildren<Camera>(true).FirstOrDefault();
            if (camera == null)
                throw new InvalidOperationException("The XR Interaction Setup prefab did not contain a camera.");

            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.014f, 0.028f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;

            Light light = UnityEngine.Object.FindObjectOfType<Light>();
            if (light != null)
            {
                light.color = new Color(0.78f, 0.86f, 1f);
                light.intensity = 0.7f;
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }
        }

        private static GameObject CreateNavigationBackdrop()
        {
            GameObject backdrop = new GameObject("NavigationBackdrop");

            CreateOrb(backdrop.transform, "BlueHorizon", new Vector3(-5.8f, 3.0f, 12f), 3.0f, blueOrbMaterial);
            CreateOrb(backdrop.transform, "WarmHorizon", new Vector3(6.6f, -1.1f, 14f), 3.8f, warmOrbMaterial);

            for (int index = 0; index < 42; index++)
            {
                float x = -8f + (index % 11) * 1.55f;
                float y = -2.2f + ((index * 7) % 13) * 0.62f;
                float z = 9f + (index % 6) * 1.4f;
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "BackdropStar_" + index.ToString("00");
                star.transform.SetParent(backdrop.transform);
                star.transform.position = new Vector3(x, y, z);
                star.transform.localScale = Vector3.one * (0.035f + (index % 4) * 0.018f);
                star.GetComponent<MeshRenderer>().sharedMaterial = index % 7 == 0 ? warmOrbMaterial : blueOrbMaterial;
                UnityEngine.Object.DestroyImmediate(star.GetComponent<Collider>());
            }

            return backdrop;
        }

        private static void CreateSpaceEnvironment()
        {
            GameObject environment = new GameObject("SpaceEnvironment");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(environment.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.8f, 1f, 2.8f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            CreateOrb(environment.transform, "BluePlanet", new Vector3(-5.5f, 3.2f, 12f), 2.2f, blueOrbMaterial);
            CreateOrb(environment.transform, "WarmPlanet", new Vector3(6.4f, 1.7f, 15f), 3.1f, warmOrbMaterial);

            for (int index = 0; index < 36; index++)
            {
                float angle = index * 137.5f * Mathf.Deg2Rad;
                float radius = 7f + (index % 9) * 1.5f;
                float height = 0.8f + (index % 7) * 0.75f;
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Star_" + index.ToString("00");
                star.transform.SetParent(environment.transform);
                star.transform.position = new Vector3(Mathf.Cos(angle) * radius, height, 7f + Mathf.Sin(angle) * radius);
                star.transform.localScale = Vector3.one * (0.025f + (index % 3) * 0.012f);
                star.GetComponent<MeshRenderer>().sharedMaterial = index % 5 == 0 ? warmOrbMaterial : blueOrbMaterial;
                UnityEngine.Object.DestroyImmediate(star.GetComponent<Collider>());
            }
        }

        private static void CreateOrb(Transform parent, string name, Vector3 position, float scale, Material material)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = name;
            orb.transform.SetParent(parent);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * scale;
            orb.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
        }

        private static CanvasGroup CreatePage(Transform parent, string name)
        {
            GameObject page = CreateRectObject(name, parent, new Vector2(1200f, 900f), Vector2.zero);
            return page.AddComponent<CanvasGroup>();
        }

        private static DifficultyCardView CreateDifficultyCard(
            Transform parent,
            DifficultySelector selector,
            DifficultyLevel level,
            float x,
            string levelText,
            string chineseName,
            string englishName,
            string description)
        {
            GameObject card = CreateRectObject(level + "Card", parent, new Vector2(300f, 400f), new Vector2(x, 4f));
            Image border = card.AddComponent<Image>();
            border.sprite = roundedSprite;
            border.type = Image.Type.Sliced;
            border.color = new Color(0.03f, 0.40f, 1f, 0.72f);
            border.raycastTarget = true;

            Image surface = CreateImage(card.transform, "Surface", new Vector2(288f, 388f), Vector2.zero, new Color(0.012f, 0.055f, 0.14f, 0.96f), false);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            CreateText(card.transform, "LevelText", levelText, 22f, new Vector2(250f, 38f), new Vector2(0f, 155f), new Color(0.72f, 0.8f, 0.9f, 0.85f), FontStyles.Normal);
            CreateText(card.transform, "ChineseName", chineseName, 48f, new Vector2(250f, 68f), new Vector2(0f, 91f), Color.white, FontStyles.Normal);
            CreateText(card.transform, "EnglishName", englishName, 24f, new Vector2(250f, 38f), new Vector2(0f, 46f), new Color(0.76f, 0.84f, 0.93f, 0.88f), FontStyles.Normal);
            CreateText(card.transform, "Description", description, 23f, new Vector2(250f, 140f), new Vector2(0f, -58f), new Color(0.83f, 0.87f, 0.92f, 0.9f), FontStyles.Normal);
            TMP_Text selected = CreateText(card.transform, "SelectedBadge", "已选择", 20f, new Vector2(180f, 34f), new Vector2(0f, -166f), new Color(0.84f, 0.92f, 1f, 1f), FontStyles.Normal);

            DifficultyCardView view = card.AddComponent<DifficultyCardView>();
            view.Configure(level, selector, button, surface, border, selected);
            selected.gameObject.SetActive(level == DifficultyLevel.Standard);
            return view;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size, Vector2 position)
        {
            GameObject buttonObject = CreateRectObject(name, parent, size, position);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = buttonGradientSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
            colors.pressedColor = new Color(0.68f, 0.84f, 1f, 1f);
            colors.disabledColor = new Color(0.55f, 0.58f, 0.62f, 0.5f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            buttonObject.AddComponent<UIInteractionFeedback>();

            TMP_Text text = CreateText(buttonObject.transform, "Label", label, 42f, size - new Vector2(24f, 12f), Vector2.zero, Color.white, FontStyles.Bold);
            text.raycastTarget = false;
            return button;
        }

        private static void ApplyTitleGradient(TMP_Text text)
        {
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(
                new Color(1f, 1f, 1f, 1f),
                new Color(0.90f, 0.97f, 1f, 1f),
                new Color(0.60f, 0.80f, 1f, 1f),
                new Color(0.82f, 0.92f, 1f, 1f));
            text.characterSpacing = 1.2f;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float size,
            Vector2 rectSize,
            Vector2 position,
            Color color,
            FontStyles style)
        {
            GameObject textObject = CreateRectObject(name, parent, rectSize, position);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 size, Vector2 position, Color color, bool raycast)
        {
            GameObject imageObject = CreateRectObject(name, parent, size, position);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Color color, bool raycast)
        {
            GameObject imageObject = CreateRectObject(name, parent, size, position);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static GameObject CreateRectObject(string name, Transform parent, Vector2 size, Vector2 position)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            SetLayerRecursively(instance, LayerMask.NameToLayer("UI"));
            return instance;
        }

        private static CanvasGroup CreateFadeCanvas(Transform parent, Camera camera, out Canvas fadeCanvas)
        {
            GameObject canvasObject = new GameObject("TransitionFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);
            SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            fadeCanvas = canvasObject.GetComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            fadeCanvas.worldCamera = camera;
            fadeCanvas.planeDistance = 0.25f;
            fadeCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject blocker = CreateRectObject("ScreenFade", canvasObject.transform, new Vector2(2400f, 1600f), Vector2.zero);
            RectTransform rect = blocker.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = blocker.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return group;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(NavigationScenePath, true),
                new EditorBuildSettingsScene(File.Exists(TrainingRoomIntegration.ScenePath)
                    ? TrainingRoomIntegration.ScenePath : TrainingScenePath, true)
            };
        }

        private static void ConfigureOpenXR()
        {
            const string settingsAssetPath = SettingsFolder + "/XRGeneralSettingsPerBuildTarget.asset";
            XRGeneralSettingsPerBuildTarget perBuild = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsAssetPath);
            if (perBuild == null)
            {
                perBuild = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perBuild, settingsAssetPath);
                EditorBuildSettings.AddConfigObject("com.unity.xr.management.loader_settings", perBuild, true);
            }

            ConfigureLoader(perBuild, BuildTargetGroup.Standalone);
            ConfigureLoader(perBuild, BuildTargetGroup.Android);
            EditorUtility.SetDirty(perBuild);
        }

        private static void ConfigureLoader(XRGeneralSettingsPerBuildTarget perBuild, BuildTargetGroup group)
        {
            if (!perBuild.HasSettingsForBuildTarget(group))
            {
                perBuild.CreateDefaultSettingsForBuildTarget(group);
                perBuild.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            UnityEngine.XR.Management.XRManagerSettings manager = perBuild.ManagerSettingsForBuildTarget(group);
            if (manager != null)
                XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);

            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
                return;

            EnableOpenXRFeature(settings, "UnityEngine.XR.OpenXR.Features.Interactions.KHRSimpleControllerProfile");
            EnableOpenXRFeature(settings, "UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile");

            if (group == BuildTargetGroup.Standalone)
            {
                EnableOpenXRFeature(settings, "UnityEngine.XR.OpenXR.Features.Interactions.HTCViveControllerProfile");
                EnableOpenXRFeature(settings, "UnityEngine.XR.OpenXR.Features.Interactions.ValveIndexControllerProfile");
                EnableOpenXRFeature(settings, "VIVE.OpenXR.VIVECosmosProfile");
            }

            EnableOpenXRFeature(settings, "VIVE.OpenXR.VIVEFocus3Profile");
            EnableOpenXRFeature(settings, "VIVE.OpenXR.Interaction.ViveInteractions");
            EnableOpenXRFeature(settings, "VIVE.OpenXR.Passthrough.VivePassthrough");
            EditorUtility.SetDirty(settings);
        }

        private static void EnableOpenXRFeature(OpenXRSettings settings, string typeName)
        {
            UnityEngine.XR.OpenXR.Features.OpenXRFeature feature = settings
                .GetFeatures()
                .FirstOrDefault(item => item.GetType().FullName == typeName);

            if (feature == null)
                return;

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (layer >= 0)
                target.layer = layer;

            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
