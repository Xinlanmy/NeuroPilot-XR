// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#else
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif

using VIVE.OpenXR.Hand;

namespace VIVE.OpenXR
{
	/// <summary>
	/// This <see cref="OpenXRInteractionFeature"/> enables the use of HTC VIVE Focus 3 interaction profiles in OpenXR.
	/// </summary>
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE Focus 3 Controller Interaction",
		BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
		Company = "HTC",
		Desc = "Allows for mapping input to the VIVE Focus 3 interaction profile.",
		DocumentationLink = "https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#XR_HTC_vive_focus3_controller_interaction",
		OpenxrExtensionStrings = kOpenxrExtensionString,
		Version = "1.0.0",
		Category = FeatureCategory.Interaction,
		FeatureId = featureId)]
#endif
	public class VIVEFocus3Profile : OpenXRInteractionFeature
	{
		#region Log
		const string LOG_TAG = "VIVE.OpenXR.VIVEFocus3Profile ";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.Log(LOG_TAG + msg); }
		void ERROR(StringBuilder msg) { Debug.LogError(LOG_TAG + msg); }
		#endregion

		private static VIVEFocus3Profile m_Instance = null;

		public const string kOpenxrExtensionString = "XR_HTC_vive_focus3_controller_interaction";

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.focus3controller";

		private static bool HandInteractionExtEnabled { get { return OpenXRRuntime.IsExtensionEnabled(ViveHandInteractionExt.kOpenxrExtensionString); } }

		/// <summary>
		/// An Input System device based on the hand interaction profile in the <a href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#XR_HTC_vive_focus3_controller_interaction">Interaction Profile</a>.
		/// </summary>
		[Preserve, InputControlLayout(displayName = "VIVE Focus 3 Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
		public class VIVEFocus3Controller : XRControllerWithRumble, IInputUpdateCallbackReceiver
		{
			#region Log
			const string LOG_TAG = "VIVE.OpenXR.VIVEFocus3Profile.VIVEFocus3Controller";
			StringBuilder m_sb = null;
			StringBuilder sb
			{
				get
				{
					if (m_sb == null) { m_sb = new StringBuilder(); }
					return m_sb;
				}
			}
			void DEBUG(StringBuilder msg) { Debug.Log(LOG_TAG + msg); }
			void ERROR(StringBuilder msg) { Debug.LogError(LOG_TAG + msg); }
			#endregion

			#region Action Path

			#region Button
			/// <summary>
			/// A [Vector2Control](xref:UnityEngine.InputSystem.Controls.Vector2Control) that represents the <see cref="VIVEFocus3Profile.thumbstick"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "Joystick", "primary2DAxis", "joystickAxis", "thumbstickAxis" }, usage = "Primary2DAxis")]
			public Vector2Control thumbstick { get; private set; }

			/// <summary>
			/// A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the <see cref="VIVEFocus3Profile.grip"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "GripAxis", "squeeze" }, usage = "Grip")]
			public AxisControl grip { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="gripPress"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "gripButton", "squeezeClicked" }, usage = "GripButton")]
			public ButtonControl gripPressed { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="gripTouch"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "GripTouch", "squeezeTouched" }, usage = "GripTouch")]
			public ButtonControl gripTouched { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="VIVEFocus3Profile.menu"/> OpenXR bindings, depending on handedness.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "Primary", "menubutton" }, usage = "MenuButton")]
			public ButtonControl menu { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="buttonA"/> <see cref="buttonX"/> OpenXR bindings, depending on handedness.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "A", "X", "buttonA", "buttonX" }, usage = "PrimaryButton")]
			public ButtonControl primaryButton { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="buttonB"/> <see cref="buttonY"/> OpenXR bindings, depending on handedness.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "B", "Y", "buttonB", "buttonY" }, usage = "SecondaryButton")]
			public ButtonControl secondaryButton { get; private set; }

			/// <summary>
			/// A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the <see cref="VIVEFocus3Profile.trigger"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "triggerAxis" }, usage = "Trigger")]
			public AxisControl trigger { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="triggerClick"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "indexButton", "triggerButton" }, usage = "TriggerButton")]
			public ButtonControl triggerPressed { get; private set; } // InputControl(offset = 20)

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="triggerTouch"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "indexTouch", "indexNearTouched" }, usage = "TriggerTouch")]
			public ButtonControl triggerTouched { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="thumbstickClick"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "JoystickOrPadPressed", "thumbstickClick", "joystickClicked", "primary2DAxisClick" }, usage = "Primary2DAxisClick")]
			public ButtonControl thumbstickClicked { get; private set; } // InputControl(offset = 22)

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="thumbstickTouch"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch", "joystickTouched", "primary2DAxisTouch" }, usage = "Primary2DAxisTouch")]
			public ButtonControl thumbstickTouched { get; private set; }

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="thumbrest"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(aliases = new[] { "ParkingTouched", "parkingTouched" })]
			public ButtonControl thumbrestTouched { get; private set; } // InputControl(offset = 24)
			#endregion

			/// <summary>
			/// A <see cref="PoseControl"/> that represents the <see cref="gripPose"/> OpenXR binding. The grip pose represents the location of the user's palm or holding a motion controller.
			/// </summary>
			[Preserve, InputControl(offset = 0, aliases = new[] { "device", "gripPose" }, usage = "Device")]
			public PoseControl devicePose { get; private set; }
			/// <summary>
			/// A <see cref="PoseControl"/> that represents the <see cref="VIVEFocus3Profile.aim"/> OpenXR binding. The pointer pose represents the tip of the controller pointing forward.
			/// </summary>
			[Preserve, InputControl(offset = 0, aliases = new[] { "aimPose", "pointerPose" }, usage = "Pointer")]
			public PoseControl pointer { get; private set; }

