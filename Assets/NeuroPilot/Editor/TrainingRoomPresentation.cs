using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace NeuroPilotXR.Editor
{
    // Native world-space uGUI; no screen-space overlays attached to the user's head.
    public static class TrainingRoomPresentation
    {
        private static readonly Color Ink = new Color(0.045f, 0.075f, 0.11f, 0.96f);
        private static readonly Color Muted = new Color(0.64f, 0.75f, 0.82f);
        private static readonly Color Accent = new Color(0.25f, 0.84f, 0.93f);
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

            var footer = Panel("TrainingFooter", canvas, new Vector2(0, -375), new Vector2(1160, 132), Ink);
            hud.footerRoot = footer.gameObject;
            hud.hintText = Reuse(hud.hintText, "HintText", footer, new Vector2(0, 24), new Vector2(1100, 48), 30, Color.white);
            hud.modeText = Reuse(hud.modeText, "ModeText", footer, new Vector2(0, -29), new Vector2(1100, 40), 24, Muted);

            var countdown = Panel("ReadyCard", canvas, Vector2.zero, new Vector2(260, 240), Ink);
            hud.countdownRoot = countdown.gameObject;
            Label("ReadyLabel", countdown, "准备开始", new Vector2(0, 72), new Vector2(230, 48), 30, Muted);
            hud.countdownText = Label("ReadyValue", countdown, "3", new Vector2(0, -20), new Vector2(230, 132), 104, Accent);
            hud.countdownText.verticalOverflow = VerticalWrapMode.Overflow;
            var result = hud.resultPanel.GetComponent<RectTransform>();
            result.anchoredPosition = Vector2.zero;
            result.sizeDelta = new Vector2(980, 720);
            var resultImage = result.GetComponent<Image>();
            resultImage.sprite = rounded; resultImage.type = Image.Type.Sliced; resultImage.color = Ink;
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
            var panel = Panel(name, parent, new Vector2(x, 380), new Vector2(350, 138), Ink);
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
