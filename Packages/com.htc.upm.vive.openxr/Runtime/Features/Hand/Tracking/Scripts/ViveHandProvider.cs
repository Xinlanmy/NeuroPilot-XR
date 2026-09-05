using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Interaction;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
namespace VIVE.OpenXR.Hand
{
    public class ViveHandProvider : XRHandSubsystemProvider
    {
        #region Hand Interaction
        private const string kFeatureAimPos = "PointerPosition";
        private const string kFeatureAimRot = "PointerRotation";
        private const string kFeatureAimValue = "PointerActivateValue";
        private const string kFeatureGripPos = "DevicePosition";
        private const string kFeatureGripRot = "DeviceRotation";
        private const string kFeatureGripValue = "GraspValue";
        private const string kFeaturePinchPos = "PinchPosition";
        private const string kFeaturePinchRot = "PinchRotation";
        private const string kFeaturePinchValue = "PinchValue";
        private const string kFeaturePokePos = "PokePosition";
        private const string kFeaturePokeRot = "PokeRotation";

        private class HandDevice
        {
            public Pose aimPose => m_AimPose;
            public Pose gripPose => m_GripPose;
            public Pose pinchPose => m_PinchPose;
            public Pose pokePose => m_PokePose;
            public float aimActivateValue => m_AimActivateValue;
            public float graspValue => m_GraspValue;
            public float pinchValue => m_PinchValue;

            private Pose m_AimPose = Pose.identity;
            private Pose m_GripPose = Pose.identity;
            private Pose m_PinchPose = Pose.identity;
            private Pose m_PokePose = Pose.identity;
            private float m_AimActivateValue = 0;
            private float m_GraspValue = 0;
            private float m_PinchValue = 0;

            private InputDevice device = default(InputDevice);
            private Dictionary<string, InputFeatureUsage<Vector3>> posUsageMapping = new Dictionary<string, InputFeatureUsage<Vector3>>();
            private Dictionary<string, InputFeatureUsage<Quaternion>> rotUsageMapping = new Dictionary<string, InputFeatureUsage<Quaternion>>();
            private Dictionary<string, InputFeatureUsage<float>> valueUsageMapping = new Dictionary<string, InputFeatureUsage<float>>();

            public HandDevice(InputDevice device)
            {
                this.device = device;

                List<InputFeatureUsage> inputFeatures = new List<InputFeatureUsage>();
                device.TryGetFeatureUsages(inputFeatures);
                for (int i = 0; i < inputFeatures.Count; i++)
                {
                    InputFeatureUsage feature = inputFeatures[i];
                    switch (feature.name)
                    {
                        case kFeatureAimPos:
                        case kFeatureGripPos:
                        case kFeaturePinchPos:
                        case kFeaturePokePos:
                            posUsageMapping.Add(feature.name, feature.As<Vector3>());
                            break;
                        case kFeatureAimRot:
                        case kFeatureGripRot:
                        case kFeaturePinchRot:
                        case kFeaturePokeRot:
                            rotUsageMapping.Add(feature.name, feature.As<Quaternion>());
                            break;
                        case kFeatureAimValue:
                        case kFeatureGripValue:
                        case kFeaturePinchValue:
                            valueUsageMapping.Add(feature.name, feature.As<float>());
                            break;

                        default:
                            break;
                    }
                }
            }

            public void UpdateInputValue()
            {
                UpdatePosition();
                UpdateRotation();
                UpdateValue();
            }

            private void UpdatePosition()
            {
                var enumerator = posUsageMapping.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var feature = enumerator.Current;
                    string featureName = feature.Key;
                    InputFeatureUsage<Vector3> featureUsage = feature.Value;
                    if (device.TryGetFeatureValue(featureUsage, out Vector3 position))
                    {
                        switch (featureName)
                        {
                            case kFeatureAimPos:
                                m_AimPose.position = position;
                                break;
                            case kFeatureGripPos:
                                m_GripPose.position = position;
                                break;
                            case kFeaturePinchPos:
                                m_PinchPose.position = position;
                                break;
                            case kFeaturePokePos:
                                m_PokePose.position = position;
                                break;
                        }
                    }
                }
            }

