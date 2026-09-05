// Copyright HTC Corporation All Rights Reserved.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VIVE.OpenXR.CompositionLayer;

namespace VIVE.OpenXR.Editor.CompositionLayer
{
	[CustomEditor(typeof(ViveCompositionLayerExtraSettings))]
	internal class ViveCompositionLayerEditorExtraSettings : UnityEditor.Editor
	{
		//private SerializedProperty SettingsEditorEnableSharpening;

		static string PropertyName_SharpeningEnable = "SettingsEditorEnableSharpening";
		static GUIContent Label_SharpeningEnable = new GUIContent("Enable Sharpening", "Enable Sharpening.");
		SerializedProperty Property_SharpeningEnable;

		static string PropertyName_SharpeningLevel = "SettingsEditorSharpeningLevel";
		static GUIContent Label_SharpeningLevel = new GUIContent("Sharpening Level", "Select Sharpening Level.");
		SerializedProperty Property_SharpeningLevel;

		static string PropertyName_SharpeningMode = "SettingsEditorSharpeningMode";
		static GUIContent Label_SharpeningMode = new GUIContent("Sharpening Mode", "Select Sharpening Mode.");
		SerializedProperty Property_SharpeningMode;

		void OnEnable()
		{
			Property_SharpeningEnable = serializedObject.FindProperty(PropertyName_SharpeningEnable);
			Property_SharpeningMode = serializedObject.FindProperty(PropertyName_SharpeningMode);
			Property_SharpeningLevel = serializedObject.FindProperty(PropertyName_SharpeningLevel);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(Property_SharpeningEnable, new GUIContent(Label_SharpeningEnable));
			EditorGUILayout.PropertyField(Property_SharpeningMode, new GUIContent(Label_SharpeningMode));
			EditorGUILayout.PropertyField(Property_SharpeningLevel, new GUIContent(Label_SharpeningLevel));

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
