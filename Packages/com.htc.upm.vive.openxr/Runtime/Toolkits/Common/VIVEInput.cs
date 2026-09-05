// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Hand;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.OpenXR;
#endif

namespace VIVE.OpenXR.Toolkits.Common
{
    public enum DeviceCategory
    {
        None = 0,
        HMD = 1,
        CenterEye = 2,
        LeftController = 3,
        RightController = 4,
        LeftHand = 5,
        RightHand = 6,
        Tracker0 = 7,
        Tracker1 = 8,
        Tracker2 = 9,
        Tracker3 = 10,
        Tracker4 = 11,
    }
    public enum PoseState
    {
        None = 0,
        IsTracked = 1,
        Position = 2,
        Rotation = 3,
        Velocity = 4,
        AngularVelocity = 5,
        Acceleration = 6,
        AngularAcceleration = 7,
    }
    public enum Handedness
    {
        None = -1,
        Right = 0,
        Left = 1,
    }
    public enum HandEvent
    {
        None = 0,
        PinchValue = 0x00000001,
        PinchPose = 0x00000002,
        GraspValue = 0x00000010,
        GraspPose = 0x00000020,
    }
    public enum ButtonEvent
    {
        None = 0,
        GripValue = 0x00000001,
        GripPress = 0x00000002,
        TriggerValue = 0x00000010,
        TriggerTouch = 0x00000020,
        TriggerPress = 0x00000040,
        Primary2DAxisValue = 0x00000100,
        Primary2DAxisTouch = 0x00000200,
        Primary2DAxisPress = 0x00000400,
        Secondary2DAxisValue = 0x00001000,
        Secondary2DAxisTouch = 0x00002000,
        Secondary2DAxisPress = 0x00004000,
        PrimaryButton = 0x00010000,
        SecondaryButton = 0x00020000,
        ParkingTouch = 0x00100000,
        Menu = 0x01000000,
    }
    public enum HandJointType : Int32
    {
        Palm = XrHandJointEXT.XR_HAND_JOINT_PALM_EXT,
        Wrist = XrHandJointEXT.XR_HAND_JOINT_WRIST_EXT,
        Thumb_Joint0 = XrHandJointEXT.XR_HAND_JOINT_THUMB_METACARPAL_EXT,
        Thumb_Joint1 = XrHandJointEXT.XR_HAND_JOINT_THUMB_PROXIMAL_EXT,
        Thumb_Joint2 = XrHandJointEXT.XR_HAND_JOINT_THUMB_DISTAL_EXT,
        Thumb_Tip = XrHandJointEXT.XR_HAND_JOINT_THUMB_TIP_EXT,
        Index_Joint0 = XrHandJointEXT.XR_HAND_JOINT_INDEX_METACARPAL_EXT,
        Index_Joint1 = XrHandJointEXT.XR_HAND_JOINT_INDEX_PROXIMAL_EXT,
        Index_Joint2 = XrHandJointEXT.XR_HAND_JOINT_INDEX_INTERMEDIATE_EXT,
        Index_Joint3 = XrHandJointEXT.XR_HAND_JOINT_INDEX_DISTAL_EXT,
        Index_Tip = XrHandJointEXT.XR_HAND_JOINT_INDEX_TIP_EXT,
        Middle_Joint0 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_METACARPAL_EXT,
        Middle_Joint1 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_PROXIMAL_EXT,
        Middle_Joint2 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_INTERMEDIATE_EXT,
        Middle_Joint3 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_DISTAL_EXT,
        Middle_Tip = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_TIP_EXT,
        Ring_Joint0 = XrHandJointEXT.XR_HAND_JOINT_RING_METACARPAL_EXT,
        Ring_Joint1 = XrHandJointEXT.XR_HAND_JOINT_RING_PROXIMAL_EXT,
        Ring_Joint2 = XrHandJointEXT.XR_HAND_JOINT_RING_INTERMEDIATE_EXT,
        Ring_Joint3 = XrHandJointEXT.XR_HAND_JOINT_RING_DISTAL_EXT,
        Ring_Tip = XrHandJointEXT.XR_HAND_JOINT_RING_TIP_EXT,
        Pinky_Joint0 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_METACARPAL_EXT,
        Pinky_Joint1 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_PROXIMAL_EXT,
        Pinky_Joint2 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_INTERMEDIATE_EXT,
        Pinky_Joint3 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_DISTAL_EXT,
        Pinky_Tip = XrHandJointEXT.XR_HAND_JOINT_LITTLE_TIP_EXT,
        Count = XrHandJointEXT.XR_HAND_JOINT_MAX_ENUM_EXT,
    }

