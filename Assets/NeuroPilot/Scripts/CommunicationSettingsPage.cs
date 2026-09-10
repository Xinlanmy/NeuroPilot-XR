using System;
using System.Net.WebSockets;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroPilotXR.Navigation
{
    public sealed class CommunicationSettingsPage : MonoBehaviour
    {
        public TMP_Text address, status;
        private string draft;
        private ConnectionTestDialog dialog;
        private bool testing;
        private CancellationTokenSource probeCancellation;

        private void OnEnable() { draft = CommunicationSettings.Host; Refresh(); }
        private void OnDisable()
        {
            probeCancellation?.Cancel();
            if (dialog != null) dialog.Hide();
        }
        public void Append(string value)
        {
            if (draft.Length + value.Length > 15) return;
            foreach (char c in value) if ((c < '0' || c > '9') && c != '.') return;
            draft += value; Refresh();
        }
        public void Backspace() { if (draft.Length > 0) draft = draft.Substring(0, draft.Length - 1); Refresh(); }
        public void Clear() { draft = ""; Refresh(); }
        public void RestoreDefault() { draft = CommunicationSettings.DefaultHost; Refresh(); }
        public void Save()
        {
            if (!CommunicationSettings.TrySave(draft))
            {
                status.text = "地址格式有误，请输入完整 IPv4 地址";
                status.color = new Color(1, .65f, .4f); return;
            }
            draft = CommunicationSettings.Host; Refresh();
            status.text = "已保存 · 单球 / 多球共用 · 重启后保留";
        }

        /// <summary>
        /// 「测试连接」按钮入口：向 ws://{IP}:{Port} 发起真实 WebSocket 握手探测，
        /// 结果以模态弹窗呈现。draft 为空时测已保存地址；draft 无效不发起网络请求。
        /// 探测成功即关闭握手（ws_server 正常清理），不打扰后续正式连接。
        /// </summary>
        public void TestConnection()
        {
            if (testing) return;
            string input = string.IsNullOrWhiteSpace(draft) ? CommunicationSettings.Host : draft;
            if (!CommunicationSettings.TryNormalize(input, out string host))
            {
                Dialog().Show("连接测试", "地址格式有误，请输入完整 IPv4 地址", new Color(1f, .55f, .45f));
                return;
            }

            string url = "ws://" + host + ":" + CommunicationSettings.Port;
            testing = true;
            status.text = "正在测试连接 " + url + " …";
            Probe(url);
        }

        /// <summary>async void 仅作事件处理器；await 续体回 Unity 主线程，弹窗操作安全。</summary>
        private async void Probe(string url)
        {
            string message;
            Color color;
            var socket = new ClientWebSocket();
            var lifetime = new CancellationTokenSource();
            probeCancellation = lifetime;
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(4));
                    await socket.ConnectAsync(new Uri(url), cts.Token);
                }

                message = "连接成功\n" + url;
                color = new Color(.4f, .95f, .5f);
                try
                {
                    using (var closeCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
                    {
                        closeCts.CancelAfter(TimeSpan.FromSeconds(1));
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test-done", closeCts.Token);
                    }
                }
                catch
                {
                    try { socket.Abort(); } catch { /* 关闭阶段异常一律忽略 */ }
                }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                message = "连接失败\n" + url + "\n" + Describe(ex) +
                    "\n请确认 PC 端 ws_server 已启动、头显与 PC 同一 WiFi、防火墙放行 8765";
                color = new Color(1f, .55f, .45f);
                try { socket.Abort(); } catch { /* 关闭阶段异常一律忽略 */ }
            }
            finally
            {
                socket.Dispose();
                testing = false;
                probeCancellation = null;
                lifetime.Dispose();
            }

            if (this == null || !isActiveAndEnabled || lifetime.IsCancellationRequested) return;
            Dialog().Show("连接测试", message, color);
            status.text = "当前已保存：" + CommunicationSettings.Host + "  ·  端口 " + CommunicationSettings.Port;
        }

        private static string Describe(Exception ex)
        {
            if (ex is OperationCanceledException || ex.GetBaseException() is OperationCanceledException)
            {
                return "超时（4 秒无响应）";
            }

            string text = ex.GetBaseException().Message;
            return text.Length > 120 ? text.Substring(0, 120) + "…" : text;
        }

        private ConnectionTestDialog Dialog()
        {
            if (dialog == null)
            {
                dialog = gameObject.AddComponent<ConnectionTestDialog>();
                dialog.Font = status != null ? status.font : address != null ? address.font : TMP_Settings.defaultFontAsset;
            }
            return dialog;
        }

        private void Refresh()
        {
            if (address == null || status == null) return;
            address.text = string.IsNullOrEmpty(draft) ? "请输入 IP 地址" : draft;
            status.color = new Color(.5f, .85f, 1);
            status.text = "当前已保存：" + CommunicationSettings.Host + "  ·  端口 " + CommunicationSettings.Port;
        }

        /// <summary>模态结果弹窗：设置页面板内代码自建（遮罩 + 卡片 + 确定），不依赖场景预制。</summary>
        private sealed class ConnectionTestDialog : MonoBehaviour
        {
            private GameObject root;
            private TMP_Text titleText;
            private TMP_Text message;
            public TMP_FontAsset Font { private get; set; }

            public void Show(string title, string content, Color color)
            {
                if (root == null) Build();
                root.SetActive(true);
                titleText.text = title;
                message.text = content;
                message.color = color;
            }

            private void Build()
            {
                root = new GameObject("ConnectionTestDialog", typeof(RectTransform), typeof(Image));
                root.transform.SetParent(transform, false);
                var rt = (RectTransform)root.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

                var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(root.transform, false);
                var cardRt = (RectTransform)card.transform;
                cardRt.sizeDelta = new Vector2(840, 480);
                card.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.13f, 0.96f);

                titleText = CreateText(card.transform, "Title", "连接测试", new Vector2(0, 170), new Vector2(760, 56), 40, new Color(0.9f, 0.95f, 1f));
                message = CreateText(card.transform, "Message", "", new Vector2(0, 10), new Vector2(760, 240), 30, Color.white);

                var btn = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btn.transform.SetParent(card.transform, false);
                var btnRt = (RectTransform)btn.transform;
                btnRt.anchoredPosition = new Vector2(0, -165);
                btnRt.sizeDelta = new Vector2(220, 64);
                btn.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
                btn.GetComponent<Button>().onClick.AddListener(Hide);
                CreateText(btn.transform, "Label", "确定", Vector2.zero, new Vector2(200, 56), 34, Color.white);
            }

            public void Hide() { if (root != null) root.SetActive(false); }

            private TMP_Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (Font != null)
                {
                    tmp.font = Font;
                }

                tmp.text = content;
                tmp.fontSize = fontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = color;
                tmp.raycastTarget = false;
                return tmp;
            }
        }
    }
}
