using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// White-room integration seam, NOT a WebSocket client or an EEG classifier.
/// A future transport subscribes to OutgoingMessage and dispatches received JSON
/// to TryReceiveCommand on Unity's main thread. No networking occurs in this version.
/// </summary>
public sealed class TrainingEegPort : MonoBehaviour
{
    [Header("Python 连接预留（本版不自动联网）")]
    [Tooltip("APK 应填写采集电脑局域网 IP；留空表示尚未配置。")]
    public string serverHost = "";
    [Range(1, 65535)] public int serverPort = 8765;
    [Tooltip("仅供后续适配器联调；启用后不再把扳机/空格计为脑电命中。")]
    public bool eegInputEnabled;

    public event Action<string> OutgoingMessage;
    public string CurrentTargetId { get; private set; }
    public const float HeartbeatIntervalSeconds = 1f;
    public const float HeartbeatTimeoutSeconds = 3f;
    private long outgoingSeq;
    private long lastCommandSeq = -1;
    private bool pendingHit;
    private int mainThread;
    private SessionManager session;

    [Serializable] public sealed class Payload
    {
        public string target_id;
        public float freq_hz;
        public int level;
        public int wave;
    }

    [Serializable] public sealed class Envelope
    {
        public string type;
        public long ts;
        public long seq = -1;
        public Payload payload;
    }

    private void Awake()
    {
        mainThread = Thread.CurrentThread.ManagedThreadId;
        session = GetComponent<SessionManager>();
    }

    public void BeginStimulus(float frequency)
    {
        EndStimulus();
        // Never reuse IDs across restarts: a late classification cannot hit a new ball.
        CurrentTargetId = Guid.NewGuid().ToString("N");
        Emit("stimulus_onset", new Payload { target_id = CurrentTargetId, freq_hz = frequency, level = 1, wave = 1 });
    }

    public void EndStimulus()
    {
        string target = CurrentTargetId;
        CurrentTargetId = null;
        pendingHit = false;
        if (target != null) Emit("stimulus_offset", new Payload { target_id = target });
    }

    private void Emit(string type, Payload payload)
    {
        var message = new Envelope { type = type, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), seq = outgoingSeq++, payload = payload };
        var listeners = OutgoingMessage;
        if (listeners == null) return;
        string json = JsonUtility.ToJson(message);
        foreach (Action<string> listener in listeners.GetInvocationList())
        {
            try { listener(json); }
            catch (Exception error) { Debug.LogException(error); }
        }
    }

    /// <summary>Main-thread adapter entry. Only current-target fire is implemented.
    /// Difficulty/auto/reset remain unimplemented: no silent change to training rules.</summary>
    public bool TryReceiveCommand(string json)
    {
        if (Thread.CurrentThread.ManagedThreadId != mainThread || !eegInputEnabled ||
            session == null || !session.isActiveAndEnabled ||
            session.CurrentState != SessionManager.State.AwaitHit || CurrentTargetId == null ||
            pendingHit || string.IsNullOrEmpty(json) || json.Length > 8192) return false;
        Envelope command;
        try { command = JsonUtility.FromJson<Envelope>(json); }
        catch (ArgumentException) { return false; }
        if (command == null || command.type != "command_fire" || command.ts <= 0 ||
            command.seq < 0 || command.seq <= lastCommandSeq || command.payload == null ||
            command.payload.target_id != CurrentTargetId) return false;
        lastCommandSeq = command.seq;
        pendingHit = true;
        return true;
    }

    public bool ConsumeHit()
    {
        bool result = pendingHit;
        pendingHit = false;
        return result;
    }

    // Future transport calls this on the main thread after a NEW connection is established.
    public void ResetConnectionSequence() { lastCommandSeq = -1; pendingHit = false; }
}