#if UNITY_ANDROID
			/// <summary>
			/// A <see cref="PoseControl"/> representing the <see cref="VIVEFocus3Profile.pinchPose"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(offset = 0, usage = "Pinch")]
			public PoseControl pinchPose { get; private set; }
			/// <summary>
			/// A <see cref="PoseControl"/> representing the <see cref="VIVEFocus3Profile.pokePose"/> OpenXR binding.
			/// </summary>
			[Preserve, InputControl(offset = 0, alias = "indexTip", usage = "Poke")]
			public PoseControl pokePose { get; private set; }
#endif

			/// <summary>
			/// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) required for backwards compatibility with the XRSDK layouts. This represents the overall tracking state of the device. This value is equivalent to mapping devicePose/isTracked.
			/// </summary>
			[Preserve, InputControl(offset = 24)]
			new public ButtonControl isTracked { get; private set; }
			/// <summary>
			/// A [IntegerControl](xref:UnityEngine.InputSystem.Controls.IntegerControl) required for backwards compatibility with the XRSDK layouts. This represents the bit flag set to indicate what data is valid. This value is equivalent to mapping devicePose/trackingState.
			/// </summary>
			[Preserve, InputControl(offset = 28)]
			new public IntegerControl trackingState { get; private set; }
			/// <summary>
			/// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the device position. This value is equivalent to mapping devicePose/position.
			/// </summary>
			[Preserve, InputControl(offset = 32, noisy = true, alias = "gripPosition")]
			new public Vector3Control devicePosition { get; private set; }
			/// <summary>
			/// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the device orientation. This value is equivalent to mapping devicePose/rotation.
			/// </summary>
			[Preserve, InputControl(offset = 44, noisy = true, alias = "gripOrientation")]
			new public QuaternionControl deviceRotation { get; private set; }

			/// <summary>
			/// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for back compatibility with the XRSDK layouts. This is the pointer position. This value is equivalent to mapping pointer/position.
			/// </summary>
			[Preserve, InputControl(offset = 92, noisy = true)]
			public Vector3Control pointerPosition { get; private set; }
			/// <summary>
			/// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the pointer rotation. This value is equivalent to mapping pointer/rotation.
			/// </summary>
			[Preserve, InputControl(offset = 104, noisy = true, alias = "pointerOrientation")]
			public QuaternionControl pointerRotation { get; private set; }

#if UNITY_ANDROID
			/// <summary>
			/// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the pinch position. This value is equivalent to mapping pinchPose/position.
			/// </summary>
			[Preserve, InputControl(offset = 152, noisy = true)]
			public Vector3Control pinchPosition { get; private set; }
			/// <summary>
			/// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the pinch orientation. This value is equivalent to mapping pinchPose/rotation.
			/// </summary>
			[Preserve, InputControl(offset = 164, noisy = true)]
			public QuaternionControl pinchRotation { get; private set; }

			/// <summary>
			/// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the poke position. This value is equivalent to mapping pokePose/position.
			/// </summary>
			[Preserve, InputControl(offset = 212, noisy = true)]
			public Vector3Control pokePosition { get; private set; }
			/// <summary>
			/// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the poke orientation. This value is equivalent to mapping pokePose/rotation.
			/// </summary>
			[Preserve, InputControl(offset = 224, noisy = true)]
			public QuaternionControl pokeRotation { get; private set; }
