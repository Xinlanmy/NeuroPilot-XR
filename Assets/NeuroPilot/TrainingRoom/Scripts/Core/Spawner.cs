using UnityEngine;

/// <summary>
/// 出球器：在可配范围内随机生成/复用一个球，控制其位置、闪烁与反馈状态。
/// 由 SessionManager 驱动。
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("引用(由场景构建器注入)")]
    public SessionConfig config;
    public Transform ball;              // 球体 Transform
    public SSVEPFlicker flicker;        // 球上的闪烁组件
    public Renderer ballRenderer;       // 球的 Renderer(做反馈变色)
    public Camera viewCamera;           // 仅供入场建立训练坐标，不参与后续选点
    public Vector3 TrainingOrigin { get; private set; }
    public Quaternion TrainingRotation { get; private set; } = Quaternion.identity;
    public bool HasTrainingFrame { get; private set; }

    public void SetTrainingFrame(Vector3 origin, Quaternion rotation)
    {
        TrainingOrigin = origin;
        TrainingRotation = rotation;
        HasTrainingFrame = true;
        _lastPos = Vector3.positiveInfinity;
    }

    private Vector3 _lastPos = Vector3.zero;
    private Material _ballMat;
    private Coroutine _feedbackRoutine;

    private void Awake()
    {
        if (ballRenderer != null) _ballMat = ballRenderer.material;
    }

    /// <summary>出下一个球：随机位置(避免与上一次过近)、开始闪烁</summary>
    public void SpawnNext()
    {
        TrySpawnNext();
    }

    public bool TrySpawnNext()
    {
        if (ball == null || config == null || !HasTrainingFrame) return false;
        if (!TryPickSpawnPosition(TrainingOrigin, TrainingRotation, config, _lastPos, out Vector3 p))
            return false;
        _lastPos = p;
        ball.position = p;

        // 先激活球：同步触发 SSVEPFlicker.Awake，初始化其 _sw/_mat。
        // 否则 inactive 球上组件尚未 Awake，直接 StartFlicker 会 NullReference(_sw)。
        ball.gameObject.SetActive(true);

        if (flicker != null)
        {
            flicker.Configure(config.flickerHz, config.dutyCycle, config.onColor, config.offColor);
            flicker.ResetMaterial();
            flicker.StartFlicker();
        }
        return true;
    }

    // Fixed training frame captured ONCE on room entry. Head rotation and position
    // never change this region, including when the next ball or next round starts.
    public static bool TryPickSpawnPosition(Vector3 origin, Quaternion rotation, SessionConfig settings, Vector3 last, out Vector3 position)
    {
        position = default;
        if (settings == null) return false;
        float radius = Mathf.Max(0.01f, settings.ballDiameter * 0.5f);
        float dMin = Mathf.Max(0.5f + radius, Mathf.Min(settings.spawnZRange.x, settings.spawnZRange.y));
        float dMax = Mathf.Max(dMin, Mathf.Max(settings.spawnZRange.x, settings.spawnZRange.y));
        float xMin = Mathf.Max(-4f + radius + 0.1f, Mathf.Min(settings.spawnXRange.x, settings.spawnXRange.y));
        float xMax = Mathf.Min(4f - radius - 0.1f, Mathf.Max(settings.spawnXRange.x, settings.spawnXRange.y));
        float yMin = Mathf.Max(radius + 0.1f, Mathf.Min(settings.spawnYRange.x, settings.spawnYRange.y));
        float yMax = Mathf.Min(3.5f - radius - 0.1f, Mathf.Max(settings.spawnYRange.x, settings.spawnYRange.y));
        bool found = false;
        for (int i = 0; i < 64; i++)
        {
            float depth = Random.Range(dMin, dMax);
            float halfWidth = Mathf.Min(3f, depth * Mathf.Tan(22f * Mathf.Deg2Rad));
            Vector3 local = new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(yMin, yMax) - origin.y, depth);
            Vector3 world = origin + rotation * local;
            if (world.x < xMin || world.x > xMax || world.y < yMin || world.y > yMax ||
                world.z < radius + 0.1f || world.z > 12f - radius - 0.1f) continue;
            position = world;
            found = true;
            if (Vector3.Distance(world, last) > 1.2f) return true;
        }
        // Invalid room/configuration: fail safely. Do not change the user's frame.
        return found;
    }

    /// <summary>命中反馈：变绿,停闪烁</summary>
    public void PlayHitFeedback(Color c, float duration)
    {
        StopFlicker();
        SetBallColor(c);
    }

    /// <summary>漏失反馈：变灰,停闪烁</summary>
    public void PlayMissFeedback(Color c, float duration)
    {
        StopFlicker();
        SetBallColor(c);
    }

    /// <summary>收球：隐藏,停闪烁</summary>
    public void DespawnCurrent()
    {
        StopFlicker();
        if (ball != null) ball.gameObject.SetActive(false);
    }

    private void StopFlicker()
    {
        if (flicker != null) flicker.StopFlicker();
    }

    private void SetBallColor(Color c)
    {
        if (_ballMat != null)
        {
            _ballMat.color = c;
            _ballMat.EnableKeyword("_EMISSION");
            _ballMat.SetColor("_EmissionColor", c * 0.8f);
        }
    }
}
