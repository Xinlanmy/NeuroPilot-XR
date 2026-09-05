// Copyright HTC Corporation All Rights Reserved.

using UnityEngine.Scripting;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.OpenXR;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Input;
using System;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#else
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif

namespace VIVE.OpenXR.Hand
{
    /// <summary>
    /// This <see cref="OpenXRInteractionFeature"/> enables the use of hand interaction profiles in OpenXR. It enables <see cref="ViveHandInteractionExt.kOpenxrExtensionString">XR_EXT_hand_interaction</see> in the underyling runtime.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "VIVE XR Hand Interaction Ext (Deprecated)",
        Hidden = true,
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "HTC",
        Desc = "Support for enabling the KHR hand interaction profile. Will register the controller map for hand interaction if enabled.",
        DocumentationLink = "https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_hand_interaction",
        Version = "1.0.0",
        OpenxrExtensionStrings = kOpenxrExtensionString,
        Category = FeatureCategory.Interaction,
        FeatureId = featureId)]
#endif
    [Obsolete("This class is deprecated. Please use Unity Hand Interaction Profile instead.")]
    public class ViveHandInteractionExt : OpenXRInteractionFeature
    {
        #region Log
        const string LOG_TAG = "VIVE.OpenXR.Hand.ViveHandInteractionExt";
        StringBuilder m_sb = null;
        StringBuilder sb {
            get {
                if (m_sb == null) { m_sb = new StringBuilder(); }
                return m_sb;
            }
        }
        void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
        void WARNING(StringBuilder msg) { Debug.LogWarningFormat("{0} {1}", LOG_TAG, msg); }
        #endregion

		/// <summary>
		/// OpenXR specification <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_HTC_hand_interaction">12.69. XR_HTC_hand_interaction</see>.
		/// </summary>
		public const string kOpenxrExtensionString = "XR_EXT_hand_interaction";

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "vive.openxr.feature.hand.interaction.ext";

        [Preserve, InputControlLayout(displayName = "VIVE Hand Interaction Ext (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
        public class HandInteractionExtDevice : XRController
        {
            #region Log
            const string LOG_TAG = "VIVE.OpenXR.Hand.ViveHandInteractionExt.HandInteractionExtDevice";
            void DEBUG(string msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
            #endregion

            #region Action Path
            /// <summary>
            /// A <see cref="PoseControl"/> representing the <see cref="ViveHandInteractionExt.grip"/> OpenXR binding.
            /// </summary>
			[Preserve, InputControl(offset = 0, aliases = new[] { "device", "gripPose" }, usage = "Device")]
            public PoseControl devicePose { get; private set; }

            /// <summary>
            /// A <see cref="PoseControl"/> representing the <see cref="ViveHandInteractionExt.aim"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(offset = 0, alias = "aimPose", usage = "Pointer")]
            public PoseControl pointer { get; private set; }

            /// <summary>
            /// A <see cref="PoseControl"/> representing the <see cref="ViveHandInteractionExt.pinchPose"/> OpenXR binding.
            /// </summary>
			[Preserve, InputControl(offset = 0, usage = "Pinch")]
            public PoseControl pinchPose { get; private set; }

            /// <summary>
            /// A <see cref="PoseControl"/> representing the <see cref="ViveHandInteractionExt.poke"/> OpenXR binding.
            /// </summary>
			[Preserve, InputControl(offset = 0, alias = "indexTip", usage = "Poke")]
            public PoseControl pokePose { get; private set; }

            /// <summary>
            /// A <see cref="AxisControl"/> representing information from the <see cref="ViveHandInteractionExt.graspValue"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "gripValue" }, usage = "GraspValue")]
            public AxisControl graspValue { get; private set; }
            /// <summary>
            /// A <see cref="ButtonControl"/> representing the <see cref="ViveHandInteractionExt.graspReady"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "isGrasped", "isGripped" }, usage = "GraspReady")]
            public ButtonControl graspReady { get; private set; }

            /// <summary>
            /// A <see cref="AxisControl"/> representing information from the <see cref="ViveHandInteractionExt.pointerActivateValue"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "pointerValue" }, usage = "PointerActivateValue")]
            public AxisControl pointerActivateValue { get; private set; }
            /// <summary>
            /// A <see cref="ButtonControl"/> representing the <see cref="ViveHandInteractionExt.pointerActivateReady"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "isPointed", "pointerReady" }, usage = "PointerActivateReady")]
            public ButtonControl pointerActivateReady { get; private set; }

            /// <summary>
            /// A <see cref="AxisControl"/> representing information from the <see cref="ViveHandInteractionExt.pinchValue"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(usage = "PinchValue")]
            public AxisControl pinchValue { get; private set; }
            /// <summary>
            /// A <see cref="ButtonControl"/> representing the <see cref="ViveHandInteractionExt.pinchReady"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "isPinched" }, usage = "PinchReady")]
            public ButtonControl pinchReady { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) required for backwards compatibility with the XRSDK layouts. This represents the overall tracking state of the device. This value is equivalent to mapping devicePose/isTracked.
            /// </summary>
            [Preserve, InputControl(offset = 2)]
            new public ButtonControl isTracked { get; private set; }
            /// <summary>
            /// A [IntegerControl](xref:UnityEngine.InputSystem.Controls.IntegerControl) required for backwards compatibility with the XRSDK layouts. This represents the bit flag set to indicate what data is valid. This value is equivalent to mapping devicePose/trackingState.
            /// </summary>
            [Preserve, InputControl(offset = 4)]
            new public IntegerControl trackingState { get; private set; }
            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the device position. This value is equivalent to mapping devicePose/position.
            /// </summary>
            [Preserve, InputControl(offset = 8, noisy = true, alias = "gripPosition")]
            new public Vector3Control devicePosition { get; private set; }
            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the device orientation. This value is equivalent to mapping devicePose/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 20, noisy = true, alias = "gripRotation")]
            new public QuaternionControl deviceRotation { get; private set; }

            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the aim position. This value is equivalent to mapping pointer/position.
            /// </summary>
            [Preserve, InputControl(offset = 72, noisy = true)]
            public Vector3Control pointerPosition { get; private set; }
            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the aim orientation. This value is equivalent to mapping pointer/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 84, noisy = true)]
            public QuaternionControl pointerRotation { get; private set; }

            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the pinch position. This value is equivalent to mapping pinchPose/position.
            /// </summary>
            [Preserve, InputControl(offset = 136, noisy = true)]
            public Vector3Control pinchPosition { get; private set; }
            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the pinch orientation. This value is equivalent to mapping pinchPose/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 148, noisy = true)]
            public QuaternionControl pinchRotation { get; private set; }

            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the poke position. This value is equivalent to mapping pokePose/position.
            /// </summary>
            [Preserve, InputControl(offset = 200, noisy = true)]
            public Vector3Control pokePosition { get; private set; }
            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the poke orientation. This value is equivalent to mapping pokePose/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 212, noisy = true)]
            public QuaternionControl pokeRotation { get; private set; }
            #endregion

            /// <summary>
            /// Internal call used to assign controls to the the correct element.
            /// </summary>
            protected override void FinishSetup()
            {
                DEBUG("FinishSetup() interfaceName: " + description.interfaceName
                    + ", deviceClass: " + description.deviceClass
                    + ", product: " + description.product
                    + ", serial: " + description.serial
                    + ", version: " + description.version);

                base.FinishSetup();

                pointer = GetChildControl<PoseControl>("pointer");
                pointerActivateValue = GetChildControl<AxisControl>("pointerActivateValue");
                pointerActivateReady = GetChildControl<ButtonControl>("pointerActivateReady");

                devicePose = GetChildControl<PoseControl>("devicePose");
                graspValue = GetChildControl<AxisControl>("graspValue");
                graspReady = GetChildControl<ButtonControl>("graspReady");

                pinchPose = GetChildControl<PoseControl>("pinchPose");
                pinchValue = GetChildControl<AxisControl>("pinchValue");
                pinchReady = GetChildControl<ButtonControl>("pinchReady");

                pokePose = GetChildControl<PoseControl>("pokePose");
            }
        }

        /// <summary>
        /// The interaction profile string used to reference the hand interaction input device.
        /// </summary>
        public const string profile = "/interaction_profiles/ext/hand_interaction_ext";

        #region Supported component paths
        /// <summary>
        /// Constant for a pose interaction binding '.../input/aim/pose' OpenXR Input Binding.<br></br>
        /// Typically used for aiming at objects out of arm¡¦s reach. When using a hand interaction profile, it is typically paired with <see cref="pointerActivateValue"/> to optimize aiming ray stability while performing the gesture.<br></br>
        /// When using a controller interaction profile, the "aim" pose is typically paired with a trigger or a button for aim and fire operations.
        /// </summary>
        public const string aim = "/input/aim/pose";

        /// <summary>
        /// Constant for a float interaction binding '.../input/aim_activate_ext/value' OpenXR Input Binding.<br></br>
        /// A 1D analog input component indicating that the user activated the action on the target that the user is pointing at with the aim pose.
        /// </summary>
        public const string pointerActivateValue = "/input/aim_activate_ext/value";

        /// <summary>
        /// Constant for a boolean interaction binding '.../input/aim_activate_ext/ready_ext' OpenXR Input Binding.<br></br>
        /// A boolean input, where the value XR_TRUE indicates that the fingers to perform the "aim_activate" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing an "aim_activate" gesture.
        /// </summary>
        public const string pointerActivateReady = "/input/aim_activate_ext/ready_ext";

        /// <summary>
        /// Constant for a pose interaction binding '.../input/grip/pose' OpenXR Input Binding.<br></br>
        /// Typically used for holding a large object in the user¡¦s hand. When using a hand interaction profile, it is typically paired with <see cref="graspValue"/> for the user to directly manipulate an object held in a hand.<br></br>
        /// When using a controller interaction profile, the "grip" pose is typically paired with a "squeeze" button or trigger that gives the user the sense of tightly holding an object.
        /// </summary>
        public const string grip = "/input/grip/pose";

        /// <summary>
        /// Constant for a float interaction binding '.../input/grasp_ext/value' OpenXR Input Binding.<br></br>
        /// A 1D analog input component indicating that the user is making a fist.
        /// </summary>
        public const string graspValue = "/input/grasp_ext/value";

        /// <summary>
        /// Constant for a boolean interaction binding '.../input/grasp_ext/ready_ext' OpenXR Input Binding.<br></br>
        /// A boolean input, where the value XR_TRUE indicates that the hand performing the grasp action is properly tracked by the hand tracking device and it is observed to be ready to perform or is performing the grasp action.
        /// </summary>
        public const string graspReady = "/input/grasp_ext/ready_ext";

        /// <summary>
        /// Constant for a pose interaction binding '.../input/pinch_ext/pose' OpenXR Input Binding.<br></br>
        /// Typically used for directly manipulating a small object using the pinch gesture. When using a hand interaction profile, it is typically paired with the <see cref="pinchValue"/>.<br></br>
        /// When using a controller interaction profile, it is typically paired with a trigger manipulated with the index finger, which typically requires curling the index finger and applying pressure with the fingertip.
        /// </summary>
        public const string pinchPose = "/input/pinch_ext/pose";

        /// <summary>
        /// Constant for a float interaction binding '.../input/pinch_ext/value' OpenXR Input Binding.<br></br>
        /// A 1D analog input component indicating the extent which the user is bringing their finger and thumb together to perform a "pinch" gesture.
        /// </summary>
        public const string pinchValue = "/input/pinch_ext/value";

        /// <summary>
        /// Constant for a boolean interaction binding '.../input/pinch_ext/ready_ext' OpenXR Input Binding.<br></br>
        /// A boolean input, where the value XR_TRUE indicates that the fingers used to perform the "pinch" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing a "pinch" gesture.
        /// </summary>
        public const string pinchReady = "/input/pinch_ext/ready_ext";

        /// <summary>
        /// Constant for a pose interaction binding '.../input/poke_ext/pose' OpenXR Input Binding.<br></br>
        /// Typically used for contact-based interactions using the motion of the hand or fingertip. It typically does not pair with other hand gestures or buttons on the controller. The application typically uses a sphere collider with the "poke" pose to visualize the pose and detect touch with a virtual object.
        /// </summary>
        public const string poke = "/input/poke_ext/pose";
        #endregion