#endif
			/// <summary>
			/// A <see cref="HapticControl"/> that represents the <see cref="VIVEFocus3Profile.haptic"/> binding.
			/// </summary>
			[Preserve, InputControl(usage = "Haptic")]
			public HapticControl haptic { get; private set; }
#endregion

			private bool UpdateInputDeviceInRuntime = false;

			/// <summary>
			/// Internal call used to assign controls to the the correct element.
			/// </summary>
			protected override void FinishSetup()
			{
				base.FinishSetup();

				thumbstick = GetChildControl<Vector2Control>("thumbstick");
				trigger = GetChildControl<AxisControl>("trigger");
				triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
				triggerTouched = GetChildControl<ButtonControl>("triggerTouched");
				grip = GetChildControl<AxisControl>("grip");
				gripPressed = GetChildControl<ButtonControl>("gripPressed");
				gripTouched = GetChildControl<ButtonControl>("gripTouched");
				menu = GetChildControl<ButtonControl>("menu");
				primaryButton = GetChildControl<ButtonControl>("primaryButton");
				secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
				thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
				thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
				thumbrestTouched = GetChildControl<ButtonControl>("thumbrestTouched");

				devicePose = GetChildControl<PoseControl>("devicePose");
				pointer = GetChildControl<PoseControl>("pointer");
#if UNITY_ANDROID
				if (HandInteractionExtEnabled)
				{
					pinchPose = GetChildControl<PoseControl>("pinchPose");
					pokePose = GetChildControl<PoseControl>("pokePose");
				}
#endif
				isTracked = GetChildControl<ButtonControl>("isTracked");
				trackingState = GetChildControl<IntegerControl>("trackingState");
				devicePosition = GetChildControl<Vector3Control>("devicePosition");
				deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
				pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
				pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");
				haptic = GetChildControl<HapticControl>("haptic");

				sb.Clear()
					.Append("FinishSetup() device interfaceName: ").Append(description.interfaceName)
					.Append(", deviceClass: ").Append(description.deviceClass)
					.Append(", product: ").Append(description.product)
					.Append(", serial: ").Append(description.serial)
					.Append(", capabilities: ").Append(description.capabilities);
				DEBUG(sb);
			}

			private bool bRoleUpdatedLeft = false, bRoleUpdatedRight = false;
			public void OnUpdate()
			{
				if (!UpdateInputDeviceInRuntime) { return; }
				if (m_Instance == null) { return; }

				string func = "OnUpdate() ";
				if (leftHand.isTracked.ReadValue() > 0 && !bRoleUpdatedLeft)
				{
					sb.Clear().Append(func)
						.Append("product: ").Append(description.product)
						.Append(" with user path: ").Append(UserPaths.leftHand).Append(" is_tracked."); DEBUG(sb);

					XrPath path = StringToPath(UserPaths.leftHand);

					if (m_Instance.GetInputSourceName(path, XrInputSourceLocalizedNameFlags.XR_INPUT_SOURCE_LOCALIZED_NAME_USER_PATH_BIT, out string role) != XrResult.XR_SUCCESS)
					{
						sb.Clear().Append(func)
							.Append("GetInputSourceName XR_INPUT_SOURCE_LOCALIZED_NAME_USER_PATH_BIT failed."); ERROR(sb);
					}
					else
					{
						sb.Clear().Append(func)
							.Append("product: ").Append(description.product)
							.Append(" with user path: ").Append(UserPaths.leftHand).Append(" has role: ").Append(role); DEBUG(sb);
					}

					if (m_Instance.GetInputSourceName(path, XrInputSourceLocalizedNameFlags.XR_INPUT_SOURCE_LOCALIZED_NAME_SERIAL_NUMBER_BIT_HTC, out string sn) != XrResult.XR_SUCCESS)
					{
						sb.Clear().Append(func)
							.Append("GetInputSourceName XR_INPUT_SOURCE_LOCALIZED_NAME_SERIAL_NUMBER_BIT_HTC failed."); ERROR(sb);
					}
					else
					{
						sb.Clear().Append(func)
							.Append("product: ").Append(description.product)
							.Append(" with user path: ").Append(UserPaths.leftHand).Append(" has serial number: ").Append(role); DEBUG(sb);
					}

					bRoleUpdatedLeft = true;
				}
				if (rightHand.isTracked.ReadValue() > 0 && !bRoleUpdatedRight)
				{
					sb.Clear().Append(func)
						.Append("product: ").Append(description.product)
						.Append(" with user path: ").Append(UserPaths.rightHand).Append(" is_tracked."); DEBUG(sb);

					XrPath path = StringToPath(UserPaths.rightHand);

					if (m_Instance.GetInputSourceName(path, XrInputSourceLocalizedNameFlags.XR_INPUT_SOURCE_LOCALIZED_NAME_USER_PATH_BIT, out string role) != XrResult.XR_SUCCESS)
					{
						sb.Clear().Append(func)
							.Append("GetInputSourceName XR_INPUT_SOURCE_LOCALIZED_NAME_USER_PATH_BIT failed."); ERROR(sb);
					}
					else
					{
						sb.Clear().Append(func)
							.Append("product: ").Append(description.product)
							.Append(" with user path: ").Append(UserPaths.rightHand).Append(" has role: ").Append(role); DEBUG(sb);
					}

					if (m_Instance.GetInputSourceName(path, XrInputSourceLocalizedNameFlags.XR_INPUT_SOURCE_LOCALIZED_NAME_SERIAL_NUMBER_BIT_HTC, out string sn) != XrResult.XR_SUCCESS)
					{
						sb.Clear().Append(func)
							.Append("GetInputSourceName XR_INPUT_SOURCE_LOCALIZED_NAME_SERIAL_NUMBER_BIT_HTC failed."); ERROR(sb);
					}
					else
					{
						sb.Clear().Append(func)
							.Append("product: ").Append(description.product)
							.Append(" with user path: ").Append(UserPaths.leftHand).Append(" has serial number: ").Append(role); DEBUG(sb);
					}

					bRoleUpdatedRight = true;
				}
			}
		}

		/// <summary>
		/// The interaction profile string used to reference the <a href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#XR_HTC_vive_focus3_controller_interaction">Interaction Profile</a>.
		/// </summary>
		public const string profile = "/interaction_profiles/htc/vive_focus3_controller";

