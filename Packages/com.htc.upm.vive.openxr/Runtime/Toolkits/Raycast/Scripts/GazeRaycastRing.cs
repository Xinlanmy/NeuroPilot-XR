// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VIVE.OpenXR.Raycast
{
    public class GazeRaycastRing : RaycastRing
    {
        const string LOG_TAG = "VIVE.OpenXR.Raycast.GazeRaycastRing";
        void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }

        #region Inspector
        [SerializeField]
        [Tooltip("Use Eye Tracking data for Gaze.")]
        private bool m_EyeTracking = false;
        public bool EyeTracking { get { return m_EyeTracking; } set { m_EyeTracking = value; } }

        [SerializeField]
        private InputActionReference m_EyePose = null;
        public InputActionReference EyePose { get => m_EyePose; set => m_EyePose = value; }
        bool getTracked(InputActionReference actionReference)
        {
            bool tracked = false;

            if (OpenXRHelper.VALIDATE(actionReference, out string value))
            {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.InputSystem.XR.PoseState))
#else
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.XR.OpenXR.Input.Pose))
#endif
                {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                    tracked = actionReference.action.ReadValue<UnityEngine.InputSystem.XR.PoseState>().isTracked;
#else
                    tracked = actionReference.action.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>().isTracked;
#endif
                    if (printIntervalLog)
                    {
                        sb.Clear().Append("getTracked(").Append(tracked).Append(")");
                        DEBUG(sb);
                    }
                }
            }
            else
            {
                if (printIntervalLog)
                {
                    sb.Clear().Append("getTracked() invalid input: ").Append(value);
                    DEBUG(sb);
                }
            }

            return tracked;
        }
        InputTrackingState getTrackingState(InputActionReference actionReference)
        {
            InputTrackingState state = InputTrackingState.None;

            if (OpenXRHelper.VALIDATE(actionReference, out string value))
            {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.InputSystem.XR.PoseState))
#else
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.XR.OpenXR.Input.Pose))
#endif
                {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                    state = actionReference.action.ReadValue<UnityEngine.InputSystem.XR.PoseState>().trackingState;
#else
                    state = actionReference.action.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>().trackingState;
#endif
                    if (printIntervalLog)
                    {
                        sb.Clear().Append("getTrackingState(").Append(state).Append(")");
                        DEBUG(sb);
                    }
                }
            }
            else
            {
                if (printIntervalLog)
                {
                    sb.Clear().Append("getTrackingState() invalid input: ").Append(value);
                    DEBUG(sb);
                }
            }

            return state;
        }
        Vector3 getDirection(InputActionReference actionReference)
        {
            Quaternion rotation = Quaternion.identity;

            if (OpenXRHelper.VALIDATE(actionReference, out string value))
            {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.InputSystem.XR.PoseState))
#else
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.XR.OpenXR.Input.Pose))
#endif
                {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                    rotation = actionReference.action.ReadValue<UnityEngine.InputSystem.XR.PoseState>().rotation;
#else
                    rotation = actionReference.action.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>().rotation;
#endif
                    if (printIntervalLog)
                    {
                        sb.Clear().Append("getDirection(").Append(rotation.x).Append(", ").Append(rotation.y).Append(", ").Append(rotation.z).Append(", ").Append(rotation.w).Append(")");
                        DEBUG(sb);
                    }
                    return (rotation * Vector3.forward);
                }
            }
            else
            {
                if (printIntervalLog)
                {
                    sb.Clear().Append("getDirection() invalid input: ").Append(value);
                    DEBUG(sb);
                }
            }

            return Vector3.forward;
        }
        Vector3 getOrigin(InputActionReference actionReference)
        {
            var origin = Vector3.zero;

            if (OpenXRHelper.VALIDATE(actionReference, out string value))
            {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.InputSystem.XR.PoseState))
#else
                if (actionReference.action.activeControl.valueType == typeof(UnityEngine.XR.OpenXR.Input.Pose))
