using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace NeuroPilotXR.Navigation
{
    /// <summary>
    /// Places this world-space window once after tracking starts, and again when
    /// the application returns from the background. Never follows normal head motion.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class SpatialWindowInitialPlacement : MonoBehaviour
    {
        [SerializeField] private Transform viewer;
        [SerializeField, Min(0.55f)] private float initialDistance = 1.5f;
        [SerializeField] private float eyeHeightOffset = -0.10f;
        [SerializeField, Min(0.05f)] private float trackingGraceSeconds = 0.35f;
        [SerializeField, Min(1f)] private float maximumTrackingWaitSeconds = 8f;

        private Canvas[] canvases;
        private bool[] canvasStates;
        private BaseRaycaster[] raycasters;
        private bool[] raycasterStates;
        private XRGrabInteractable[] grabInteractables;
        private bool[] grabStates;
        private Rigidbody windowBody;
        private bool hidden;
        private bool waitingForPlacement;
        private bool applicationPaused;
        private float waitStartedAt;
        private float trackingValidSince = -1f;
        private int validTrackingFrames;
        private int waitStartedFrame;

        public bool IsWaitingForPlacement => waitingForPlacement;

        public void Configure(Transform viewerTransform, float distance = 1.5f)
        {
            viewer = viewerTransform;
            initialDistance = Mathf.Max(0.55f, distance);
        }

        public void Configure(Camera viewerCamera, float distance = 1.5f)
        {
            Configure(viewerCamera != null ? viewerCamera.transform : null, distance);
        }

        private void Awake()
        {
            windowBody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            BeginInitialPlacement();
        }

        private void OnDisable()
        {
            waitingForPlacement = false;
            RestoreVisibilityAndInteraction();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                applicationPaused = true;
                if (isActiveAndEnabled)
                    HideUntilPlaced();
                return;
            }

            // Unity also sends an initial false callback. Only a real background
            // round trip should rearm an already completed placement.
            if (!applicationPaused)
                return;

            applicationPaused = false;
            if (isActiveAndEnabled)
                BeginInitialPlacement();
        }

        /// <summary>Rearms one placement without storing the previous world position.</summary>
        public void BeginInitialPlacement()
        {
            if (!isActiveAndEnabled)
                return;

            waitingForPlacement = true;
            waitStartedAt = Time.realtimeSinceStartup;
            waitStartedFrame = Time.frameCount;
            trackingValidSince = -1f;
            validTrackingFrames = 0;
            HideUntilPlaced();
        }

        private void LateUpdate()
        {
            if (!waitingForPlacement || applicationPaused)
                return;

            float now = Time.realtimeSinceStartup;
            bool hasViewer = ResolveViewer();
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool trackedHead = HasTrackedHeadPose(head);
            bool expectsHeadset = Application.platform == RuntimePlatform.Android ||
                                  XRSettings.isDeviceActive || head.isValid;

            if (hasViewer && trackedHead)
            {
                if (trackingValidSince < 0f)
                    trackingValidSince = now;

                ++validTrackingFrames;
                // TrackedPoseDriver applies input before LateUpdate. A small grace
                // also lets XROrigin finish applying its tracking-origin offset.
                if (validTrackingFrames >= 2 && now - trackingValidSince >= trackingGraceSeconds)
                    PlaceInFrontOfViewer();
            }
            else
            {
                trackingValidSince = -1f;
                validTrackingFrames = 0;

                if (hasViewer && !expectsHeadset && Time.frameCount - waitStartedFrame >= 2 &&
                    now - waitStartedAt >= trackingGraceSeconds)
                    PlaceInFrontOfViewer();
            }

            if (!waitingForPlacement || now - waitStartedAt < maximumTrackingWaitSeconds)
                return;

            // A failed/missing headset must not leave the entire interface hidden.
            // Use the best available camera pose once; normal tracking recovery
            // never unexpectedly moves a window that the user may already be using.
            bool placed = PlaceInFrontOfViewer();
            if (!placed)
            {
                waitingForPlacement = false;
                RestoreVisibilityAndInteraction();
            }

            Debug.LogWarning(placed
                ? "[NeuroPilot] Head tracking did not become ready in time. Placed the window using the current viewer pose."
                : "[NeuroPilot] No viewer camera is available. Restored the window at its scene position.", this);
        }

        /// <summary>
        /// Immediately places the window using the current viewer world pose.
        /// Returns false if there is no usable viewer, leaving a pending wait active.
        /// </summary>
        public bool PlaceInFrontOfViewer()
        {
            if (!ResolveViewer())
                return false;

            Pose pose = CalculatePose(viewer.position, viewer.rotation, initialDistance, eyeHeightOffset);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (windowBody == null)
                windowBody = GetComponent<Rigidbody>();
            if (windowBody != null)
            {
                windowBody.position = pose.position;
                windowBody.rotation = pose.rotation;
                if (!windowBody.isKinematic)
                {
                    windowBody.velocity = Vector3.zero;
                    windowBody.angularVelocity = Vector3.zero;
                }
            }

            waitingForPlacement = false;
            RestoreVisibilityAndInteraction();
            return true;
        }

        /// <summary>
        /// Returns an upright Canvas pose whose +Z follows the viewer's horizontal
        /// forward direction. The canvas front therefore faces back toward the viewer.
        /// </summary>
        public static Pose CalculatePose(Vector3 viewerPosition, Quaternion viewerRotation,
            float distance = 1.5f, float heightOffset = -0.10f)
        {
            Vector3 horizontalForward = Vector3.ProjectOnPlane(viewerRotation * Vector3.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude < 0.0001f)
            {
                // Looking straight up/down still has a useful heading from head right.
                Vector3 horizontalRight = Vector3.ProjectOnPlane(viewerRotation * Vector3.right, Vector3.up);
                horizontalForward = Vector3.Cross(horizontalRight, Vector3.up);
            }
            if (horizontalForward.sqrMagnitude < 0.0001f)
                horizontalForward = Vector3.forward;

            horizontalForward.Normalize();
            return new Pose(
                viewerPosition + horizontalForward * Mathf.Max(0.55f, distance) + Vector3.up * heightOffset,
                Quaternion.LookRotation(horizontalForward, Vector3.up));
        }

        private bool ResolveViewer()
        {
            if (viewer == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    viewer = mainCamera.transform;
            }

            return viewer != null && viewer.gameObject.activeInHierarchy &&
                   IsFinite(viewer.position) && IsFinite(viewer.rotation);
        }

        private static bool HasTrackedHeadPose(InputDevice head)
        {
            const InputTrackingState required = InputTrackingState.Position | InputTrackingState.Rotation;
            return head.isValid &&
                   head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
                   head.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) &&
                   (state & required) == required &&
                   head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) && IsFinite(position) &&
                   head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation) && IsFinite(rotation);
        }

        private void HideUntilPlaced()
        {
            if (hidden)
                return;

            canvases = GetComponentsInChildren<Canvas>(true);
            canvasStates = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; ++i)
            {
                canvasStates[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }

            raycasters = GetComponentsInChildren<BaseRaycaster>(true);
            raycasterStates = new bool[raycasters.Length];
            for (int i = 0; i < raycasters.Length; ++i)
            {
                raycasterStates[i] = raycasters[i].enabled;
                raycasters[i].enabled = false;
            }

            grabInteractables = GetComponentsInChildren<XRGrabInteractable>(true);
            grabStates = new bool[grabInteractables.Length];
            for (int i = 0; i < grabInteractables.Length; ++i)
            {
                grabStates[i] = grabInteractables[i].enabled;
                grabInteractables[i].enabled = false;
            }

            hidden = true;
        }

        private void RestoreVisibilityAndInteraction()
        {
            if (!hidden)
                return;

            for (int i = 0; i < canvases.Length; ++i)
                if (canvases[i] != null)
                    canvases[i].enabled = canvasStates[i];
            for (int i = 0; i < raycasters.Length; ++i)
                if (raycasters[i] != null)
                    raycasters[i].enabled = raycasterStates[i];
            for (int i = 0; i < grabInteractables.Length; ++i)
                if (grabInteractables[i] != null)
                    grabInteractables[i].enabled = grabStates[i];

            hidden = false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w) &&
                   value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.0001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