#region Supported component paths
		// Available Bindings
		// Left Hand Only
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/x/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
		/// </summary>
		public const string buttonX = "/input/x/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/y/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
		/// </summary>
		public const string buttonY = "/input/y/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/menu/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
		/// </summary>
		public const string menu = "/input/menu/click";

		// Right Hand Only
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/a/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
		/// </summary>
		public const string buttonA = "/input/a/click";
		/// <summary>
		/// Constant for a boolean interaction binding '..."/input/b/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
		/// </summary>
		public const string buttonB = "/input/b/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
		/// </summary>
		public const string system = "/input/system/click";

		// Both Hands
		/// <summary>
		/// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string grip = "/input/squeeze/value";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/squeeze/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string gripPress = "/input/squeeze/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/squeeze/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string gripTouch = "/input/squeeze/touch";
		/// <summary>
		/// Constant for a float interaction binding '.../input/trigger/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string trigger = "/input/trigger/value";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/trigger/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string triggerClick = "/input/trigger/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/trigger/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string triggerTouch = "/input/trigger/touch";
		/// <summary>
		/// Constant for a Vector2 interaction binding '.../input/thumbstick' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string thumbstick = "/input/thumbstick";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/thumbstick/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string thumbstickClick = "/input/thumbstick/click";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/thumbstick/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string thumbstickTouch = "/input/thumbstick/touch";
		/// <summary>
		/// Constant for a boolean interaction binding '.../input/thumbrest/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string thumbrest = "/input/thumbrest/touch";

		/// <summary>
		/// Constant for a hand grip pose interaction binding '.../input/grip/pose' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string gripPose = "/input/grip/pose";

		/// <summary>
		/// Constant for a hand point pose interaction binding '.../input/aim/pose' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
		/// </summary>
		public const string aim = "/input/aim/pose";
