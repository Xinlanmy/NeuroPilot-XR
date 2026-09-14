using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace NeuroPilotXR.Editor
{
    // Native world-space uGUI; no screen-space overlays attached to the user's head.
    public static class TrainingRoomPresentation
    {
        // Cards mirror the navigation panel: rounded navy surface with a cyan edge, so the HUD stops
        // reading as flat rectangles pasted on the wall.
        private static readonly Color Glass = new Color(0.06f, 0.13f, 0.26f, 0.94f);
        private static readonly Color Muted = new Color(0.64f, 0.75f, 0.82f);
        private static readonly Color Accent = new Color(0.25f, 0.84f, 0.93f);
        private static readonly Color Edge = new Color(0.08f, 0.6f, 1f, 0.85f);
        private static Font font;
        private static Sprite rounded;

        public static void Prepare(TrainingHUD hud, XROrigin rig)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/NeuroPilot/Fonts/NotoSansSC-Variable.ttf");
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/NeuroPilot/Art/RoundedRectangle.asset");
            var canvas = (RectTransform)hud.transform;
            canvas.sizeDelta = new Vector2(1600, 1050);
            canvas.localScale = Vector3.one * 0.002f;
            canvas.SetPositionAndRotation(new Vector3(0, 1.6f, 5.2f), Quaternion.identity);
            var stats = Node("StatsBar", canvas, Vector2.zero, new Vector2(1200, 1000));
            hud.statsRoot = stats.gameObject;
            Label("Brand", stats, "NEUROPILOT   /   ATTENTION LAB", new Vector2(0, 475), new Vector2(1120, 45), 26, new Color(0.85f, 0.92f, 0.96f));
            hud.statText = Stat("HitsCard", stats, hud.statText, "命中数", -370);
            hud.timeText = Stat("TimeCard", stats, hud.timeText, "剩余时间", 0);
            hud.accuracyText = Stat("AccuracyCard", stats, hud.accuracyText, "命中率", 370);

            var footer = Card("TrainingFooter", canvas, new Vector2(0, -375), new Vector2(1160, 132), Glass);
            hud.footerRoot = footer.gameObject;
            hud.hintText = Reuse(hud.hintText, "HintText", footer, new Vector2(0, 24), new Vector2(1100, 48), 30, Color.white);
            // The hint carries a second, diagnostic line whenever eye data is missing. The box is one
            // line tall by design, so overflow is the only thing keeping that line from being dropped
            // in silence; Reuse leaves the mode at Truncate.
            hud.hintText.verticalOverflow = VerticalWrapMode.Overflow;
            hud.modeText = Reuse(hud.modeText, "ModeText", footer, new Vector2(0, -29), new Vector2(1100, 40), 24, Muted);

            var countdown = Card("ReadyCard", canvas, Vector2.zero, new Vector2(260, 240), Glass);
            hud.countdownRoot = countdown.gameObject;
            Label("ReadyLabel", countdown, "准备开始", new Vector2(0, 72), new Vector2(230, 48), 30, Muted);
            hud.countdownText = Label("ReadyValue", countdown, "3", new Vector2(0, -20), new Vector2(230, 132), 104, Accent);
            hud.countdownText.verticalOverflow = VerticalWrapMode.Overflow;
            var result = hud.resultPanel.GetComponent<RectTransform>();
            result.anchoredPosition = Vector2.zero;
            result.sizeDelta = new Vector2(980, 720);
            var resultImage = result.GetComponent<Image>();
            resultImage.sprite = rounded; resultImage.type = Image.Type.Sliced; resultImage.color = Glass;
            AddEdge(result.gameObject);
            hud.resultText = Reuse(hud.resultText, "ResultText", result, Vector2.zero, new Vector2(890, 650), 36, Color.white);
            hud.resultText.supportRichText = true;
            result.SetAsLastSibling();
            hud.resultPanel.SetActive(false);
            hud.ResetForRound(hud.session != null && hud.session.config != null ? hud.session.config.roundDuration : 180);

            // Keep the training controllers and rays visible, as requested.
            // Restore prefab renderer defaults so the locomotion vignette stays off.
            foreach (var controller in rig.GetComponentsInChildren<XRBaseController>(true))
                controller.hideControllerModel = false;
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>(true))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                if (source != null) renderer.enabled = source.enabled;
            }
            foreach (var visual in rig.GetComponentsInChildren<XRInteractorLineVisual>(true)) visual.enabled = true;

            Tint("Front Wall", new Color(0.22f, 0.27f, 0.31f));
            Tint("Back Wall", new Color(0.78f, 0.82f, 0.85f));
            Tint("Left Wall", new Color(0.78f, 0.82f, 0.85f));
            Tint("Right Wall", new Color(0.78f, 0.82f, 0.85f));
            Tint("Ceiling", new Color(0.88f, 0.91f, 0.94f));
            Tint("Floor", new Color(0.48f, 0.53f, 0.57f));
            var marker = GameObject.Find("Floor Center Mark");
            if (marker != null) marker.SetActive(false);

            // The room is a sealed box: if the enclosure casts, the ceiling shadow-maps the whole interior
            // to black. Only the practice balls (runtime primitives, casting by default) drop a shadow,
            // which is what gives the mid-air targets their depth cue.
            foreach (string shell in new[] { "Ceiling", "Front Wall", "Back Wall", "Left Wall", "Right Wall", "Floor" })
                SetShellShadowFlags(shell);
            var key = GameObject.Find("Key Light");
            var keyLight = key != null ? key.GetComponent<Light>() : null;
            if (keyLight != null)
            {
                keyLight.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(keyLight);
            }
            ConfigureShadowBudget();
        }

        private static void SetShellShadowFlags(string name)
        {
            var obj = GameObject.Find(name);
            var shellRenderer = obj != null ? obj.GetComponent<Renderer>() : null;
            if (shellRenderer == null) return;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shellRenderer.receiveShadows = true;
            EditorUtility.SetDirty(shellRenderer);
        }

        /// <summary>The room is 12 m deep; the 50 m default budget spends shadow-map texels on nothing.</summary>
        private static void ConfigureShadowBudget()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) return;
            pipeline.shadowDistance = 20f;
            // supportsSoftShadows is read-only; URP only exposes the backing field through SerializedObject.
            var serialized = new SerializedObject(pipeline);
            var soft = serialized.FindProperty("m_SoftShadowsSupported");
            if (soft != null) soft.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static void Tint(string name, Color color)
        {
            var obj = GameObject.Find(name);
            if (obj == null) return;
            var renderer = obj.GetComponent<Renderer>();
            string path = "Assets/NeuroPilot/TrainingRoom/Materials/Palette_" + name.Replace(" ", "_") + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetFloat("_Smoothness", 0.05f);
                AssetDatabase.CreateAsset(material, path);
            }
            renderer.sharedMaterial = material;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }

        private static Text Stat(string name, Transform parent, Text value, string title, float x)
        {
            var panel = Card(name, parent, new Vector2(x, 380), new Vector2(350, 138), Glass);
            var caption = Label("Caption", panel, title, new Vector2(0, 39), new Vector2(290, 40), 28, Muted);
            caption.alignment = TextAnchor.MiddleLeft;
            var output = Reuse(value, "Value", panel, new Vector2(0, -23), new Vector2(290, 75), 58, Color.white);
            output.verticalOverflow = VerticalWrapMode.Overflow;
            output.alignment = TextAnchor.MiddleLeft;
            var accent = Panel("Accent", panel, new Vector2(-150, 0), new Vector2(3, 90), Accent);
            return output;
        }

        private static RectTransform Node(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var child = parent.Find(name);
            var rect = child != null ? (RectTransform)child : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Node(name, parent, position, size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Card(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Panel(name, parent, position, size, color);
            AddEdge(rect.gameObject);
            return rect;
        }

        private static void AddEdge(GameObject target)
        {
            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = Edge;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static Text Label(string name, Transform parent, string content, Vector2 position, Vector2 size, int pixels, Color color)
        {
            var text = Reuse(null, name, parent, position, size, pixels, color);
            text.text = content;
            return text;
        }

        private static Text Reuse(Text text, string name, Transform parent, Vector2 position, Vector2 size, int pixels, Color color)
        {
            if (text != null) { text.transform.SetParent(parent, false); text.name = name; }
            var rect = Node(name, parent, position, size);
            text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = pixels; text.color = color;
            text.fontStyle = FontStyle.Normal; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