    public static class VIVEInput
    {
        private const string kFloatType = "float";
        private const string kVector2Type = "Vector2";
        private const string kVector3Type = "Vector3";
        private const string kQuaternionType = "Quaternion";
        private const string kPoseType = "Pose";

        private struct InputActionMapping
        {
            public DeviceCategory device;
            public PoseState poseState;
            public ButtonEvent buttonEvent;
            public HandEvent handEvent;
            public InputAction inputAction { get; private set; }

            public InputActionMapping(string in_BindingPath, DeviceCategory in_Device,
                                      PoseState in_PoseState = PoseState.None,
                                      ButtonEvent in_ButtonEvent = ButtonEvent.None,
                                      HandEvent in_HandEvent = HandEvent.None,
                                      string in_Type = "")
            {
                inputAction = new InputAction(binding: in_BindingPath, expectedControlType: in_Type);
                inputAction.Enable();
                this.device = in_Device;
                this.poseState = in_PoseState;
                this.buttonEvent = in_ButtonEvent;
                this.handEvent = in_HandEvent;
            }

            public static InputActionMapping Identify => new InputActionMapping("", DeviceCategory.None);

            public override bool Equals(object obj)
            {
                return obj is InputActionMapping inputActionMapping &&
                       device == inputActionMapping.device &&
                       poseState == inputActionMapping.poseState &&
                       buttonEvent == inputActionMapping.buttonEvent &&
                       handEvent == inputActionMapping.handEvent &&
                       inputAction == inputActionMapping.inputAction;
            }
            public override int GetHashCode()
            {
                return device.GetHashCode() ^ poseState.GetHashCode() ^ buttonEvent.GetHashCode() ^ handEvent.GetHashCode() ^ inputAction.GetHashCode();
            }
            public static bool operator ==(InputActionMapping source, InputActionMapping target) => source.Equals(target);
            public static bool operator !=(InputActionMapping source, InputActionMapping target) => !(source == (target));
        }
        private struct JointData
        {
            public bool isValid { get; private set; }
            public Vector3 position { get; private set; }
            public Quaternion rotation { get; private set; }

            public JointData(bool in_IsValid, Vector3 in_Position, Quaternion in_Rotation)
            {
                this.isValid = in_IsValid;
                this.position = in_Position;
                this.rotation = in_Rotation;
            }

            public static JointData Identify => new JointData(false, Vector3.zero, Quaternion.identity);
        }
        private struct HandData
        {
            public bool isTracked { get; private set; }
            public int updateTime { get; private set; }
            public JointData[] joints { get; private set; }
            private JointData[] jointBuffer;

            public HandData(JointData[] in_Joints)
            {
                jointBuffer = new JointData[(int)HandJointType.Count];
                for (int i = 0; i < in_Joints.Length; i++)
                {
                    jointBuffer[i] = in_Joints[i];
                }
                this.joints = jointBuffer;
                isTracked = true;
                for (int i = 0; i < this.joints.Length; i++)
                {
                    if (!this.joints[i].isValid)
                    {
                        isTracked = false;
                        break;
                    }
                }
                updateTime = Time.frameCount;
            }