#if UNITY_ANDROID
		/// <summary>
		/// Constant for a pose interaction binding '.../input/pinch_ext/pose' OpenXR Input Binding.<br></br>
		/// Typically used for directly manipulating a small object using the pinch gesture. When using a hand interaction profile, it is typically paired with the <see cref="pinchValue"/>.<br></br>
		/// When using a controller interaction profile, it is typically paired with a trigger manipulated with the index finger, which typically requires curling the index finger and applying pressure with the fingertip.
		/// </summary>
		public const string pinchPose = "/input/pinch_ext/pose";

		/// <summary>
		/// Constant for a pose interaction binding '.../input/poke_ext/pose' OpenXR Input Binding.<br></br>
		/// Typically used for contact-based interactions using the motion of the hand or fingertip. It typically does not pair with other hand gestures or buttons on the controller. The application typically uses a sphere collider with the "poke" pose to visualize the pose and detect touch with a virtual object.
		/// </summary>
		public const string pokePose = "/input/poke_ext/pose";
#endif
		/// <summary>
		/// Constant for a haptic interaction binding '.../output/haptic' OpenXR Input Binding. Used by input subsystem to bind actions to physical outputs.
		/// </summary>
		public const string haptic = "/output/haptic";
#endregion

		private const string kLayoutName = "VIVEFocus3Controller";
		private const string kDeviceLocalizedName = "VIVE Focus 3 Controller OpenXR";
		/// <summary>
		/// Registers the <see cref="VIVEFocus3Controller"/> layout with the Input System.
		/// </summary>
		protected override void RegisterDeviceLayout()
		{
			sb.Clear().Append("RegisterDeviceLayout() ").Append(kLayoutName).Append(", product: ").Append(kDeviceLocalizedName); DEBUG(sb);
			InputSystem.RegisterLayout(typeof(VIVEFocus3Controller),
				kLayoutName,
				matches: new InputDeviceMatcher()
					.WithInterface(XRUtilities.InterfaceMatchAnyVersion)
					.WithProduct(kDeviceLocalizedName));
		}

		/// <summary>
		/// Removes the <see cref="VIVEFocus3Controller"/> layout from the Input System.
		/// </summary>
		protected override void UnregisterDeviceLayout()
		{
			sb.Clear().Append("UnregisterDeviceLayout() ").Append(kLayoutName); DEBUG(sb);
			InputSystem.RemoveLayout(kLayoutName);
		}

#if UNITY_XR_OPENXR_1_9_1
		/// <summary>
		/// Return interaction profile type. VIVEFocus3Controller profile is Device type.
		/// </summary>
		/// <returns>Interaction profile type.</returns>
		protected override InteractionProfileType GetInteractionProfileType()
		{
			return typeof(VIVEFocus3Controller).IsSubclassOf(typeof(XRController)) ? InteractionProfileType.XRController : InteractionProfileType.Device;
		}

		/// <summary>
		/// Return device layer out string used for registering device VIVEFocus3Controller in InputSystem.
		/// </summary>
		/// <returns>Device layout string.</returns>
		protected override string GetDeviceLayoutName()
		{
			return kLayoutName;
		}
