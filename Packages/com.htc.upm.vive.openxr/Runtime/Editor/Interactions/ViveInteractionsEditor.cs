// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;

using VIVE.OpenXR.Interaction;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace VIVE.OpenXR.Editor.Interaction
{
	[CustomEditor(typeof(ViveInteractions))]
	public class ViveInteractionsEditor : UnityEditor.Editor
	{
		SerializedProperty m_ViveHandInteraction, m_ViveWristTracker, m_ViveXRTracker;
#if UNITY_ANDROID
		SerializedProperty m_KHRHandInteraction;
#endif

		private void OnEnable()
		{
			m_ViveHandInteraction = serializedObject.FindProperty("m_ViveHandInteraction");
			m_ViveWristTracker = serializedObject.FindProperty("m_ViveWristTracker");
			m_ViveXRTracker = serializedObject.FindProperty("m_ViveXRTracker");
#if UNITY_ANDROID
			m_KHRHandInteraction = serializedObject.FindProperty("m_KHRHandInteraction");
#endif
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			#region GUI
			GUIStyle boxStyleInfo = new GUIStyle(EditorStyles.helpBox);
			boxStyleInfo.fontSize = 12;
			boxStyleInfo.wordWrap = true;

			GUIStyle boxStyleWarning = new GUIStyle(EditorStyles.helpBox);
			boxStyleWarning.fontSize = 12;
			boxStyleWarning.fontStyle = FontStyle.Bold;
			boxStyleInfo.wordWrap = true;

			// ViveHandInteraction
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label(
				"The VIVE Hand Interaction feature enables hand selection and squeezing functions of XR_HTC_hand_interaction extension.\n" +
				"Please note that enabling this feature impacts runtime performance.",
				boxStyleInfo);
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(m_ViveHandInteraction);

			// ViveWristTracker
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label(
				"The VIVE Wrist Tracker feature enables wrist tracker pose and button functions of XR_HTC_vive_wrist_tracker_interaction extension.\n" +
				"Please note that enabling this feature impacts runtime performance.",
				boxStyleInfo);
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(m_ViveWristTracker);

			// ViveXrTracker
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label(
				"The VIVE XR Tracker feature enables ultimate tracker pose and button functions.\n" +
				"WARNING:\n" +
				"Please be aware that enabling this feature significantly affects runtime performance.",
				boxStyleWarning);
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(m_ViveXRTracker);
			/*
#if UNITY_ANDROID
			// ViveHandInteractionExt
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label(
				"The KHR Hand Interaction feature enables hand functions of XR_EXT_hand_interaction extension.\n" +
				"Please note that enabling this feature impacts runtime performance.",
				boxStyleInfo);
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(m_KHRHandInteraction);
#endif
			*/
			#endregion

			ViveInteractions myScript = target as ViveInteractions;
			if (myScript.enabled)
			{
				bool viveHandInteraction = myScript.UseViveHandInteraction();
				bool viveWristTracker = myScript.UseViveWristTracker();
				bool viveXrTracker = myScript.UseViveXrTracker();
				//bool khrHandInteraction = myScript.UseKhrHandInteraction();

				OpenXRSettings settings = null;
#if UNITY_ANDROID
				settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
#elif UNITY_STANDALONE
				settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
#endif
				if (settings != null)
				{
					bool addPathEnumeration = false;
					foreach (var feature in settings.GetFeatures<OpenXRInteractionFeature>())
					{
						if (feature is Hand.ViveHandInteraction) { feature.enabled = viveHandInteraction; }
						if (feature is Tracker.ViveWristTracker) { feature.enabled = viveWristTracker; }
						if (feature is Tracker.ViveXRTracker)
						{
							feature.enabled = viveXrTracker;
							addPathEnumeration = viveXrTracker;
						}
						//if (feature is Hand.ViveHandInteractionExt) { feature.enabled = khrHandInteraction; }
					}

					foreach (var feature in settings.GetFeatures<OpenXRFeature>())
					{
						if (addPathEnumeration && feature is VivePathEnumeration) { feature.enabled = true; }
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}

	/*public class ViveInteractionsBuildHook : OpenXRFeatureBuildHooks
	{
		public override int callbackOrder => 1;
		public override Type featureType => typeof(VIVEFocus3Feature);
		protected override void OnPostGenerateGradleAndroidProjectExt(string path)
		{
		}
		protected override void OnPostprocessBuildExt(BuildReport report)
		{
		}
		protected override void OnPreprocessBuildExt(BuildReport report)
		{
			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
			if (settings != null)
			{
				foreach (var feature in settings.GetFeatures<OpenXRFeature>())
				{
					if (feature is ViveInteractions && feature.enabled)
					{
						bool viveHandInteraction= ((ViveInteractions)feature).UseViveHandInteraction();
						bool viveWristTracker	= ((ViveInteractions)feature).UseViveWristTracker();
						bool viveXrTracker		= ((ViveInteractions)feature).UseViveXrTracker();
						bool khrHandInteraction = ((ViveInteractions)feature).UseKhrHandInteraction();
						Debug.LogFormat($"ViveInteractionsBuildHook() viveHandInteraction: {viveHandInteraction}, viveWristTracker: {viveWristTracker}, viveXrTracker: {viveXrTracker}, khrHandInteraction: {khrHandInteraction}");

						EnableInteraction(viveHandInteraction, viveWristTracker, viveXrTracker, khrHandInteraction);
						break;
					}
				}
			}
		}

		private static void EnableInteraction(
			bool viveHandInteraction = false,
			bool viveWristTracker = false,
			bool viveXrTracker = false,
			bool khrHandInteraction = false)
		{
			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
			if (settings == null) { return; }

			foreach (var feature in settings.GetFeatures<OpenXRInteractionFeature>())
			{
				if (feature is Hand.ViveHandInteraction) { feature.enabled = viveHandInteraction; Debug.LogFormat($"EnableInteraction() ViveHandInteraction: {feature.enabled}"); }
				if (feature is Tracker.ViveWristTracker) { feature.enabled = viveWristTracker; Debug.LogFormat($"EnableInteraction() ViveWristTracker: {feature.enabled}"); }
				if (feature is Tracker.ViveXRTracker) { feature.enabled = viveXrTracker; Debug.LogFormat($"EnableInteraction() ViveXRTracker: {feature.enabled}"); }
				if (feature is Hand.ViveHandInteractionExt) { feature.enabled = khrHandInteraction; Debug.LogFormat($"EnableInteraction() ViveHandInteractionExt: {feature.enabled}"); }
			}
		}
	}*/
}
#endif