            public void Update(JointData[] in_Joints)
            {
                for (int i = 0; i < in_Joints.Length; i++)
                {
                    jointBuffer[i] = in_Joints[i];
                }
                this.joints = jointBuffer;
                isTracked = true;
                for (int i = 0; i < this.joints.Length; i++)
                {
                    if (!this.joints[i].isValid)
                    {
                        isTracked = false;
                        break;
                    }
                }
                updateTime = Time.frameCount;
            }

            public static HandData Identify
            {
                get
                {
                    JointData[] newJoints = new JointData[(int)HandJointType.Count];
                    for (int i = 0; i < newJoints.Length; i++)
                    {
                        newJoints[i] = JointData.Identify;
                    }
                    return new HandData(newJoints);
                }
            }
        }

        private static bool m_IsInitInputActions = false;
        private static bool m_IsSupportViveHand = false;
        private static bool m_IsSupportXrHand = false;
        private static List<InputActionMapping> s_InputActions = new List<InputActionMapping>();
        private static HandData m_LeftHand = HandData.Identify;
        private static HandData m_RightHand = HandData.Identify;
        private static JointData[] m_JointBuffer = new JointData[(int)HandJointType.Count];
#if UNITY_XR_HANDS
        private static XRHandSubsystem m_HandSubsystem = null;
        private static List<XRHandSubsystem> m_HandSubsystems = new List<XRHandSubsystem>();
#endif

