using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 融合服务器地址运行时配置面板：APK 直装后改 ws 地址免重出包。
    /// TrialSessionManager.Start 经 Ensure() 挂接；面板于启动时出现在被试面前 1.5m，
    /// 已连接且无人动过它时 2s 后自动关闭（正式试次不抢视野）；改过地址则常驻直到手动关闭。
    /// 保存即写 PlayerPrefs，下次启动生效；头显系统键盘不可用时走
    /// persistentDataPath/fusion_url.txt 覆盖文件兜底（见 FusionLink.Awake）。
    /// </summary>
    public sealed class FusionServerConfigPanel : MonoBehaviour
    {
        private static FusionServerConfigPanel _instance;

        private FusionLink _link;
        private TMP_InputField _input;
        private TMP_Text _status;
        private bool _touched;
        private float _connectedSince = -1f;

        /// <summary>幂等挂接：fusionLink 为空或面板已存在时不做事。</summary>
        public static void Ensure(FusionLink link)
        {
            if (link == null || _instance != null)
            {
                return;
            }

            var go = new GameObject("FusionServerConfigPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            _instance = go.AddComponent<FusionServerConfigPanel>();
            _instance.Build(link);
        }

        private void Build(FusionLink link)
        {
            _link = link;

            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(760, 400);
            transform.localScale = Vector3.one * 0.0015f;

            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;
            Vector3 origin = cam != null ? cam.transform.position : Vector3.zero;
            transform.SetPositionAndRotation(
                origin + forward * 1.5f + Vector3.down * 0.1f,
                Quaternion.LookRotation(forward, Vector3.up));

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(transform, false);
            Stretch((RectTransform)bg.transform);
            bg.color = new Color(0.05f, 0.08f, 0.13f, 0.92f);

            CreateText(bg.transform, "Title", "融合服务器地址", new Vector2(0, 158), new Vector2(720, 56), 40, new Color(0.9f, 0.95f, 1f));
            _status = CreateText(bg.transform, "Status", "", new Vector2(0, -158), new Vector2(720, 48), 32, Color.gray);

            var inputGo = new GameObject("UrlInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(bg.transform, false);
            var inputRt = (RectTransform)inputGo.transform;
            inputRt.anchoredPosition = new Vector2(0, 50);
            inputRt.sizeDelta = new Vector2(620, 72);
            inputGo.GetComponent<Image>().color = new Color(0.92f, 0.94f, 0.97f);

            var inputText = CreateText(inputGo.transform, "Text", "", Vector2.zero, Vector2.zero, 36, Color.black);
            inputText.alignment = TextAlignmentOptions.Left;
            inputText.enableWordWrapping = false;
            inputText.margin = new Vector4(16, 0, 16, 0);
            var inputTextRt = (RectTransform)inputText.transform;
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.sizeDelta = Vector2.zero;

            _input = inputGo.GetComponent<TMP_InputField>();
            _input.textComponent = inputText;
            _input.text = link.ServerUrl;
            _input.onValueChanged.AddListener(_ => _touched = true);

            CreateButton(bg.transform, "PresetLoopback", "串流本机", new Vector2(-240, -70), () =>
            {
                _input.text = "ws://127.0.0.1:8765";
                _touched = true;
            });
            CreateButton(bg.transform, "Apply", "保存并重连", new Vector2(0, -70), () =>
            {
                _touched = true;
                _link.ApplyServerUrl(_input.text);
            });
            CreateButton(bg.transform, "Close", "关闭", new Vector2(240, -70), () => gameObject.SetActive(false));
        }

        private void Update()
        {
            if (_link == null || _status == null)
            {
                return;
            }

            if (_link.IsConnected)
            {
                _status.text = $"已连接 {_link.ServerUrl}";
                _status.color = new Color(0.4f, 0.95f, 0.5f);
                if (!_touched)
                {
                    if (_connectedSince < 0f)
                    {
                        _connectedSince = Time.unscaledTime;
                    }
                    else if (Time.unscaledTime - _connectedSince > 2f)
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                _connectedSince = -1f;
                _status.text = $"未连接（{_link.ServerUrl}）——确认 PC 端 ws_server 已启动、同 WiFi、防火墙放行 8765";
                _status.color = new Color(1f, 0.55f, 0.45f);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            if (size != Vector2.zero)
            {
                rt.sizeDelta = size;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static void CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 64);
            go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            CreateText(go.transform, "Label", label, Vector2.zero, Vector2.zero, 34, Color.white);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