#pragma warning disable
        private bool m_XrInstanceCreated = false;
#pragma warning restore
        private XrInstance m_XrInstance = 0;
        /// <summary>
        /// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateInstance">xrCreateInstance</see> is done.
        /// </summary>
        /// <param name="xrInstance">The created instance.</param>
        /// <returns>True for valid <see cref="XrInstance">XrInstance</see></returns>
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
            {
                sb.Clear().Append("OnInstanceCreate() ").Append(kOpenxrExtensionString).Append(" is NOT enabled."); WARNING(sb);
                return false;
            }

            m_XrInstanceCreated = true;
            m_XrInstance = xrInstance;
            sb.Clear().Append("OnInstanceCreate() " + m_XrInstance); DEBUG(sb);

            return base.OnInstanceCreate(xrInstance);
        }

        private const string kLayoutName = "ViveHandInteractionExt";
        private const string kDeviceLocalizedName = "Vive Hand Interaction Ext OpenXR";
        /// <summary>
        /// Registers the <see cref="HandInteractionExtDevice"/> layout with the Input System.
        /// </summary>
        protected override void RegisterDeviceLayout()
        {
            sb.Clear().Append("RegisterDeviceLayout() ").Append(kLayoutName).Append(", product: ").Append(kDeviceLocalizedName); DEBUG(sb);
            InputSystem.RegisterLayout(typeof(HandInteractionExtDevice),
                kLayoutName,
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceLocalizedName));
        }

        /// <summary>
        /// Removes the <see cref="HandInteractionExtDevice"/> layout from the Input System.
        /// </summary>
        protected override void UnregisterDeviceLayout()
        {
            sb.Clear().Append("UnregisterDeviceLayout() ").Append(kLayoutName); DEBUG(sb);
            InputSystem.RemoveLayout(kLayoutName);
        }

