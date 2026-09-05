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
    ///   （主线程回调，可安全操作 Unity 对象）。
    /// 断线自动重连；未连接时上行静默丢弃（训练不中断，自动降级键盘模拟模式）。
    /// 所有 await 的续体都回到 Unity 主线程同步上下文，无需额外调度。
    /// </summary>
    public sealed class FusionLink : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "ws://127.0.0.1:8765";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private float reconnectDelaySeconds = 2f;

        /// <summary>下行命令分发事件（主线程）：type 为 command_* 之一。</summary>
        public event Action<string, FusionJson> CommandReceived;

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
            TryAbortSocket();
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

        private async Task RunClient(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _socket = new ClientWebSocket();
                    await _socket.ConnectAsync(new Uri(serverUrl), token);
                    _warnedOffline = false;
                    Debug.Log($"[FusionLink] 已连接 {serverUrl}");
                    Task receive = ReceiveLoop(token);
                    Task send = SendLoop(token);
                    await Task.WhenAny(receive, send);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FusionLink] 连接中断：{ex.Message}（{reconnectDelaySeconds}s 后重连）");
                }

                TryAbortSocket();
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay((int)(reconnectDelaySeconds * 1000), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SendLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_outbox.IsEmpty || !IsConnected)
                {
                    await Task.Delay(30, token);
                    continue;
                }

                while (_outbox.TryDequeue(out string message) && IsConnected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(message);
                    await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                }
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            while (IsConnected && !token.IsCancellationRequested)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[FusionLink] 服务端关闭连接");
                    return;
                }

                if (FusionJson.TryParseEnvelope(sb.ToString(), out string type, out FusionJson fields) && type != "ping")
                {
                    CommandReceived?.Invoke(type, fields);
                }
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
                Debug.LogWarning($"[FusionLink] 未连接融合层（{serverUrl}），上行事件将丢弃；训练继续（键盘模拟模式）");
            }
        }

        private void TryAbortSocket()
        {
            try
            {
                _socket?.Abort();
            }
            catch
            {
                // 关闭阶段的异常一律忽略
            }
        }
    }
}
