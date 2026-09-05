// Copyright HTC Corporation All Rights Reserved.

using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.CompositionLayer
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Composition Layer (Extra Settings) (Beta)",
		Desc = "Enable this feature to use the Composition Layer Extra Settings.",
		Company = "HTC",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenxrExtensionStrings,
		Version = "1.0.0",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		FeatureId = featureId
	)]
#endif
	public class ViveCompositionLayerExtraSettings : OpenXRFeature
	{
		const string LOG_TAG = "VIVE.OpenXR.ViveCompositionLayer.ExtraSettings";
		static void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
		static void WARNING(string msg) { Debug.LogWarning(LOG_TAG + " " + msg); }
		static void ERROR(string msg) { Debug.LogError(LOG_TAG + " " + msg); }

		/// <summary>
		/// Settings Editor Enable Sharpening or Not.
		/// </summary>
		public bool SettingsEditorEnableSharpening = false;

		/// <summary>
		/// Support Sharpening or Not.
		/// </summary>
		public bool supportSharpening = false;

		/// <summary>
		/// Settings Editor Sharpening Mode
		/// </summary>
		public XrSharpeningModeHTC SettingsEditorSharpeningMode = XrSharpeningModeHTC.FAST;

		/// <summary>
		/// Settings Editor Sharpening Levell
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float SettingsEditorSharpeningLevel = 1.0f;

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.compositionlayer.extrasettings";

		/// <summary>
		/// OpenXR specification.
		/// </summary>
		public const string kOpenxrExtensionStrings = "XR_HTC_composition_layer_extra_settings";

		#region OpenXR Life Cycle
		private bool m_XrInstanceCreated = false;
		/// <summary>
		/// The XR instance is created or not.
		/// </summary>
		public bool XrInstanceCreated
		{
			get { return m_XrInstanceCreated; }
		}
		private XrInstance m_XrInstance = 0;
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			foreach (string kOpenxrExtensionString in kOpenxrExtensionStrings.Split(' '))
			{
				if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
				{
					WARNING("OnInstanceCreate() " + kOpenxrExtensionString + " is NOT enabled.");
				}
			}

			m_XrInstanceCreated = true;
			m_XrInstance = xrInstance;
			DEBUG("OnInstanceCreate() " + m_XrInstance);

			return true;
		}

		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			m_XrInstanceCreated = false;
			DEBUG("OnInstanceDestroy() " + m_XrInstance);
		}

		private XrSystemId m_XrSystemId = 0;
		protected override void OnSystemChange(ulong xrSystem)
		{
			m_XrSystemId = xrSystem;
			DEBUG("OnSystemChange() " + m_XrSystemId);
		}

		private bool m_XrSessionCreated = false;
		/// <summary>
		/// The XR session is created or not.
		/// </summary>
		public bool XrSessionCreated
		{
			get { return m_XrSessionCreated; }
		}
		private XrSession m_XrSession = 0;
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			DEBUG("OnSessionCreate() " + m_XrSession);
		}

		private bool m_XrSessionEnding = false;
		/// <summary>
		/// The XR session is ending or not.
		/// </summary>
		public bool XrSessionEnding
		{
			get { return m_XrSessionEnding; }
		}

		protected override void OnSessionBegin(ulong xrSession)
		{
			m_XrSessionEnding = false;
			DEBUG("OnSessionBegin() " + m_XrSession);

			//enable Sharpening 
			if (OpenXRRuntime.IsExtensionEnabled("XR_HTC_composition_layer_extra_settings"))
			{
				ViveCompositionLayer_UpdateSystemProperties(m_XrInstance, m_XrSystemId);
				supportSharpening = ViveCompositionLayer_IsSupportSharpening();
				if (supportSharpening && SettingsEditorEnableSharpening)
				{
					EnableSharpening(SettingsEditorSharpeningMode, SettingsEditorSharpeningLevel);
				}
			}
		}

		protected override void OnSessionEnd(ulong xrSession)
		{
			m_XrSessionEnding = true;
			DEBUG("OnSessionEnd() " + m_XrSession);
		}

		protected override void OnSessionDestroy(ulong xrSession)
		{
			m_XrSessionCreated = false;
			DEBUG("OnSessionDestroy() " + xrSession);
		}

		#endregion

		#region Wrapper Functions
		private const string ExtLib = "viveopenxr";

		[DllImportAttribute(ExtLib, EntryPoint = "viveCompositionLayer_UpdateSystemProperties")]
		private static extern int VIVEOpenXR_ViveCompositionLayer_UpdateSystemProperties(XrInstance instance, XrSystemId system_id);
		private int ViveCompositionLayer_UpdateSystemProperties(XrInstance instance, XrSystemId system_id)
		{
			return VIVEOpenXR_ViveCompositionLayer_UpdateSystemProperties(instance, system_id);
		}

		[DllImportAttribute(ExtLib, EntryPoint = "viveCompositionLayer_IsSupportSharpening")]
		private static extern bool VIVEOpenXR_ViveCompositionLayer_IsSupportSharpening();
		private bool ViveCompositionLayer_IsSupportSharpening()
		{
			return VIVEOpenXR_ViveCompositionLayer_IsSupportSharpening();
		}

		[DllImportAttribute(ExtLib, EntryPoint = "viveCompositionLayer_enableSharpening")]
		private static extern int VIVEOpenXR_ViveCompositionLayer_enableSharpening(XrSharpeningModeHTC sharpeningMode, float sharpeningLevel);
		/// <summary>
		/// Enable the sharpening setting applying to the projection layer.
		/// </summary>
		/// <param name="sharpeningMode">The sharpening mode in <see cref="XrSharpeningModeHTC"/>.</param>
		/// <param name="sharpeningLevel">The sharpening level in float [0, 1].</param>
		/// <returns>True for success.</returns>
		public bool EnableSharpening(XrSharpeningModeHTC sharpeningMode, float sharpeningLevel)
		{
			return (VIVEOpenXR_ViveCompositionLayer_enableSharpening(sharpeningMode, sharpeningLevel) == 0);
		}

		[DllImportAttribute(ExtLib, EntryPoint = "viveCompositionLayer_disableSharpening")]
		private static extern int VIVEOpenXR_ViveCompositionLayer_DisableSharpening();
		/// <summary>
		/// Disable the sharpening setting on the projection layer.
		/// </summary>
		/// <returns>True for success</returns>
		public bool DisableSharpening()
		{
			return (VIVEOpenXR_ViveCompositionLayer_DisableSharpening() == 0);
		}
		#endregion
	}
}