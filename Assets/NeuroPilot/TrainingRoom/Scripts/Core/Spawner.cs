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
    public Camera viewCamera;           // 视野锥基准相机(不依赖 tag)

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
        if (ball == null) return;

        Vector3 p = PickSpawnPosition();
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
    }

    private Vector3 PickSpawnPosition()
    {
        // 视野锥约束：球只落在被试正前方视野内（不跑到身侧/身后）。
        // 用相机局部空间生成：局部坐标 (x=水平, y=垂直, z=前向深度)，
        // 深度在 spawnZRange 内随机，x/y 由该深度处的可视半宽/半高决定，留 viewMargin 边距。
        // 即使 VR 头显旋转，球也始终落在眼前视野内。
        Camera cam = (viewCamera != null) ? viewCamera : Camera.main;
        Vector3 camPos = (cam != null) ? cam.transform.position : new Vector3(0f, 1.6f, 2.5f);
        Quaternion camRot = (cam != null) ? cam.transform.rotation : Quaternion.identity;
        float fov = (cam != null) ? cam.fieldOfView : 60f;
        float aspect = (cam != null && cam.aspect > 0f) ? cam.aspect : (16f / 9f);

        float yMin = Mathf.Min(config.spawnYRange.x, config.spawnYRange.y);
        float yMax = Mathf.Max(config.spawnYRange.x, config.spawnYRange.y);
        float dMin = Mathf.Min(config.spawnZRange.x, config.spawnZRange.y);
        float dMax = Mathf.Max(config.spawnZRange.x, config.spawnZRange.y);

        float tanHalfV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        float tanHalfH = tanHalfV * aspect;

        // 与上一球最小间隔 1.2m，避免原地复出
        for (int i = 0; i < 40; i++)
        {
            float depth = Random.Range(dMin, dMax);

            // 该深度处可视半宽/半高（收缩 margin，球有直径且贴边观感差）
            float halfW = Mathf.Max(0.3f, depth * tanHalfH - config.viewMargin);
            float halfH = Mathf.Max(0.3f, depth * tanHalfV - config.viewMargin);

            // 相机局部坐标 → 世界坐标
            Vector3 local = new Vector3(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH), depth);
            Vector3 world = camPos + camRot * local;

            // 房间边界保护：不嵌墙、不穿地板/天花板
            float xMin = Mathf.Min(config.spawnXRange.x, config.spawnXRange.y);
            float xMax = Mathf.Max(config.spawnXRange.x, config.spawnXRange.y);
            world.x = Mathf.Clamp(world.x, xMin, xMax);
            world.y = Mathf.Clamp(world.y, yMin, yMax);
            world.z = Mathf.Clamp(world.z, camPos.z + 0.8f, 11f); // 前方 0.8m 起，前墙 z=12 内

            if (Vector3.Distance(world, _lastPos) > 1.2f || i == 39)
            {
                _lastPos = world;
                return world;
            }
        }

        // 兜底：正前方中央
        _lastPos = camPos + camRot * new Vector3(0f, 0f, (dMin + dMax) * 0.5f);
        return _lastPos;
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
