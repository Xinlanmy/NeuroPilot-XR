// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Support",
		Desc = "Necessary to deploy an VIVE XR compatible app.",
		Company = "HTC",
		DocumentationLink = "https://developer.vive.com/resources/openxr/openxr-mobile/tutorials/how-install-vive-wave-openxr-plugin/",
		OpenxrExtensionStrings = kOpenxrExtensionStrings,
		Version = "1.0.0",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		CustomRuntimeLoaderBuildTargets = new[] { BuildTarget.Android },
		FeatureId = featureId
	)]
#endif
	public class VIVEFocus3Feature : OpenXRFeature
	{
		const string LOG_TAG = "VIVE.OpenXR.VIVEFocus3Feature";
		static void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
		static void WARNING(string msg) { Debug.LogWarning(LOG_TAG + " " + msg); }
		static void ERROR(string msg) { Debug.LogError(LOG_TAG + " " + msg); }

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "com.unity.openxr.feature.vivefocus3";

		/// <summary>
		/// OpenXR specification <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_KHR_android_surface_swapchain">12.2. XR_KHR_android_surface_swapchain</see>.
		/// </summary>
		public const string kOpenxrExtensionStrings = "";

		/// <summary>
		/// Enable Hand Tracking or Not.
		/// </summary>
		//public bool enableHandTracking = false;

		/// <summary>
		/// Enable Tracker or Not.
		/// </summary>
		//public bool enableTracker = false;

		/// <inheritdoc />
		//protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		//{
		//	Debug.Log("EXT: registering our own xrGetInstanceProcAddr");
		//	return intercept_xrGetInstanceProcAddr(func);
		//}

		//private const string ExtLib = "viveopenxr";
		//[DllImport(ExtLib, EntryPoint = "intercept_xrGetInstanceProcAddr")]
		//private static extern IntPtr intercept_xrGetInstanceProcAddr(IntPtr func);

		private XrInstance m_XrInstance = 0;

		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			/*
			foreach (string kOpenxrExtensionString in kOpenxrExtensionStrings.Split(' '))
			{
				if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
				{
					WARNING("OnInstanceCreate() " + kOpenxrExtensionString + " is NOT enabled.");
				}
				else
				{
					DEBUG("OnInstanceCreate() " + kOpenxrExtensionString + " is enabled.");
				}
			}
			*/

			m_XrInstance = xrInstance;
			Debug.Log("OnInstanceCreate() " + m_XrInstance);

			return GetXrFunctionDelegates(xrInstance);
		}

		private static XrSession m_XrSession = 0;
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			DEBUG("OnSessionCreate() " + m_XrSession);
		}

		protected override void OnSessionDestroy(ulong xrSession)
		{
			DEBUG("OnSessionDestroy() " + xrSession);
			m_XrSession = 0;
		}

		/// xrGetInstanceProcAddr
		OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;

		private bool GetXrFunctionDelegates(XrInstance xrInstance)
		{
			/// xrGetInstanceProcAddr
			if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
			{
				DEBUG("Get function pointer of xrGetInstanceProcAddr.");
				XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
					xrGetInstanceProcAddr,
					typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
			}
			else
			{
				ERROR("xrGetInstanceProcAddr failed.");
				return false;
			}

			IntPtr funcPtr = IntPtr.Zero;

			return true;
		}