            private void UpdateRotation() 
            {
                var enumerator = rotUsageMapping.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var feature = enumerator.Current;
                    string featureName = feature.Key;
                    InputFeatureUsage<Quaternion> featureUsage = feature.Value;
                    if (device.TryGetFeatureValue(featureUsage, out Quaternion rotation))
                    {
                        switch (featureName)
                        {
                            case kFeatureAimRot:
                                m_AimPose.rotation = rotation;
                                break;
                            case kFeatureGripRot:
                                m_GripPose.rotation = rotation;
                                break;
                            case kFeaturePinchRot:
                                m_PinchPose.rotation = rotation;
                                break;
                            case kFeaturePokeRot:
                                m_PokePose.rotation = rotation;
                                break;
                        }
                    }
                }
            }

            private void UpdateValue()
            {
                var enumerator = valueUsageMapping.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var feature = enumerator.Current;
                    string featureName = feature.Key;
                    InputFeatureUsage<float> featureUsage = feature.Value;
                    if (device.TryGetFeatureValue(featureUsage, out float value))
                    {
                        switch (featureName)
                        {
                            case kFeatureAimValue:
                                m_AimActivateValue = value;
                                break;
                            case kFeatureGripValue:
                                m_GraspValue = value;
                                break;
                            case kFeaturePinchValue:
                                m_PinchValue = value;
                                break;
                        }
                    }
                }
            }
        }
        private static HandDevice leftHandDevice = null;
        private static HandDevice rightHandDevice = null;
        private const string kInteractionDeviceName = "Vive Hand Interaction Ext OpenXR";
        #endregion

        private ViveHandTracking viveHand;

        public override void Destroy() { }

        public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
        {
            handJointsInLayout[XRHandJointID.Palm.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.Wrist.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.ThumbMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.IndexMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.MiddleMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.RingMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.LittleMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleTip.ToIndex()] = true;
        }

        public override void Start()
        {
            Initialize();
#if UNITY_XR_HANDS_1_5_0
            InitHandInteractionDevices();
            InputDevices.deviceConnected += DeviceConnected;
            InputDevices.deviceDisconnected += DeviceDisconnected;
#endif
        }

        public override void Stop()
        {
#if UNITY_XR_HANDS_1_5_0
            InputDevices.deviceConnected -= DeviceConnected;
            InputDevices.deviceDisconnected -= DeviceDisconnected;
#endif
        }

        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType updateType, ref Pose leftHandRootPose, NativeArray<XRHandJoint> leftHandJoints, ref Pose rightHandRootPose, NativeArray<XRHandJoint> rightHandJoints)
        {
            XRHandSubsystem.UpdateSuccessFlags flags = XRHandSubsystem.UpdateSuccessFlags.None;
            if (UpdateHand(true, ref leftHandRootPose, ref leftHandJoints))
            {
                flags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose | XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
            }
            if (UpdateHand(false, ref rightHandRootPose, ref rightHandJoints))
            {
                flags |= XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose | XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            }
#if UNITY_XR_HANDS_1_5_0
            if (updateType == XRHandSubsystem.UpdateType.Dynamic && canSurfaceCommonPoseData)
            {
                UpdateHandInteraction();
            }
#endif
            return flags;
        }