#if UNITY_XR_OPENXR_1_9_1
        /// <summary>
        /// Return interaction profile type. HandInteractionExtDevice profile is Device type.
        /// </summary>
        /// <returns>Interaction profile type.</returns>
        protected override InteractionProfileType GetInteractionProfileType()
        {
            return typeof(HandInteractionExtDevice).IsSubclassOf(typeof(XRController)) ? InteractionProfileType.XRController : InteractionProfileType.Device;
        }

        /// <summary>
        /// Return device layer out string used for registering device HandInteractionExtDevice in InputSystem.
        /// </summary>
        /// <returns>Device layout string.</returns>
        protected override string GetDeviceLayoutName()
        {
            return kLayoutName;
        }
#endif

        /// <summary>
        /// Registers action maps to Unity XR.
        /// </summary>
        protected override void RegisterActionMapsWithRuntime()
        {
            sb.Clear().Append("RegisterActionMapsWithRuntime() Action map vivehandinteractionext")
                .Append(", localizedName: ").Append(kDeviceLocalizedName)
                .Append(", desiredInteractionProfile").Append(profile);
            DEBUG(sb);

            ActionMapConfig actionMap = new ActionMapConfig()
            {
                name = "vivehandinteractionext",
                localizedName = kDeviceLocalizedName,
                desiredInteractionProfile = profile,
                manufacturer = "HTC",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>()
                {
                    new DeviceConfig()
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left),
                        userPath = UserPaths.leftHand
                    },
                    new DeviceConfig()
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right),
                        userPath = UserPaths.rightHand
                    }
                },
                actions = new List<ActionConfig>()
                {
                    // Grip Pose
                    new ActionConfig()
                    {
                        name = "devicePose",
                        localizedName = "Grasp Pose",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Device"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = grip,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Grip Value
                    new ActionConfig()
                    {
                        name = "graspValue",
                        localizedName = "Grip Axis",
                        type = ActionType.Axis1D,
                        usages = new List<string>()
                        {
                            "GraspValue"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = graspValue,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Grip Ready
					new ActionConfig()
                    {
                        name = "graspReady",
                        localizedName = "Is Grasped",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "GraspReady"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = graspReady,
                                interactionProfileName = profile,
                            },
                        }
                    },
					// Aim Pose
					new ActionConfig()
                    {
                        name = "pointer",
                        localizedName = "Aim Pose",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Pointer"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = aim,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Aim Value
                    new ActionConfig()
                    {
                        name = "pointerActivateValue",
                        localizedName = "Pointer Axis",
                        type = ActionType.Axis1D,
                        usages = new List<string>()
                        {
                            "PointerActivateValue"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = pointerActivateValue,
                                interactionProfileName = profile,
                            }
                        }
                    },
					// Aim Ready
					new ActionConfig()
                    {
                        name = "pointerActivateReady",
                        localizedName = "Is Pointed",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "PointerActivateReady"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = pointerActivateReady,
                                interactionProfileName = profile,
                            },
                        }
                    },
                    // Pinch Pose
                    new ActionConfig()
                    {
                        name = "pinchPose",
                        localizedName = "Pinch Pose",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Pinch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = pinchPose,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Pinch Value
                    new ActionConfig()
                    {
                        name = "pinchValue",
                        localizedName = "Pinch Axis",
                        type = ActionType.Axis1D,
                        usages = new List<string>()
                        {
                            "PinchValue"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = pinchValue,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Pinch Ready
					new ActionConfig()
                    {
                        name = "pinchReady",
                        localizedName = "Is Pinched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "PinchReady"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = pinchReady,
                                interactionProfileName = profile,
                            },
                        }
                    },
					// Poke Pose
					new ActionConfig()
                    {
                        name = "pokePose",
                        localizedName = "Index Tip",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Poke"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = poke,
                                interactionProfileName = profile,
                            }
                        }
                    },
                }
            };

            AddActionMap(actionMap);
        }
    }
}