        #region Public Interface

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.IsTracked)
            {
                eventResult = IsHandTracked(GetHandedness(device));
                return true;
            }
            else
            {
                if (GetInputActionMapping(device, poseState, out InputActionMapping inputActionMapping))
                {
                    var inputAction = inputActionMapping.inputAction;
                    if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                    {
                        eventResult = inputActionMapping.inputAction.ReadValue<float>() > 0;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out Vector3 eventResult)
        {
            CheckInitialize();
            eventResult = Vector3.zero;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.Position)
            {
                GetJointPose(GetHandedness(device), HandJointType.Wrist, out Pose jointPose);
                eventResult = jointPose.position;
                return true;
            }
            else
            {
                if (GetInputActionMapping(device, poseState, out InputActionMapping inputActionMapping))
                {
                    var inputAction = inputActionMapping.inputAction;
                    if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kVector3Type)
                    {
                        eventResult = inputActionMapping.inputAction.ReadValue<Vector3>();
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out Quaternion eventResult)
        {
            CheckInitialize();
            eventResult = Quaternion.identity;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.Rotation)
            {
                GetJointPose(GetHandedness(device), HandJointType.Wrist, out Pose jointPose);
                eventResult = jointPose.rotation;
                return true;
            }
            else
            {
                if (GetInputActionMapping(device, poseState, out InputActionMapping inputActionMapping))
                {
                    var inputAction = inputActionMapping.inputAction;
                    if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kQuaternionType)
                    {
                        eventResult = inputActionMapping.inputAction.ReadValue<Quaternion>();
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Check if a specified button event has toggled at this frame and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output whether the button event has toggled.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonDown(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            if (GetInputActionMapping(GetController(handedness), buttonEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                {
                    eventResult = inputActionMapping.inputAction.WasPressedThisFrame();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled at this frame and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output whether the button event has toggled.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonUp(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            if (GetInputActionMapping(GetController(handedness), buttonEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                {
                    eventResult = inputActionMapping.inputAction.WasReleasedThisFrame();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            if (GetInputActionMapping(GetController(handedness), buttonEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>() == 1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out float eventResult)
        {
            CheckInitialize();
            eventResult = 0f;
            if (GetInputActionMapping(GetController(handedness), buttonEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out Vector2 eventResult)
        {
            CheckInitialize();
            eventResult = Vector2.zero;
            if (GetInputActionMapping(GetController(handedness), buttonEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kVector2Type)
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<Vector2>();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified hand event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the hand event for.</param>
        /// <param name="handEvent">The specified hand event to check.</param>
        /// <param name="eventResult">Output for the hand event.</param>
        /// <returns>Returns true if the hand event was successfully retrieved, otherwise false.</returns>
        public static bool GetHandValue(Handedness handedness, HandEvent handEvent, out float eventResult)
        {
            CheckInitialize();
            eventResult = 0;
            if (GetInputActionMapping(GetHand(handedness), handEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kFloatType)
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified hand event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the hand event for.</param>
        /// <param name="handEvent">The specified hand event to check.</param>
        /// <param name="eventResult">Output for the hand event.</param>
        /// <returns>Returns true if the hand event was successfully retrieved, otherwise false.</returns>
        public static bool GetHandValue(Handedness handedness, HandEvent handEvent, out Pose eventResult)
        {
            CheckInitialize();
            eventResult = Pose.identity;
            if (GetInputActionMapping(GetHand(handedness), handEvent, out InputActionMapping inputActionMapping))
            {
                var inputAction = inputActionMapping.inputAction;
                if (inputAction != null && inputAction.enabled && inputAction.expectedControlType == kPoseType)
                {
# if USE_INPUT_SYSTEM_POSE_CONTROL
                    UnityEngine.InputSystem.XR.PoseState pose = inputActionMapping.inputAction.ReadValue<UnityEngine.InputSystem.XR.PoseState>();
#else
                    UnityEngine.XR.OpenXR.Input.Pose pose = inputActionMapping.inputAction.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>();
#endif
                    eventResult = new Pose(pose.position, pose.rotation);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves the pose of a specified hand joint for the given handedness.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to get the joint pose for.</param>
        /// <param name="joint">The specific hand joint to retrieve the pose of.</param>
        /// <param name="jointPose">Outputs the pose of the specified hand joint.</param>
        /// <returns>Returns true if the joint pose was successfully retrieved, otherwise false.</returns>
        public static bool GetJointPose(Handedness handedness, HandJointType joint, out Pose jointPose)
        {
            CheckHandUpdated();
            jointPose = Pose.identity;
            if (handedness == Handedness.Left)
            {
                jointPose = new Pose(m_LeftHand.joints[(int)joint].position, m_LeftHand.joints[(int)joint].rotation);
                return m_LeftHand.joints[(int)joint].isValid;
            }
            else
            {
                jointPose = new Pose(m_RightHand.joints[(int)joint].position, m_RightHand.joints[(int)joint].rotation);
                return m_RightHand.joints[(int)joint].isValid;
            }
        }

        /// <summary>
        /// Determines if the specified hand is currently being tracked.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check for tracking.</param>
        /// <returns>Returns true if the specified hand is being tracked, otherwise false.</returns>
        public static bool IsHandTracked(Handedness handedness)
        {
            CheckHandUpdated();
            return handedness == Handedness.Left ? m_LeftHand.isTracked : m_RightHand.isTracked;
        }

        public static bool IsHandValidate()
        {
            if (!m_IsInitInputActions)
            {
                ViveHandTracking viveHand = OpenXRSettings.Instance.GetFeature<ViveHandTracking>();
                if (viveHand)
                {
                    m_IsSupportViveHand = true;
                }
#if UNITY_XR_HANDS
                HandTracking xrHand = OpenXRSettings.Instance.GetFeature<HandTracking>();
                if (xrHand)
                {
                    m_IsSupportXrHand = true;
                }
#endif
            }
            return m_IsSupportViveHand || m_IsSupportXrHand;
        }

        #endregion

        [RuntimeInitializeOnLoadMethod]
        private static bool CheckInitialize()
        {
            if (!m_IsInitInputActions)
            {
                Initialized();
                IsHandValidate();
                m_IsInitInputActions = true;
            }
            return m_IsInitInputActions;
        }

        private static void Initialized()
        {
            #region Head
            s_InputActions.Add(new InputActionMapping("<XRHMD>/isTracked", DeviceCategory.HMD, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyePosition", DeviceCategory.HMD, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyeRotation", DeviceCategory.HMD, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyeVelocity", DeviceCategory.HMD, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAngularVelocity", DeviceCategory.HMD, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAcceleration", DeviceCategory.HMD, in_PoseState: PoseState.Acceleration, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAngularAcceleration", DeviceCategory.HMD, in_PoseState: PoseState.AngularAcceleration, in_Type: kVector3Type));
            #endregion
            #region Eye
            s_InputActions.Add(new InputActionMapping("<EyeGaze>/pose/isTracked", DeviceCategory.CenterEye, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<EyeGaze>/pose/position", DeviceCategory.CenterEye, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<EyeGaze>/pose/rotation", DeviceCategory.CenterEye, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<EyeGaze>/pose/velocity", DeviceCategory.CenterEye, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<EyeGaze>/pose/angularVelocity", DeviceCategory.CenterEye, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));
            #endregion
            #region Controller
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/isTracked", DeviceCategory.LeftController, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/pointerPosition", DeviceCategory.LeftController, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/pointerRotation", DeviceCategory.LeftController, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceVelocity", DeviceCategory.LeftController, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAngularVelocity", DeviceCategory.LeftController, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAcceleration", DeviceCategory.LeftController, in_PoseState: PoseState.Acceleration, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAngularAcceleration", DeviceCategory.LeftController, in_PoseState: PoseState.AngularAcceleration, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{grip}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.GripValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{gripButton}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.GripPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{trigger}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.TriggerValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/triggerTouched", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.TriggerTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{triggerButton}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.TriggerPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxis}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Primary2DAxisValue, in_Type: kVector2Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxisTouch}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Primary2DAxisTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxisClick}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Primary2DAxisPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxis}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Secondary2DAxisValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxisTouch}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Secondary2DAxisTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxisClick}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Secondary2DAxisPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primaryButton}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.PrimaryButton, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondaryButton}", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.SecondaryButton, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/parkingTouched", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.ParkingTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{LeftHand}/menu", DeviceCategory.LeftController, in_ButtonEvent: ButtonEvent.Menu, in_Type: kFloatType));

            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/isTracked", DeviceCategory.RightController, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/pointerPosition", DeviceCategory.RightController, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/pointerRotation", DeviceCategory.RightController, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceVelocity", DeviceCategory.RightController, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAngularVelocity", DeviceCategory.RightController, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAcceleration", DeviceCategory.RightController, in_PoseState: PoseState.Acceleration, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAngularAcceleration", DeviceCategory.RightController, in_PoseState: PoseState.AngularAcceleration, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{grip}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.GripValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{gripButton}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.GripPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{trigger}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.TriggerValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/triggerTouched", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.TriggerTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{triggerButton}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.TriggerPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxis}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Primary2DAxisValue, in_Type: kVector2Type));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxisTouch}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Primary2DAxisTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxisClick}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Primary2DAxisPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxis}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Secondary2DAxisValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxisTouch}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Secondary2DAxisTouch, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxisClick}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.Secondary2DAxisPress, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primaryButton}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.PrimaryButton, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondaryButton}", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.SecondaryButton, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<XRController>{RightHand}/parkingTouched", DeviceCategory.RightController, in_ButtonEvent: ButtonEvent.ParkingTouch, in_Type: kFloatType));
            #endregion
            #region Hand
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/selectValue", DeviceCategory.LeftHand, in_HandEvent: HandEvent.PinchValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/pointerPose", DeviceCategory.LeftHand, in_HandEvent: HandEvent.PinchPose, in_Type: kPoseType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/gripValue", DeviceCategory.LeftHand, in_HandEvent: HandEvent.GraspValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/devicePose", DeviceCategory.LeftHand, in_HandEvent: HandEvent.GraspPose, in_Type: kPoseType));

            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/selectValue", DeviceCategory.RightHand, in_HandEvent: HandEvent.PinchValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/pointerPose", DeviceCategory.RightHand, in_HandEvent: HandEvent.PinchPose, in_Type: kPoseType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/gripValue", DeviceCategory.RightHand, in_HandEvent: HandEvent.GraspValue, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/devicePose", DeviceCategory.RightHand, in_HandEvent: HandEvent.GraspPose, in_Type: kPoseType));
            #endregion
            #region Tracker
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/isTracked", DeviceCategory.Tracker0, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePosition", DeviceCategory.Tracker0, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/deviceRotation", DeviceCategory.Tracker0, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/velocity", DeviceCategory.Tracker0, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/angularVelocity", DeviceCategory.Tracker0, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));

            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/isTracked", DeviceCategory.Tracker1, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePosition", DeviceCategory.Tracker1, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/deviceRotation", DeviceCategory.Tracker1, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/velocity", DeviceCategory.Tracker1, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/angularVelocity", DeviceCategory.Tracker1, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));

            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/isTracked", DeviceCategory.Tracker2, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePosition", DeviceCategory.Tracker2, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/deviceRotation", DeviceCategory.Tracker2, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/velocity", DeviceCategory.Tracker2, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/angularVelocity", DeviceCategory.Tracker2, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));

            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/isTracked", DeviceCategory.Tracker3, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePosition", DeviceCategory.Tracker3, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/deviceRotation", DeviceCategory.Tracker3, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/velocity", DeviceCategory.Tracker3, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/angularVelocity", DeviceCategory.Tracker3, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));

            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/isTracked", DeviceCategory.Tracker4, in_PoseState: PoseState.IsTracked, in_Type: kFloatType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePosition", DeviceCategory.Tracker4, in_PoseState: PoseState.Position, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/deviceRotation", DeviceCategory.Tracker4, in_PoseState: PoseState.Rotation, in_Type: kQuaternionType));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/velocity", DeviceCategory.Tracker4, in_PoseState: PoseState.Velocity, in_Type: kVector3Type));
            s_InputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/angularVelocity", DeviceCategory.Tracker4, in_PoseState: PoseState.AngularVelocity, in_Type: kVector3Type));
            #endregion
        }

        private static bool GetInputActionMapping(DeviceCategory device, PoseState poseState, out InputActionMapping inputActionMapping)
        {
            inputActionMapping = default;
            for (int i = 0; i < s_InputActions.Count; i++)
            {
                var action = s_InputActions[i];
                if (action.device == device && action.poseState == poseState)
                {
                    inputActionMapping = action;
                    return true;
                }
            }
            return false;
        }

        private static bool GetInputActionMapping(DeviceCategory device, ButtonEvent buttonEvent, out InputActionMapping inputActionMapping)
        {
            inputActionMapping = default;
            for (int i = 0; i < s_InputActions.Count; i++)
            {
                var action = s_InputActions[i];
                if (action.device == device && action.buttonEvent == buttonEvent)
                {
                    inputActionMapping = action;
                    return true;
                }
            }
            return false;
        }

        private static bool GetInputActionMapping(DeviceCategory device, HandEvent handEvent, out InputActionMapping inputActionMapping)
        {
            inputActionMapping = default;
            for (int i = 0; i < s_InputActions.Count; i++)
            {
                var action = s_InputActions[i];
                if (action.device == device && action.handEvent == handEvent)
                {
                    inputActionMapping = action;
                    return true;
                }
            }
            return false;
        }

        private static void CheckHandUpdated()
        {
            int frameCount = Time.frameCount;
            if (frameCount > m_LeftHand.updateTime ||
                frameCount > m_RightHand.updateTime)
            {
#if UNITY_XR_HANDS
                if (m_IsSupportViveHand || m_IsSupportXrHand)
                {
                    if (m_HandSubsystem == null || !m_HandSubsystem.running)
                    {
                        if (m_HandSubsystem != null)
                        {
                            m_HandSubsystem.updatedHands -= OnUpdatedHands;
                            m_HandSubsystem = null;
                        }

                        m_HandSubsystems.Clear();
                        SubsystemManager.GetSubsystems(m_HandSubsystems);
                        for (var i = 0; i < m_HandSubsystems.Count; ++i)
                        {
                            var xrHand = m_HandSubsystems[i];
                            if (xrHand.running)
                            {
                                m_HandSubsystem = xrHand;
                                m_HandSubsystem.updatedHands += OnUpdatedHands;
                                break;
                            }
                        }
                    }
                }
#else
                if (m_IsSupportViveHand)
                {
                    UpdateViveHand(true);
                    UpdateViveHand(false);
                }
#endif
            }
        }

        private static void UpdateViveHand(bool isLeft)
        {
            bool isUpdated = XR_EXT_hand_tracking.Interop.GetJointLocations(isLeft, out XrHandJointLocationEXT[] viveJoints);
            for (int i = 0; i < m_JointBuffer.Length; i++)
            {
                bool isValid = isUpdated &&
                               viveJoints[i].locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_TRACKED_BIT) &&
                               viveJoints[i].locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT);
                Vector3 position = viveJoints[i].pose.position.ToUnityVector();
                Quaternion rotation = viveJoints[i].pose.orientation.ToUnityQuaternion();
                m_JointBuffer[i] = new JointData(isValid, position, rotation);
            }
            if (isLeft)
            {
                m_LeftHand.Update(m_JointBuffer);
            }
            else
            {
                m_RightHand.Update(m_JointBuffer);
            }
        }