#if UNITY_XR_HANDS_1_5_0
        public override bool canSurfaceCommonPoseData => HandInteractionSupport();

        public override bool TryGetAimPose(Handedness handedness, out Pose aimPose)
        {
            aimPose = Pose.identity;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                aimPose = handDevice.aimPose;
                return true;
            }
            return false;
        }

        public override bool TryGetAimActivateValue(Handedness handedness, out float aimActivateValue)
        {
            aimActivateValue = 0;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                aimActivateValue = handDevice.aimActivateValue;
                return true;
            }
            return false;
        }

        public override bool TryGetGripPose(Handedness handedness, out Pose gripPose)
        {
            gripPose = Pose.identity;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                gripPose = handDevice.gripPose;
                return true;
            }

            return false;
        }

        public override bool TryGetGraspValue(Handedness handedness, out float graspValue)
        {
            graspValue = 0;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                graspValue = handDevice.graspValue;
                return true;
            }
            return false;
        }

        public override bool TryGetPinchPose(Handedness handedness, out Pose pinchPose)
        {
            pinchPose = Pose.identity;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                pinchPose = handDevice.pinchPose;
                return true;
            }
            return false;
        }

        public override bool TryGetPinchValue(Handedness handedness, out float pinchValue)
        {
            pinchValue = 0;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                pinchValue = handDevice.pinchValue;
                return true;
            }
            return false;
        }

        public override bool TryGetPokePose(Handedness handedness, out Pose pokePose)
        {
            pokePose = Pose.identity;
            HandDevice handDevice = GetHandDevice(handedness);
            if (handDevice != null)
            {
                pokePose = handDevice.pokePose;
                return true;
            }
            return false;
        }

        private void DeviceConnected(InputDevice inputDevice)
        {
            if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
                inputDevice.name == kInteractionDeviceName)
            {
                leftHandDevice = new HandDevice(inputDevice);
            }
            if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
                inputDevice.name == kInteractionDeviceName)
            {
                rightHandDevice = new HandDevice(inputDevice);
            }
        }

        private void DeviceDisconnected(InputDevice inputDevice)
        {
            if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
                inputDevice.name == kInteractionDeviceName)
            {
                leftHandDevice = default;
            }
            if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
                inputDevice.name == kInteractionDeviceName)
            {
                rightHandDevice = default;
            }
        }

        private void InitHandInteractionDevices()
        {
            List<InputDevice> inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand |
                                                       InputDeviceCharacteristics.HandTracking |
                                                       InputDeviceCharacteristics.TrackedDevice, inputDevices);
            for (int i = 0; i < inputDevices.Count; i++)
            {
                InputDevice inputDevice = inputDevices[i];
                DeviceConnected(inputDevice);
            }
        }

        private void UpdateHandInteraction()
        {
            if (leftHandDevice != null)
            {
                leftHandDevice.UpdateInputValue();
            }
            if (rightHandDevice != null)
            {
                rightHandDevice.UpdateInputValue();
            }
        }

        private HandDevice GetHandDevice(Handedness handedness) => handedness == Handedness.Left ? leftHandDevice : rightHandDevice;

        private bool HandInteractionSupport()
        {
            ViveInteractions viveInteractions = OpenXRSettings.Instance.GetFeature<ViveInteractions>();
            if (viveInteractions.enabled)
            {
                return viveInteractions.UseKhrHandInteraction();
            }
            return false;
        }
#endif

        private void Initialize()
        {
            viveHand = OpenXRSettings.Instance.GetFeature<ViveHandTracking>();
        }

        private bool UpdateHand(bool isLeft, ref Pose handRootPose, ref NativeArray<XRHandJoint> handJoints)
        {
            if (!viveHand) { return false; }
            bool isValid = viveHand.GetJointLocations(isLeft, out XrHandJointLocationEXT[] viveJoints);

            Handedness handedness = isLeft ? Handedness.Left : Handedness.Right;
            XRHandJointTrackingState trackingState = XRHandJointTrackingState.None;
            for (int jointIndex = XRHandJointID.BeginMarker.ToIndex(); jointIndex < XRHandJointID.EndMarker.ToIndex(); ++jointIndex)
            {
                XRHandJointID jointID = XRHandJointIDUtility.FromIndex(jointIndex);
                int viveIndex = XRHandJointIDToIndex(jointID);

                Pose pose = Pose.identity;
                if (isValid)
                {
                    pose.position = viveJoints[viveIndex].pose.position.ToUnityVector();
                    pose.rotation = viveJoints[viveIndex].pose.orientation.ToUnityQuaternion();
                    trackingState = XRHandJointTrackingState.Pose;
                }
                handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness, trackingState, jointID, pose);
            }
            handJoints[XRHandJointID.Wrist.ToIndex()].TryGetPose(out handRootPose);
            return isValid;
        }

        private int XRHandJointIDToIndex(XRHandJointID id)
        {
            switch (id)
            {
                case XRHandJointID.Palm:
                    return 0;
                case XRHandJointID.Wrist:
                    return 1;
                default:
                    return (int)id - 1;
            }
        }
    }
}
#endif

