using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

namespace NeuroPilotXR.Navigation
{
    /// <summary>Which runtime path produced the most recent eye pose.</summary>
    public enum GazeSource { None, OpenXrExt, ViveHtc }

    /// <summary>
    /// Real eye pose, expressed in tracking space, never head-gaze fallback.
    /// Two runtime paths are attempted in order because they are not interchangeable on hardware:
    /// 1. Unity's Eye Gaze Interaction profile (XR_EXT_eye_gaze_interaction). Unity disables this
    ///    feature by itself when the runtime does not enable the extension, and then no device with
    ///    the EyeTracking characteristic ever appears in UnityEngine.XR.InputDevices.
    /// 2. VIVE's own tracker (XR_HTC_eye_tracker), which Focus Vision headsets ship regardless.
    /// The public diagnostic properties exist so a headset build can report which half is missing
    /// instead of failing silently.
    /// </summary>
    public sealed class EyeGazeProvider : MonoBehaviour
    {
        public XROrigin origin;
        private readonly List<InputDevice> devices = new List<InputDevice>();
        private bool focused = true;

        /// <summary>Source of the last sample; None means no eye pose was available.</summary>
        public GazeSource Source { get; private set; }
        /// <summary>Eye devices exposed through UnityEngine.XR.InputDevices.</summary>
        public int LegacyDeviceCount { get; private set; }
        /// <summary>Tracking state reported by the legacy eye device.</summary>
        public InputTrackingState LegacyState { get; private set; }
        /// <summary>Frames probed, and frames that yielded a usable ray.</summary>
        public int SampleCount { get; private set; }
        public int ValidSampleCount { get; private set; }
        /// <summary>Runtime support for the two eye tracking extensions.</summary>
        public bool ExtGazeEnabled { get; private set; }
        public bool HtcTrackerEnabled { get; private set; }
        /// <summary>One-line on-device readout; safe to show in the HUD.</summary>
        public string Diagnostic => "标准眼动" + (ExtGazeEnabled ? "可用" : "未启用") +
            " VIVE眼动" + (HtcTrackerEnabled ? "可用" : "未启用") +
            " 设备" + LegacyDeviceCount + " 有效帧" + ValidSampleCount + "/" + SampleCount;

#if UNITY_EDITOR
        public bool TestOverride, TestValid;
        public Ray TestRay;
#endif

        private int cachedFrame = -1;
        private bool cachedValid;
        private Ray cachedRay;
        private float nextExtensionProbe;
        private int viveFailures;
        private bool extensionsProbed;

        private void OnApplicationFocus(bool value) => focused = value;

        /// <summary>
        /// Extension support is fixed once the OpenXR instance exists, and the instance is created
        /// after this component awakes, so probe until support is reported and then stop.
        /// </summary>
        private void RefreshExtensions()
        {
            // Once the instance has been created the answer is fixed, so stop probing. Without this
            // latch a path that disabled itself after repeated failures would be re-enabled every second.
            if (extensionsProbed) return;
            if (Time.unscaledTime < nextExtensionProbe) return;
            nextExtensionProbe = Time.unscaledTime + 1f;
            // A diagnostic probe must never take the training down.
            try
            {
                ExtGazeEnabled = OpenXRRuntime.IsExtensionEnabled("XR_EXT_eye_gaze_interaction");
                HtcTrackerEnabled = OpenXRRuntime.IsExtensionEnabled("XR_HTC_eye_tracker");
                extensionsProbed = ExtGazeEnabled || HtcTrackerEnabled;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Eye extension probe failed: " + error.Message);
            }
        }

        public bool TryGetRay(out Ray ray)
        {
            ray = default;
#if UNITY_EDITOR
            if (TestOverride) { ray = TestRay; return TestValid; }
#endif
            if (!focused || origin == null || origin.Camera == null) return false;
            // Update and Ready both poll every frame; probe the runtime once per frame only.
            if (Time.frameCount == cachedFrame) { ray = cachedRay; return cachedValid; }
            cachedFrame = Time.frameCount;
            RefreshExtensions();
            if (TryOpenXrEyeGaze(out cachedRay)) Source = GazeSource.OpenXrExt;
            else if (TryViveEyeTracker(out cachedRay)) Source = GazeSource.ViveHtc;
            else Source = GazeSource.None;
            cachedValid = Source != GazeSource.None;
            SampleCount++;
            if (cachedValid) ValidSampleCount++;
            ray = cachedRay;
            return cachedValid;
        }