#endif

		private List<ActionConfig> RequestActionConfigs()
		{
			if (HandInteractionExtEnabled)
			{
				sb.Clear().Append("RequestActionConfigs() XR_EXT_hand_interaction is enabled."); DEBUG(sb);
				return new List<ActionConfig>()
				{
					// Thumbstick Axis
					new ActionConfig()
					{
						name = "thumbstick",
						localizedName = "Thumbstick Axis",
						type = ActionType.Axis2D,
						usages = new List<string>()
						{
							"Primary2DAxis"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstick,
								interactionProfileName = profile,
							}
						}
					},
					// Grip Axis
					new ActionConfig()
					{
						name = "grip",
						localizedName = "Grip Axis",
						type = ActionType.Axis1D,
						usages = new List<string>()
						{
							"Grip"
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
					// Grip Press
					new ActionConfig()
					{
						name = "gripPressed",
						localizedName = "Grip Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"GripButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = gripPress,
								interactionProfileName = profile,
							}
						}
					},
					// Grip Touch
					// Known issue: Registering gripTouched will cause Controller Interaction Profile not work.
					/*new ActionConfig()
					{
						name = "gripTouched",
						localizedName = "Grip Touched",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"GripTouch"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = gripTouch,
								interactionProfileName = profile,
							}
						}
					},*/
					// Menu
					new ActionConfig()
					{
						name = "menu",
						localizedName = "Menu",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"MenuButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = menu,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = system,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},
					// X / A Press
					new ActionConfig()
					{
						name = "primaryButton",
						localizedName = "Primary Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"PrimaryButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = buttonX,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = buttonA,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},


					// Y / B Press
					new ActionConfig()
					{
						name = "secondaryButton",
						localizedName = "Secondary Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"SecondaryButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = buttonY,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = buttonB,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},


					// Trigger Axis
					new ActionConfig()
					{
						name = "trigger",
						localizedName = "Trigger Axis",
						type = ActionType.Axis1D,
						usages = new List<string>()
						{
							"Trigger"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = trigger,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger Press
					new ActionConfig()
					{
						name = "triggerPressed",
						localizedName = "Trigger Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"TriggerButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = triggerClick,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger Touch
					new ActionConfig()
					{
						name = "triggerTouched",
						localizedName = "Trigger Touched",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"TriggerTouch"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = triggerTouch,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbstick Click
					new ActionConfig()
					{
						name = "thumbstickClicked",
						localizedName = "Thumbstick Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"Primary2DAxisClick"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstickClick,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbstick Touch
					new ActionConfig()
					{
						name = "thumbstickTouched",
						localizedName = "Thumbstick Touched",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"Primary2DAxisTouch"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstickTouch,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbrest Touch
					new ActionConfig()
					{
						name = "thumbrestTouched",
						localizedName = "Parking Touch",
						type = ActionType.Binary,
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbrest,
								interactionProfileName = profile,
							}
						}
					},
					// Device Pose
					new ActionConfig()
					{
						name = "devicePose",
						localizedName = "Grip Pose",
						type = ActionType.Pose,
						usages = new List<string>()
						{
							"Device"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = gripPose,
								interactionProfileName = profile,
							}
						}
					},
					// Pointer Pose
					new ActionConfig()
					{
						name = "pointer",
						localizedName = "Pointer Pose",
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
#if UNITY_ANDROID
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
								interactionPath = pokePose,
								interactionProfileName = profile,
							}
						}
					},
#endif
					// Haptics
					new ActionConfig()
					{
						name = "haptic",
						localizedName = "Haptic Output",
						type = ActionType.Vibrate,
						usages = new List<string>() { "Haptic" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = haptic,
								interactionProfileName = profile,
							}
						}
					}
				};
			}
			else
			{
				sb.Clear().Append("RequestActionConfigs() XR_EXT_hand_interaction is disabled."); DEBUG(sb);
				return new List<ActionConfig>()
				{
					// Thumbstick Axis
					new ActionConfig()
					{
						name = "thumbstick",
						localizedName = "Thumbstick Axis",
						type = ActionType.Axis2D,
						usages = new List<string>()
						{
							"Primary2DAxis"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstick,
								interactionProfileName = profile,
							}
						}
					},
					// Grip Axis
					new ActionConfig()
					{
						name = "grip",
						localizedName = "Grip Axis",
						type = ActionType.Axis1D,
						usages = new List<string>()
						{
							"Grip"
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
					// Grip Press
					new ActionConfig()
					{
						name = "gripPressed",
						localizedName = "Grip Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"GripButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = gripPress,
								interactionProfileName = profile,
							}
						}
					},
					// Menu
					new ActionConfig()
					{
						name = "menu",
						localizedName = "Menu",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"MenuButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = menu,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = system,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},
					// X / A Press
					new ActionConfig()
					{
						name = "primaryButton",
						localizedName = "Primary Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"PrimaryButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = buttonX,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = buttonA,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},


					// Y / B Press
					new ActionConfig()
					{
						name = "secondaryButton",
						localizedName = "Secondary Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"SecondaryButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = buttonY,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.leftHand }
							},
							new ActionBinding()
							{
								interactionPath = buttonB,
								interactionProfileName = profile,
								userPaths = new List<string>() { UserPaths.rightHand }
							},
						}
					},


					// Trigger Axis
					new ActionConfig()
					{
						name = "trigger",
						localizedName = "Trigger Axis",
						type = ActionType.Axis1D,
						usages = new List<string>()
						{
							"Trigger"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = trigger,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger Press
					new ActionConfig()
					{
						name = "triggerPressed",
						localizedName = "Trigger Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"TriggerButton"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = triggerClick,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger Touch
					new ActionConfig()
					{
						name = "triggerTouched",
						localizedName = "Trigger Touched",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"TriggerTouch"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = triggerTouch,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbstick Click
					new ActionConfig()
					{
						name = "thumbstickClicked",
						localizedName = "Thumbstick Pressed",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"Primary2DAxisClick"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstickClick,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbstick Touch
					new ActionConfig()
					{
						name = "thumbstickTouched",
						localizedName = "Thumbstick Touched",
						type = ActionType.Binary,
						usages = new List<string>()
						{
							"Primary2DAxisTouch"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbstickTouch,
								interactionProfileName = profile,
							}
						}
					},
					// Thumbrest Touch
					new ActionConfig()
					{
						name = "thumbrestTouched",
						localizedName = "Parking Touch",
						type = ActionType.Binary,
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = thumbrest,
								interactionProfileName = profile,
							}
						}
					},
					// Device Pose
					new ActionConfig()
					{
						name = "devicePose",
						localizedName = "Grip Pose",
						type = ActionType.Pose,
						usages = new List<string>()
						{
							"Device"
						},
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = gripPose,
								interactionProfileName = profile,
							}
						}
					},
					// Pointer Pose
					new ActionConfig()
					{
						name = "pointer",
						localizedName = "Pointer Pose",
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

					// Haptics
					new ActionConfig()
					{
						name = "haptic",
						localizedName = "Haptic Output",
						type = ActionType.Vibrate,
						usages = new List<string>() { "Haptic" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = haptic,
								interactionProfileName = profile,
							}
						}
					}
				};
			}
		}
		/// <summary>
		/// Register action maps for this device with the OpenXR Runtime. Called at runtime before Start.
		/// </summary>
		protected override void RegisterActionMapsWithRuntime()
		{
			ActionMapConfig actionMap = new ActionMapConfig()
			{
				name = "vivefocus3controller",
				localizedName = kDeviceLocalizedName,
				desiredInteractionProfile = profile,
				manufacturer = "HTC",
				serialNumber = "",
				deviceInfos = new List<DeviceConfig>()
				{
					new DeviceConfig()
					{
						characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left,
						userPath = UserPaths.leftHand // "/user/hand/left"
					},
					new DeviceConfig()
					{
						characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
						userPath = UserPaths.rightHand // "/user/hand/right"
					}
				},
				actions = RequestActionConfigs()
			};

			AddActionMap(actionMap);
		}