#if UNITY_EDITOR
		protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
		{
			rules.Add(new ValidationRule(this)
			{
				message = "The VIVE Focus 3 Controller Interaction profile is necessary.",
				checkPredicate = () =>
				{
					var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
					if (null == settings)
						return false;

					bool touchFeatureEnabled = false;
					foreach (var feature in settings.GetFeatures<OpenXRInteractionFeature>())
					{
						if (feature.enabled)
						{
							if (feature is VIVEFocus3Profile)
								touchFeatureEnabled = true;
						}
					}
					return touchFeatureEnabled;
				},
				fixIt = () =>
				{
					var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
					if (null == settings)
						return;

					foreach (var feature in settings.GetFeatures<OpenXRInteractionFeature>())
					{
						if (feature is VIVEFocus3Profile)
							feature.enabled = true;
					}
				},
				error = true,
			});
			rules.Add(CheckSpectatorCameraExtensionRequirement());
			rules.Add(ExtensionDuplicateEnabledChecker());
			rules.Add(ViveInteractionsShouldAlertRuleAndroid());
		}

		/// <summary>
		/// Get the ValidationRule to check whether the editor environment matches the spectator camera feature min requirement or not
		/// </summary>
		private ValidationRule CheckSpectatorCameraExtensionRequirement()
		{
			const bool isErrorTreatedOnBuild = true;
			const string openXrSettingPath = "Project/XR Plug-in Management/OpenXR";
			const string validationMessage = "Spectator Camera Feature is only available in Unity LTS version 2022.3.0f1 or above. Please ensure the environment matches the minimum requirement if enabled. Otherwise, please disable \"VIVE XR Spectator Camera\" checkbox or upgrade the Unity Editor.";

			return new ValidationRule(this)
			{
				message = validationMessage,
				error = isErrorTreatedOnBuild,
				checkPredicate = IsEnvMatchSpectatorCameraMinimalRequires,
				fixIt = () => SettingsService.OpenProjectSettings(openXrSettingPath)
			};
		}
		private static bool IsEnvMatchSpectatorCameraMinimalRequires()
		{
			if (IsViveSpectatorCameraEnabled())
			{
				// Spectator Camera Feature only available at Unity 2022 LTS or newer 
#if UNITY_2022_3_OR_NEWER
                return true;
#else
				return false;
#endif
			}

			// Spectator Camera Feature disable, so no need to check
			return true;
		}

		/// <summary>
		/// Check Spectator Camera Feature is enabled or not.
		/// </summary>
		/// <returns>True if Spectator Camera Feature is enabled. Otherwise, return False.</returns>
		public static bool IsViveSpectatorCameraEnabled()
		{
			GetOpenXrExtensionInfo(BuildTargetGroup.Android, out var myExtensionList, true);

			return myExtensionList.Any(extension =>
				string.Equals(extension.OpenXrExtensionStrings, SecondaryViewConfiguration.ViveSecondaryViewConfiguration.OPEN_XR_EXTENSION_STRING));
		}

		/// <summary>
		/// Get the ValidationRule to check if any OpenXR extensions are duplicately implemented and enabled.
		/// </summary>
		private ValidationRule ExtensionDuplicateEnabledChecker()
		{
			const bool isErrorTreatedOnBuild = true;
			const BuildTargetGroup checkTargetPlatform = BuildTargetGroup.Android;
			const string openXrSettingPath = "Project/XR Plug-in Management/OpenXR";
			const string validationMessage = "Compatibility and stability safeguard. Please ensure no duplicate " +
											 "OpenXR extensions are implemented and enabled.";

			return new ValidationRule(this)
			{
				message = validationMessage,
				error = isErrorTreatedOnBuild,
				checkPredicate = () => !IsOpenXrExtensionDuplicateEnabled(checkTargetPlatform),
				fixIt = () => SettingsService.OpenProjectSettings(openXrSettingPath)
			};
		}

		/// <summary>
		/// Are there any OpenXR extensions that are duplicately implemented and enabled?
		/// </summary>
		/// <param name="buildTargetGroup">The platform that is currently used.</param>
		/// <returns>True if there are any OpenXR extension is duplicately implemented and enabled. Otherwise, return false.</returns>
		private static bool IsOpenXrExtensionDuplicateEnabled(in BuildTargetGroup buildTargetGroup)
		{
			var extensionStringList = new List<string>();

			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
			foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRFeature>())
			{
				if (!feature.enabled)
				{
					continue;
				}

				FieldInfo fieldInfoOpenXrExtensionStrings = typeof(OpenXRFeature).GetField(
					"openxrExtensionStrings",
					BindingFlags.NonPublic | BindingFlags.Instance);
				if (fieldInfoOpenXrExtensionStrings != null)
				{
					var openXrExtensionStringsArray =
						((string)fieldInfoOpenXrExtensionStrings.GetValue(feature)).Split(' ');

					foreach (string stringItem in openXrExtensionStringsArray)
					{
						if (string.IsNullOrEmpty(stringItem))
						{
							continue;
						}

						if (extensionStringList.Contains(stringItem))
						{
							return true;
						}

						extensionStringList.Add(stringItem);
					}
				}
			}
			return false;
		}

		#region Rule of ViveInteractions
		private ValidationRule ViveInteractionsShouldAlertRuleAndroid()
		{
			const string validationMessage = "You should NOT enable VIVE XR - Interaction Group with no extension selected." +
				"\nYou can select the necessary extensions those your content needs through VIVE XR - Interaction Group settings.";
			const bool isErrorTreatedOnBuild = true;
			const string openXrSettingPath = "Project/XR Plug-in Management/OpenXR";

			return new ValidationRule(this)
			{
				message = validationMessage,
				error = isErrorTreatedOnBuild,
				checkPredicate = () => ViveInteractionsShouldAlert(),
				fixIt = () => SettingsService.OpenProjectSettings(openXrSettingPath)
			};
		}
		private static bool ViveInteractionsShouldAlert()
		{
			OpenXRSettings settings = null;
#if UNITY_ANDROID
			settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
#elif UNITY_STANDALONE
			settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
#endif
			if (settings != null)
			{
				foreach (var feature in settings.GetFeatures<OpenXRFeature>())
				{
					if (feature is Interaction.ViveInteractions)
					{
						if (!feature.enabled) { return true; }

						bool viveHandInteraction = ((Interaction.ViveInteractions)feature).UseViveHandInteraction();
						bool viveWristTracker = ((Interaction.ViveInteractions)feature).UseViveWristTracker();
						bool viveXrTracker = ((Interaction.ViveInteractions)feature).UseViveXrTracker();
						bool khrHandInteraction = ((Interaction.ViveInteractions)feature).UseKhrHandInteraction();

						return (viveHandInteraction || viveWristTracker || viveXrTracker || khrHandInteraction);
					}
				}
			}

			return true;
		}
		#endregion

		private struct OpenXrExtension
		{
			public string OpenXrExtensionStrings;
			public bool IsExtensionEnabled;
		}

		/// <summary>
		/// Get all OpenXR extension features according to the platform.
		/// </summary>
		/// <param name="buildTargetGroup">The platform that is currently used.</param>
		/// <param name="myExtensionList">List item that include all OpenXR extension features.</param>
		/// /// <param name="isRecordEnableOnly">Bool denotes that record extensions that enable only.</param>
		private static void GetOpenXrExtensionInfo
		(
			in BuildTargetGroup buildTargetGroup,
			out List<OpenXrExtension> myExtensionList,
			bool isRecordEnableOnly = false
		)
		{
			myExtensionList = new List<OpenXrExtension>();

			// FeatureHelpers.RefreshFeatures(buildTargetGroup);

			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
			foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRFeature>())
			{
				if (isRecordEnableOnly && !feature.enabled)
				{
					continue;
				}

				FieldInfo fieldInfoOpenXrExtensionStrings = typeof(OpenXRFeature).GetField(
					"openxrExtensionStrings",
					BindingFlags.NonPublic | BindingFlags.Instance);
				if (fieldInfoOpenXrExtensionStrings != null)
				{
					var openXrExtensionStringsArray =
						((string)fieldInfoOpenXrExtensionStrings.GetValue(feature)).Split(' ');

					foreach (var stringItem in openXrExtensionStringsArray)
					{
						if (string.IsNullOrEmpty(stringItem))
						{
							continue;
						}

						var openXrExtension = new OpenXrExtension
						{
							IsExtensionEnabled = feature.enabled,
							OpenXrExtensionStrings = stringItem
						};
						myExtensionList.Add(openXrExtension);
					}
				}
			}
		}

		/// <summary>
		/// Are there any OpenXR extensions that are duplicately implemented and enabled?
		/// </summary>
		/// <param name="openXrExtensionList">The OpenXR extension features list of the project.</param>
		/// <returns>The extension string that is duplicately implemented and enabled if it exists. Otherwise, return string.Empty.</returns>
		private static string IsOpenXrExtensionDuplicateEnabled(in List<OpenXrExtension> openXrExtensionList)
		{
			var extensionDuplicateEnabledName = string.Empty;
			var enableOpenXrExtension = new List<string>();

			foreach (OpenXrExtension extension in openXrExtensionList.Where(extension => extension.IsExtensionEnabled))
			{
				if (string.IsNullOrEmpty(extension.OpenXrExtensionStrings))
				{
					continue;
				}

				if (enableOpenXrExtension.FirstOrDefault(x =>
						x.Contains(extension.OpenXrExtensionStrings)) != null)
				{
					extensionDuplicateEnabledName = extension.OpenXrExtensionStrings;
					break;
				}

				enableOpenXrExtension.Add(extension.OpenXrExtensionStrings);
			}

			return extensionDuplicateEnabledName;
		}

		/// <summary>
		/// Is there a specific OpenXR extension that is duplicately implemented and enabled?
		/// </summary>
		/// <param name="openXrExtensionList">The OpenXR extension features list of the project.</param>
		/// <param name="checkOpenXrExtensionName">The string of the specific OpenXR extension that needs to be checked.</param>
		/// <returns>True if a specific OpenXR extension is duplicately implemented and enabled. Otherwise, return false.</returns>
		private static bool IsOpenXrExtensionDuplicateEnabled
		(
			in List<OpenXrExtension> openXrExtensionList,
			in string checkOpenXrExtensionName
		)
		{
			bool isExtensionExist = false;
			bool isExtensionDuplicate = false;

			foreach (var extension in openXrExtensionList)
			{
				if (!(string.Equals(extension.OpenXrExtensionStrings, checkOpenXrExtensionName)
					  && extension.IsExtensionEnabled))
				{
					continue;
				}

				if (!isExtensionExist)
				{
					isExtensionExist = true;
					continue;
				}

				isExtensionDuplicate = true;
				break;
			}

			return isExtensionDuplicate;
		}
#endif
	}
}