        /// <summary>Unity's standard XR_EXT_eye_gaze_interaction device.</summary>
        private bool TryOpenXrEyeGaze(out Ray ray)
        {
            ray = default;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, devices);
            LegacyDeviceCount = devices.Count;
            foreach (var device in devices)
            {
                if (!device.isValid) continue;
                if (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked) continue;
                // A property cannot be passed as an out parameter, so read into a local first.
                device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state);
                LegacyState = state;
                // Rotation alone is enough to build a gaze ray. Some runtimes report orientation
                // without a position fix, and demanding both silently throws usable gaze away.
                if ((LegacyState & InputTrackingState.Rotation) == 0) continue;
                if (!device.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out Quaternion rotation)) continue;
                Vector3 direction = rotation * Vector3.forward;
                if (!Finite(direction) || direction.sqrMagnitude < 0.5f) continue;
                // The camera's parent includes the XR Origin and floor-offset transforms, not head rotation.
                Transform trackingSpace = origin.Camera.transform.parent;
                Vector3 position = default;
                bool hasPosition = (LegacyState & InputTrackingState.Position) != 0 &&
                    device.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out position) && Finite(position);
                Vector3 worldDirection = trackingSpace != null
                    ? trackingSpace.TransformDirection(direction).normalized
                    : direction.normalized;
                if (hasPosition)
                    ray = new Ray(trackingSpace != null ? trackingSpace.TransformPoint(position) : position, worldDirection);
                else
                    ray = new Ray(origin.Camera.transform.position, worldDirection);
                return true;
            }
            return false;
        }

        /// <summary>VIVE's proprietary XR_HTC_eye_tracker, which Focus Vision exposes directly.</summary>
        private bool TryViveEyeTracker(out Ray ray)
        {
            ray = default;
            if (!HtcTrackerEnabled) return false;
            // Outside an active XR session there is no frame state to query, and the native call
            // would fault, so a desktop Editor run simply has no VIVE path.
            if (!XRSettings.isDeviceActive) return false;
            try
            {
                OpenXRSettings settings = OpenXRSettings.Instance;
                if (settings == null) return false;
                ViveEyeTracker feature = settings.GetFeature<ViveEyeTracker>();
                if (feature == null) return false;
                if (!feature.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes) || gazes == null) return false;
                Vector3 positionSum = Vector3.zero, directionSum = Vector3.zero;
                int count = 0;
                for (int i = 0; i < gazes.Length; i++)
                {
                    XrSingleEyeGazeDataHTC gaze = gazes[i];
                    if ((uint)gaze.isValid == 0u) continue;
                    Vector3 position = new Vector3(gaze.gazePose.position.x, gaze.gazePose.position.y, gaze.gazePose.position.z);
                    Quaternion rotation = new Quaternion(gaze.gazePose.orientation.x, gaze.gazePose.orientation.y,
                        gaze.gazePose.orientation.z, gaze.gazePose.orientation.w);
                    Vector3 direction = rotation * Vector3.forward;
                    if (!Finite(position) || !Finite(direction) || direction.sqrMagnitude < 0.5f) continue;
                    positionSum += position; directionSum += direction.normalized; count++;
                }
                if (count == 0) return false;
                // Averaging both eyes yields a cyclopean origin, which is what a single gaze ray needs.
                Vector3 localPosition = positionSum / count;
                Vector3 localDirection = (directionSum / count).normalized;
                Transform trackingSpace = origin.Camera.transform.parent;
                ray = trackingSpace != null
                    ? new Ray(trackingSpace.TransformPoint(localPosition), trackingSpace.TransformDirection(localDirection).normalized)
                    : new Ray(localPosition, localDirection);
                return true;
            }
            catch (System.Exception error)
            {
                // A broken native path must not spam the log once per frame forever.
                if (++viveFailures >= 5)
                {
                    HtcTrackerEnabled = false;
                    Debug.LogWarning("VIVE eye tracker disabled after repeated failures: " + error.Message);
                }
                return false;
            }
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
