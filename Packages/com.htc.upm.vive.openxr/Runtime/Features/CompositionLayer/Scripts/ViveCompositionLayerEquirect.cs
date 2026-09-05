// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using AOT;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.CompositionLayer
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Composition Layer (Equirect)",
		Desc = "Enable this feature to enable the Composition Layer Equirect Extension",
		Company = "HTC",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenXRCylinderExtensionString,
		Version = "1.0.0",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		FeatureId = featureId
	)]
#endif
	public class ViveCompositionLayerEquirect : OpenXRFeature
	{
		const string LOG_TAG = "VIVE.OpenXR.ViveCompositionLayer.Equirect";
		static void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
		static void WARNING(string msg) { Debug.LogWarning(LOG_TAG + " " + msg); }
		static void ERROR(string msg) { Debug.LogError(LOG_TAG + " " + msg); }

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.compositionlayer.equirect";

		private const string kOpenXRCylinderExtensionString = "XR_KHR_composition_layer_equirect XR_KHR_composition_layer_equirect2";

		private bool m_EquirectExtensionEnabled = true;
		/// <summary>
		/// The extension "XR_KHR_composition_layer_equirect" is enabled or not.
		/// </summary>
		public bool EquirectExtensionEnabled
		{
			get { return m_EquirectExtensionEnabled; }
		}

		private bool m_Equirect2ExtensionEnabled = true;
		/// <summary>
		/// The extension "XR_KHR_composition_layer_equirect2" is enabled or not.
		/// </summary>
		public bool Equirect2ExtensionEnabled
		{
			get { return m_Equirect2ExtensionEnabled; }
		}


		#region OpenXR Life Cycle
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			if (!OpenXRRuntime.IsExtensionEnabled("XR_KHR_composition_layer_equirect"))
			{
				WARNING("OnInstanceCreate() " + "XR_KHR_composition_layer_equirect" + " is NOT enabled.");

				m_EquirectExtensionEnabled = false;
				return false;
			}
			
			if (!OpenXRRuntime.IsExtensionEnabled("XR_KHR_composition_layer_equirect2"))
			{
				WARNING("OnInstanceCreate() " + "XR_KHR_composition_layer_equirect2" + " is NOT enabled.");

				m_Equirect2ExtensionEnabled = false;
				return false;
			}

			return true;
		}
		#endregion

		#region Wrapper Functions
		private const string ExtLib = "viveopenxr";

		[DllImportAttribute(ExtLib, EntryPoint = "submit_CompositionLayerEquirect")]
		public static extern void VIVEOpenXR_Submit_CompositionLayerEquirect(XrCompositionLayerEquirectKHR equirect, LayerType layerType, uint compositionDepth, int layerID);
		/// <summary>
		/// submit compostion layer of type equirect.
		/// </summary>
		public void Submit_CompositionLayerEquirect(XrCompositionLayerEquirectKHR equirect, LayerType layerType, uint compositionDepth, int layerID)
		{
			if (!EquirectExtensionEnabled)
			{
				ERROR("Submit_CompositionLayerEquirect: " + "XR_KHR_composition_layer_equirect" + " is NOT enabled.");
			}

			VIVEOpenXR_Submit_CompositionLayerEquirect(equirect, layerType, compositionDepth, layerID);
		}
		
		[DllImportAttribute(ExtLib, EntryPoint = "submit_CompositionLayerEquirect2")]
		public static extern void VIVEOpenXR_Submit_CompositionLayerEquirect2(XrCompositionLayerEquirect2KHR equirect2, LayerType layerType, uint compositionDepth, int layerID);
		/// <summary>
		/// submit compostion layer of type equirect2.
		/// </summary>
		public void Submit_CompositionLayerEquirect2(XrCompositionLayerEquirect2KHR equirect2, LayerType layerType, uint compositionDepth, int layerID)
		{
			if (!Equirect2ExtensionEnabled)
			{
				ERROR("Submit_CompositionLayerEquirect2: " + "XR_KHR_composition_layer_equirect2" + " is NOT enabled.");
			}

			VIVEOpenXR_Submit_CompositionLayerEquirect2(equirect2, layerType, compositionDepth, layerID);
		}
		#endregion
	}
}
