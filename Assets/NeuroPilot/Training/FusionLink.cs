using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// Unity ↔ 融合层 WebSocket 客户端（契约：主仓库 app/unity/README.md）。
    /// - 上行：stimulus_onset / stimulus_offset / lock_progress / scene_event（JsonUtility 序列化 DTO），
    ///   每 1s 发 ping 心跳；
    /// - 下行：command_fire / command_difficulty / command_auto / command_reset → CommandReceived
    ///   （主线程回调，可安全操作 Unity 对象；payload 嵌套对象已单独解析传入）。
    /// 生命周期：每轮连接持有局部 ClientWebSocket 与独立 token；断开时取消并等待收发循环
    /// 退出、Dispose socket、清空本轮上行队列（离线事件不补发），随后自动重连。
    /// EegFusion 模式断线时命中输入降级键盘模拟（见 TrialSessionManager.ResolveHitSource）。
    /// 所有 await 的续体都回到 Unity 主线程同步上下文，无需额外调度。
    /// </summary>
    public sealed class FusionLink : MonoBehaviour
    {
        private const int LoopsShutdownTimeoutMs = 3000;
        private const string ServerUrlPrefsKey = "fusion_server_url";

        [SerializeField] private string serverUrl = "ws://127.0.0.1:8765";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private float reconnectDelaySeconds = 2f;

        /// <summary>下行命令分发事件（主线程）：type 为 command_* 之一，payload 为信封 payload 子对象（无 payload 时为顶层视图）。</summary>
        public event Action<string, FusionJson> CommandReceived;

        /// <summary>下行 command_* 信封原文（主线程）：供自带信封校验的缝（v1.2.0 TryReceiveCommand）使用。
        /// 只分发 command_ 前缀信封——ping 等心跳与非命令类型不进入，避免每秒心跳流经命令解析缝。</summary>
        public event Action<string> CommandEnvelopeReceived;

        /// <summary>每次新连接建立后触发（主线程续体）：适配器在此复位下行 seq 基线。</summary>
        public event Action Connected;

        /// <summary>当前连接的 socket（仅状态查询；收发循环使用各自局部引用）。</summary>
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _outbox = new ConcurrentQueue<string>();
        private long _sequence;
        private float _pingTimer;
        private bool _warnedOffline;

        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        /// <summary>串流部署默认 127.0.0.1；APK 直装部署改为采集工作站 IP。</summary>
        public string ServerUrl
        {
            get => serverUrl;
            set => serverUrl = value;
        }

        /// <summary>Unix 毫秒时间戳（契约 t_ms 口径，与 Python 侧 recv_ts 对齐用）。</summary>
        public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>地址覆盖优先级：Inspector 默认 &lt; PlayerPrefs（配置面板保存）&lt; 覆盖文件
        /// （persistentDataPath/fusion_url.txt，adb push 兜底——系统键盘不可用时仍能改地址）。</summary>
        private void Awake()
        {
            string saved = PlayerPrefs.GetString(ServerUrlPrefsKey, null);
            if (!string.IsNullOrEmpty(saved))
            {
                serverUrl = saved;
            }

            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "fusion_url.txt");
                if (System.IO.File.Exists(path))
                {
                    string fromFile = System.IO.File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(fromFile))
                    {
                        serverUrl = fromFile;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionLink] 读取地址覆盖文件失败：{ex.Message}");
            }
        }

        /// <summary>运行时改地址（FusionServerConfigPanel 用）：持久化 + 立即重连。缺协议头自动补 ws://。</summary>
        public void ApplyServerUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            url = url.Trim();
            if (!url.StartsWith("ws://") && !url.StartsWith("wss://"))
            {
                url = "ws://" + url;
            }

            if (url == serverUrl)
            {
                return;
            }

            serverUrl = url;
            PlayerPrefs.SetString(ServerUrlPrefsKey, serverUrl);
            PlayerPrefs.Save();

            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }

            StartClient();
        }

        private void Start()
        {
            if (autoConnect)
            {
                StartClient();
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
        }

        public void StartClient()
        {
            if (_cts != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _ = RunClient(_cts.Token);
        }

        private async Task RunClient(CancellationToken clientToken)
        {
            while (!clientToken.IsCancellationRequested)
            {
                ClientWebSocket socket = null;
                CancellationTokenSource roundCts = null;
                Task receive = Task.CompletedTask;
                Task send = Task.CompletedTask;
                var stop = false;
                try
                {
                    socket = new ClientWebSocket();
                    roundCts = CancellationTokenSource.CreateLinkedTokenSource(clientToken);
                    _socket = socket;
                    await socket.ConnectAsync(new Uri(serverUrl), roundCts.Token);
                    _warnedOffline = false;
                    Debug.Log($"[FusionLink] 已连接 {serverUrl}");
                    Connected?.Invoke();
                    receive = ReceiveLoop(socket, roundCts.Token);
                    send = SendLoop(socket, roundCts.Token);
                    await Task.WhenAny(receive, send);
                }
                catch (OperationCanceledException)
                {
                    stop = clientToken.IsCancellationRequested;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FusionLink] 连接中断：{ex.Message}（{reconnectDelaySeconds}s 后重连）");
                }
                finally
                {
                    // 收尾：停两条循环 → 等待退出（带超时）→ 释放 socket → 清空本轮上行队列
                    if (roundCts != null)
                    {
                        roundCts.Cancel();
                        await AwaitLoopsShutdown(receive, send);
                        roundCts.Dispose();
                    }

                    DisposeSocket(socket);
                    // 只清理本轮自己的 socket：ApplyServerUrl 重连时新客户端可能已挂上 _socket
                    if (ReferenceEquals(_socket, socket))
                    {
                        _socket = null;
                    }

                    DrainOutbox();
                }

                if (stop)
                {
                    break;
                }

                try
                {
                    await Task.Delay((int)(reconnectDelaySeconds * 1000), clientToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SendLoop(ClientWebSocket socket, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_outbox.IsEmpty || socket.State != WebSocketState.Open)
                    {
                        await Task.Delay(30, token);
                        continue;
                    }

                    while (_outbox.TryDequeue(out string message) && socket.State == WebSocketState.Open)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 关闭语义，正常退出
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionLink] 发送循环退出：{ex.Message}");
            }
        }

        private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            try
            {
                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[FusionLink] 服务端关闭连接");
                        return;
                    }

                    if (FusionJson.TryParseEnvelope(sb.ToString(), out string type, out FusionJson envelope))
                    {
                        if (type != "ping" && type.StartsWith("command_", StringComparison.Ordinal))
                        {
                            CommandEnvelopeReceived?.Invoke(sb.ToString());
                            FusionJson payload = envelope.Obj("payload") ?? envelope;
                            CommandReceived?.Invoke(type, payload);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 关闭语义，正常退出
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionLink] 接收循环退出：{ex.Message}");
            }
        }

        /// <summary>等待收发循环退出（超时兜底），并确保异常被观察。</summary>
        private static async Task AwaitLoopsShutdown(Task receive, Task send)
        {
            if (receive.Status == TaskStatus.RanToCompletion && send.Status == TaskStatus.RanToCompletion)
            {
                return;
            }

            var all = Task.WhenAll(receive, send);
            var done = await Task.WhenAny(all, Task.Delay(LoopsShutdownTimeoutMs));
            if (done != all)
            {
                Debug.LogWarning("[FusionLink] 等待收发循环退出超时，强制继续清理");
            }

            _ = all.ContinueWith(
                t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private static void DisposeSocket(ClientWebSocket socket)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Abort();
            }
            catch
            {
                // 关闭阶段的异常一律忽略
            }

            try
            {
                socket.Dispose();
            }
            catch
            {
                // 同上
            }
        }

        /// <summary>清空上行队列：断线期间的事件按设计丢弃，重连后不补发过期刺激事件。</summary>
        private void DrainOutbox()
        {
            while (_outbox.TryDequeue(out _))
            {
            }
        }

        private void Update()
        {
            _pingTimer += Time.unscaledDeltaTime;
            if (_pingTimer >= 1f)
            {
                _pingTimer = 0f;
                SendPing();
            }
        }

        /// <summary>上行事件（payload 为 [Serializable] DTO，字段名与契约 snake_case 一致）。</summary>
        public void SendEvent(string type, object payload)
        {
            long seq = Interlocked.Increment(ref _sequence);
            string json = "{\"type\":\"" + type + "\",\"ts\":" + NowMs() + ",\"seq\":" + seq;
            if (payload != null)
            {
                json += ",\"payload\":" + JsonUtility.ToJson(payload);
            }

            Enqueue(json + "}");
        }

        /// <summary>上行完整信封原文：缝自带 ts/seq 时使用（v1.2.0 TrainingEegPort / TargetPracticeSession）。</summary>
        public void SendRaw(string json)
        {
            Enqueue(json);
        }

        private void SendPing()
        {
            long seq = Interlocked.Increment(ref _sequence);
            Enqueue(string.Concat(
                "{\"type\":\"ping\",\"ts\":", NowMs().ToString(),
                ",\"seq\":", seq.ToString(), "}"));
        }

        private void Enqueue(string json)
        {
            if (IsConnected)
            {
                _outbox.Enqueue(json);
            }
            else if (!_warnedOffline)
            {
                _warnedOffline = true;
                Debug.LogWarning($"[FusionLink] 未连接融合层（{serverUrl}），上行事件将丢弃；EegFusion 模式自动降级键盘模拟（TrialSessionManager）");
            }
        }
    }
}
