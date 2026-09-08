using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>Align the imported room once to the tracked viewer before its session starts.</summary>
public sealed class TrainingRoomEntry : MonoBehaviour
{
    public XROrigin origin;
    public SessionManager session;
    public Transform hud;
    public Vector3 TrainingOrigin { get; private set; }
    public bool IsReady { get; private set; }

    private void Awake()
    {
        if (session != null) session.enabled = false;
    }

    private IEnumerator Start()
    {
        float elapsed = 0f;
        float validFor = 0f;
        while (elapsed < 8f && validFor < 0.35f)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool valid = Application.platform != RuntimePlatform.Android && !head.isValid;
            if (head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
                head.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state))
                valid = (state & (InputTrackingState.Position | InputTrackingState.Rotation)) ==
                    (InputTrackingState.Position | InputTrackingState.Rotation);
            validFor = valid ? validFor + Time.unscaledDeltaTime : 0f;
        }
        if (origin != null && origin.Camera != null)
        {
            origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            var camera = origin.Camera.transform;
            float eyeHeight = camera.position.y;
            if (Application.platform != RuntimePlatform.Android &&
                !InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid && eyeHeight < 0.5f)
                eyeHeight = 1.6f; // Desktop preview has no tracked floor-relative eye height.
            origin.MoveCameraToWorldLocation(new Vector3(0f, eyeHeight, 2.5f));
            TrainingOrigin = camera.position;
            if (session != null && session.spawner != null)
                session.spawner.SetTrainingFrame(camera.position, Quaternion.identity);
            if (hud != null)
                hud.position = new Vector3(0f, Mathf.Max(camera.position.y, 1.6f), 5.2f);
        }
        IsReady = true;
        if (session != null) session.enabled = true;
    }
}