#region OpenXR function delegates
		/// xrGetInstanceProcAddr
		OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;
		/// xrEnumerateDisplayRefreshRatesFB
		OpenXRHelper.xrGetInputSourceLocalizedNameDelegate xrGetInputSourceLocalizedName = null;
		private bool GetXrFunctionDelegates(XrInstance xrInstance)
		{
			/// xrGetInstanceProcAddr
			if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
			{
				sb.Clear().Append("Get function pointer of xrGetInstanceProcAddr."); DEBUG(sb);
				XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
					xrGetInstanceProcAddr,
					typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
			}
			else
			{
				sb.Clear().Append("No function pointer of xrGetInstanceProcAddr"); ERROR(sb);
				return false;
			}

			IntPtr funcPtr = IntPtr.Zero;

			/// xrGetInputSourceLocalizedName
			if (XrGetInstanceProcAddr(xrInstance, "xrGetInputSourceLocalizedName", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					sb.Clear().Append("Get function pointer of xrGetInputSourceLocalizedName."); DEBUG(sb);
					xrGetInputSourceLocalizedName = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(OpenXRHelper.xrGetInputSourceLocalizedNameDelegate)) as OpenXRHelper.xrGetInputSourceLocalizedNameDelegate;
				}
				else
				{
					sb.Clear().Append("No function pointer of xrGetInputSourceLocalizedName.");
					ERROR(sb);
				}
			}
			else
			{
				sb.Clear().Append("No function pointer of xrGetInputSourceLocalizedName");
				ERROR(sb);
			}

			return true;
		}