#if UNITY_XR_HANDS
        private static void OnUpdatedHands(XRHandSubsystem xrHnad, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType type)
        {
            if (xrHnad != null && xrHnad.running)
            {
                UpdateXRHand(true, xrHnad, flags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints));
                UpdateXRHand(false, xrHnad, flags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints));
            }
        }

        private static void UpdateXRHand(bool isLeft, XRHandSubsystem xrHand, bool isUpdated)
        {
            for (int i = 0; i < m_JointBuffer.Length; i++)
            {
                XRHandJointID jointId = JointTypeToXRId(i);
                XRHandJoint joint = (isLeft ? xrHand.leftHand : xrHand.rightHand).GetJoint(jointId);

                if (isUpdated && joint.trackingState.HasFlag(XRHandJointTrackingState.Pose))
                {
                    joint.TryGetPose(out Pose pose);
                    m_JointBuffer[i] = new JointData(true, pose.position, pose.rotation);
                }
                else
                {
                    m_JointBuffer[i] = new JointData(false, Vector3.zero, Quaternion.identity);
                }
            }
            if (isLeft)
            {
                m_LeftHand.Update(m_JointBuffer);
            }
            else
            {
                m_RightHand.Update(m_JointBuffer);
            }
        }

        private static XRHandJointID JointTypeToXRId(int id)
        {
            switch (id)
            {
                case 0:
                    return XRHandJointID.Palm;
                case 1:
                    return XRHandJointID.Wrist;
                default:
                    return (XRHandJointID)(id + 1);
            }
        }
#endif

        private static DeviceCategory GetController(Handedness handedness)
        {
            DeviceCategory device = DeviceCategory.None;
            switch (handedness)
            {
                case Handedness.Left:
                    device = DeviceCategory.LeftController;
                    break;
                case Handedness.Right:
                    device = DeviceCategory.RightController;
                    break;
            }
            return device;
        }

        private static DeviceCategory GetHand(Handedness handedness)
        {
            DeviceCategory device = DeviceCategory.None;
            switch (handedness)
            {
                case Handedness.Left:
                    device = DeviceCategory.LeftHand;
                    break;
                case Handedness.Right:
                    device = DeviceCategory.RightHand;
                    break;
            }
            return device;
        }

        private static Handedness GetHandedness(DeviceCategory device)
        {
            Handedness handedness = Handedness.None;
            switch (device)
            {
                case DeviceCategory.LeftHand:
                    handedness = Handedness.Left;
                    break;
                case DeviceCategory.RightHand:
                    handedness = Handedness.Right;
                    break;
            }
            return handedness;
        }
    }
}
