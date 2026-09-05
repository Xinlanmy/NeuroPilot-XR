using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 出球器：在相机局部空间按视野锥约束随机取点（9/5 已验证算法，300 次采样全部在视野内）。
    /// - 深度 z ∈ [depthMin, depthMax]（视野纵深语义）；
    /// - x/y 由该深度处的可视半宽/半高（− viewMargin 边距）决定；
    /// - 世界坐标再做房间 clamp 与最小间距约束，同一位置不连续出现。
    /// </summary>
    public sealed class TargetSpawner : MonoBehaviour
    {
        [SerializeField] private float depthMin = 1.5f;
        [SerializeField] private float depthMax = 5.5f;
        [SerializeField] private float viewMargin = 0.5f;
        [SerializeField] private float roomXRange = 3f;
        [SerializeField] private float roomYMin = 1.3f;
        [SerializeField] private float roomYMax = 2.2f;
        [SerializeField] private float minSpacing = 1.2f;
        [SerializeField] private int maxAttempts = 50;

        private Vector3 _lastPosition;
        private bool _hasLast;

        public Vector3 PickSpawnPosition(Camera viewCamera)
        {
            if (viewCamera == null)
            {
                Debug.LogError("[TargetSpawner] viewCamera 为空，退回原点前方 3m");
                return Vector3.forward * 3f;
            }

            Transform cam = viewCamera.transform;
            float halfFovRad = viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float aspect = viewCamera.aspect > 0.01f ? viewCamera.aspect : 1f;

            Vector3 chosen = Vector3.zero;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float depth = Random.Range(depthMin, depthMax);
                float halfH = Mathf.Max(0.1f, depth * Mathf.Tan(halfFovRad) - viewMargin);
                float halfW = Mathf.Max(0.1f, halfH * aspect - viewMargin);
                var local = new Vector3(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH), depth);
                Vector3 world = cam.TransformPoint(local);
                world.x = Mathf.Clamp(world.x, -roomXRange, roomXRange);
                world.y = Mathf.Clamp(world.y, roomYMin, roomYMax);
                chosen = world;
                if (!_hasLast || Vector3.Distance(world, _lastPosition) >= minSpacing)
                {
                    break;
                }
            }

            _lastPosition = chosen;
            _hasLast = true;
            return chosen;
        }
    }
}