#endregion

		private XrResult GetInputSourceName(XrPath path, XrInputSourceLocalizedNameFlags sourceType, out string sourceName)
		{
			string func = "GetInputSourceName() ";

			sourceName = "";
			if (!m_XrSessionCreated || xrGetInputSourceLocalizedName == null) { return XrResult.XR_ERROR_VALIDATION_FAILURE; }

			string userPath = PathToString(path);
			sb.Clear().Append(func).Append("userPath: ").Append(userPath).Append(", flag: ").Append((UInt64)sourceType); DEBUG(sb);

			XrInputSourceLocalizedNameGetInfo nameInfo = new XrInputSourceLocalizedNameGetInfo(
				XrStructureType.XR_TYPE_INPUT_SOURCE_LOCALIZED_NAME_GET_INFO,
				IntPtr.Zero, path, (XrInputSourceLocalizedNameFlags)sourceType);
			UInt32 nameSizeIn = 0;
			UInt32 nameSizeOut = 0;
			char[] buffer = new char[0];

			XrResult result = xrGetInputSourceLocalizedName(m_XrSession, ref nameInfo, nameSizeIn, ref nameSizeOut, buffer);
			if (result == XrResult.XR_SUCCESS)
			{
				if (nameSizeOut < 1)
				{
					sb.Clear().Append(func)
						.Append("xrGetInputSourceLocalizedName(").Append(userPath).Append(")")
						.Append(", flag: ").Append((UInt64)sourceType)
						.Append("bufferCountOutput size is invalid!");
					ERROR(sb);
					return XrResult.XR_ERROR_VALIDATION_FAILURE;
				}

				nameSizeIn = nameSizeOut;
				buffer = new char[nameSizeIn];

				result = xrGetInputSourceLocalizedName(m_XrSession, ref nameInfo, nameSizeIn, ref nameSizeOut, buffer);
				if (result == XrResult.XR_SUCCESS)
				{
					sourceName = new string(buffer).TrimEnd('\0');
					sb.Clear().Append(func)
						.Append("xrGetInputSourceLocalizedName(").Append(userPath).Append(")")
						.Append(", flag: ").Append((UInt64)sourceType)
						.Append(", bufferCapacityInput: ").Append(nameSizeIn)
						.Append(", bufferCountOutput: ").Append(nameSizeOut)
						.Append(", sourceName: ").Append(sourceName);
					DEBUG(sb);
				}
				else
				{
					sb.Clear().Append(func)
						.Append("2.xrGetInputSourceLocalizedName(").Append(userPath).Append(")")
						.Append(", flag: ").Append((UInt64)sourceType)
						.Append(", bufferCapacityInput: ").Append(nameSizeIn)
						.Append(", bufferCountOutput: ").Append(nameSizeOut)
						.Append(" result: ").Append(result);
					ERROR(sb);
				}
			}
			else
			{
				sb.Clear().Append(func)
					.Append("1.xrGetInputSourceLocalizedName(").Append(userPath).Append(")")
					.Append(", flag: ").Append((UInt64)sourceType)
					.Append(", bufferCapacityInput: ").Append(nameSizeIn)
					.Append(", bufferCountOutput: ").Append(nameSizeOut)
					.Append(" result: ").Append(result);
				ERROR(sb);
			}

			return result;
		}

#region OpenXR Life Cycle
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
			m_XrInstanceCreated = true;
			m_XrInstance = xrInstance;
			m_Instance = this;
			sb.Clear().Append("OnInstanceCreate() ").Append(m_XrInstance); DEBUG(sb);

			GetXrFunctionDelegates(m_XrInstance);
			return true;
		}
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroyInstance">xrDestroyInstance</see> is done.
		/// </summary>
		/// <param name="xrInstance">The instance to destroy.</param>
		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			if (m_XrInstance == xrInstance)
			{
				m_XrInstanceCreated = false;
				m_XrInstance = 0;
			}
			sb.Clear().Append("OnInstanceDestroy() ").Append(xrInstance); DEBUG(sb);
		}

		private bool m_XrSessionCreated = false;
		private XrSession m_XrSession = 0;
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateSession">xrCreateSession</see> is done.
		/// </summary>
		/// <param name="xrSession">The created session ID.</param>
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			sb.Clear().Append("OnSessionCreate() ").Append(m_XrSession); DEBUG(sb);
		}
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroySession">xrDestroySession</see> is done.
		/// </summary>
		/// <param name="xrSession">The session ID to destroy.</param>
		protected override void OnSessionDestroy(ulong xrSession)
		{
			sb.Clear().Append("OnSessionDestroy() ").Append(xrSession); DEBUG(sb);
			if (m_XrSession == xrSession)
			{
				m_XrSession = 0;
				m_XrSessionCreated = false;
			}
		}
#endregion
	}
}
