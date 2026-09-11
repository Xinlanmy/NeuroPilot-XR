using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace NeuroPilotXR.Navigation
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SpatialWindowDragController : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private Image dragHandle;
        [SerializeField] private Transform viewer;
        [SerializeField] private float hoverScale = 1.01f;
        [SerializeField] private float grabScale = 1.015f;

        private Vector3 baseScale;
        private Rigidbody windowBody;
        private bool hovered;
        private bool grabbed;

        public void Configure(XRGrabInteractable interactable, Image handle)
        {
            grabInteractable = interactable;
            dragHandle = handle;
        }

        private void Awake()
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();

            // Rotation is controlled from the viewer pose while dragging. Letting
            // XRGrabInteractable also track controller rotation causes a visible fight.
            grabInteractable.trackRotation = false;
            windowBody = GetComponent<Rigidbody>();
            baseScale = transform.localScale;
            UpdateHandle();
        }

        private void OnEnable()
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            if (grabInteractable == null)
                return;

            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
            hovered = false;
            grabbed = false;
            transform.localScale = baseScale;
        }

        private void Update()
        {
            float multiplier = grabbed ? grabScale : hovered ? hoverScale : 1f;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                baseScale * multiplier,
                1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        private void LateUpdate()
        {
            if (!grabbed || !ResolveViewer() ||
                !TryCalculateFacingRotation(transform.position, viewer.position, out Quaternion rotation))
                return;

            transform.rotation = rotation;
            if (windowBody != null)
                windowBody.rotation = rotation;
        }

        /// <summary>
        /// Calculates an upright window rotation whose visible Canvas face (-Z)
        /// points toward the viewer. Vertical head offset never tilts the window.
        /// </summary>
        public static bool TryCalculateFacingRotation(Vector3 windowPosition, Vector3 viewerPosition,
            out Quaternion rotation)
        {
            Vector3 awayFromViewer = Vector3.ProjectOnPlane(windowPosition - viewerPosition, Vector3.up);
            if (!IsFinite(awayFromViewer) || awayFromViewer.sqrMagnitude < 0.0001f)
            {
                rotation = Quaternion.identity;
                return false;
            }

            rotation = Quaternion.LookRotation(awayFromViewer.normalized, Vector3.up);
            return true;
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            hovered = true;
            UpdateHandle();
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            hovered = grabInteractable.isHovered;
            UpdateHandle();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            grabbed = true;
            UpdateHandle();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            grabbed = false;
            if (!args.isCanceled)
                KeepInComfortZone();
            UpdateHandle();
        }

        private void UpdateHandle()
        {
            if (dragHandle == null)
                return;

            float alpha = grabbed ? 1f : hovered ? 0.92f : 0.55f;
            dragHandle.color = new Color(1f, 1f, 1f, alpha);
        }

        private void KeepInComfortZone()
        {
            Camera viewer = Camera.main;
            if (viewer == null)
                return;

            Vector3 offset = transform.position - viewer.transform.position;
            float distance = Mathf.Clamp(offset.magnitude, 0.55f, 3.5f);
            if (offset.sqrMagnitude < 0.0001f)
                offset = viewer.transform.forward;

            Vector3 position = viewer.transform.position + offset.normalized * distance;
            position.y = Mathf.Clamp(position.y, viewer.transform.position.y - 0.9f, viewer.transform.position.y + 1.1f);
            transform.position = position;
        }

        private bool ResolveViewer()
        {
            if (viewer == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    viewer = mainCamera.transform;
            }

            return viewer != null && viewer.gameObject.activeInHierarchy && IsFinite(viewer.position);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
