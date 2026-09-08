using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace NeuroPilotXR.Navigation
{
    /// <summary>Real OpenXR eye pose, expressed in tracking space, never head-gaze fallback.</summary>
    public sealed class EyeGazeProvider : MonoBehaviour
    {
        public XROrigin origin;
        private readonly List<InputDevice> devices = new List<InputDevice>();
        private bool focused = true;
#if UNITY_EDITOR
        public bool TestOverride, TestValid;
        public Ray TestRay;
#endif
        private void OnApplicationFocus(bool value) => focused = value;

        public bool TryGetRay(out Ray ray)
        {
            ray = default;
#if UNITY_EDITOR
            if (TestOverride) { ray = TestRay; return TestValid; }
#endif
            if (!focused || origin == null || origin.Camera == null) return false;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, devices);
            foreach (var device in devices)
            {
                if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked ||
                    !device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) ||
                    (state & (InputTrackingState.Position | InputTrackingState.Rotation)) != (InputTrackingState.Position | InputTrackingState.Rotation) ||
                    !device.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out Vector3 position) ||
                    !device.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out Quaternion rotation)) continue;
                Vector3 direction = rotation * Vector3.forward;
                if (!Finite(position) || !Finite(direction) || direction.sqrMagnitude < 0.5f) continue;
                // The camera's parent includes the XR Origin and floor-offset transforms, not head rotation.
                Transform trackingSpace = origin.Camera.transform.parent;
                ray = trackingSpace != null
                    ? new Ray(trackingSpace.TransformPoint(position), trackingSpace.TransformDirection(direction).normalized)
                    : new Ray(position, direction.normalized);
                return true;
            }
            return false;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