#endif
                {
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
                    origin = actionReference.action.ReadValue<UnityEngine.InputSystem.XR.PoseState>().position;
#else
                    origin = actionReference.action.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>().position;
#endif
                    if (printIntervalLog)
                    {
                        sb.Clear().Append("getOrigin(").Append(origin.x).Append(", ").Append(origin.y).Append(", ").Append(origin.z).Append(")");
                        DEBUG(sb);
                    }
                }
            }
            else
            {
                if (printIntervalLog)
                {
                    sb.Clear().Append("getOrigin() invalid input: ").Append(value);
                    DEBUG(sb);
                }
            }

            return origin;
        }

        [Tooltip("Event triggered by gaze.")]
        [SerializeField]
        private GazeEvent m_InputEvent = GazeEvent.Down;
        public GazeEvent InputEvent { get { return m_InputEvent; } set { m_InputEvent = value; } }

        [Tooltip("Keys for control.")]
        [SerializeField]
        private List<InputActionReference> m_ActionsKeys = new List<InputActionReference>();
        public List<InputActionReference> ActionKeys { get { return m_ActionsKeys; } set { m_ActionsKeys = value; } }

        bool getButton(InputActionReference actionReference)
        {
            if (OpenXRHelper.VALIDATE(actionReference, out string value))
            {
                if (actionReference.action.activeControl.valueType == typeof(bool))
                    return actionReference.action.ReadValue<bool>();
                if (actionReference.action.activeControl.valueType == typeof(float))
                    return actionReference.action.ReadValue<float>() > 0;
            }
            else
            {
                if (printIntervalLog)
                {
                    sb.Clear().Append("getButton() invalid input: ").Append(value);
                    DEBUG(sb);
                }
            }

            return false;
        }

        [SerializeField]
        private bool m_AlwaysEnable = false;
        public bool AlwaysEnable { get { return m_AlwaysEnable; } set { m_AlwaysEnable = value; } }
        #endregion

        #region MonoBehaviour overrides
        protected override void Awake()
        {
            base.Awake();
        }

        private bool m_KeyDown = false;
        protected override void Update()
        {
            base.Update();

            if (!IsInteractable()) { return; }

            m_KeyDown = ButtonPressed();
        }
        #endregion

        private bool IsInteractable()
        {
            bool enabled = RaycastSwitch.Gaze.Enabled;

            m_Interactable = (m_AlwaysEnable || enabled);

            if (printIntervalLog)
            {
                sb.Clear().Append("IsInteractable() enabled: ").Append(enabled).Append(", m_AlwaysEnable: ").Append(m_AlwaysEnable);
                DEBUG(sb);
            }

            return m_Interactable;
        }

        internal bool m_Down = false, m_Hold = false;
        private bool ButtonPressed()
        {
            if (m_ActionsKeys == null) { return false; }

            bool keyDown = false;
            for (int i = 0; i < m_ActionsKeys.Count; i++)
            {
                var pressed = getButton(m_ActionsKeys[i]);
                if (pressed)
                {
                    sb.Clear().Append("ButtonPressed()").Append(m_ActionsKeys[i].name).Append(" is pressed.");
                    DEBUG(sb);
                }
                keyDown |= pressed;
            }

            m_Down = false;
            if (!m_Hold) { m_Down |= keyDown; }
            m_Hold = keyDown;

            return m_Down;
        }

        protected override bool UseEyeData(out Vector3 direction)
        {
            bool isTracked = getTracked(m_EyePose);
            InputTrackingState trackingState = getTrackingState(m_EyePose);
            bool positionTracked = ((trackingState & InputTrackingState.Position) != 0);
            bool rotationTracked = ((trackingState & InputTrackingState.Rotation) != 0);

            bool useEye = m_EyeTracking
#if !UNITY_XR_OPENXR_1_6_0
                && isTracked // The isTracked value of Pose will always be flase in OpenXR 1.6.0
#endif
                //&& positionTracked
                && rotationTracked;

            getOrigin(m_EyePose);
            direction = getDirection(m_EyePose);

            if (printIntervalLog)
            {
                sb.Clear().Append("UseEyeData() m_EyeTracking: ").Append(m_EyeTracking)
                    .Append(", isTracked: ").Append(isTracked)
                    .Append(", trackingState: ").Append(trackingState)
                    .Append(", direction (").Append(direction.x).Append(", ").Append(direction.y).Append(", ").Append(direction.z).Append(")");
                DEBUG(sb);
            }

            if (!useEye) { return base.UseEyeData(out direction); }

            return useEye;
        }

        #region RaycastImpl Actions overrides
        protected override bool OnDown()
        {
            if (m_InputEvent != GazeEvent.Down) { return false; }

            bool down = false;
            if (m_RingPercent >= 100 || m_KeyDown)
            {
                m_RingPercent = 0;
                m_GazeOnTime = Time.unscaledTime;
                down = true;
                sb.Clear().Append("OnDown()"); DEBUG(sb);
            }

            return down;
        }
        protected override bool OnSubmit()
        {
            if (m_InputEvent != GazeEvent.Submit) { return false; }

            bool submit = false;
            if (m_RingPercent >= 100 || m_KeyDown)
            {
                m_RingPercent = 0;
                m_GazeOnTime = Time.unscaledTime;
                submit = true;
                sb.Clear().Append("OnSubmit()"); DEBUG(sb);
            }

            return submit;
        }
        #endregion
    }
}
