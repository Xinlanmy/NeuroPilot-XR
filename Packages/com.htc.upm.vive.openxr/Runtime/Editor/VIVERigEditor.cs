// Copyright HTC Corporation All Rights Reserved.

#if UNITY_EDITOR
using UnityEditor;

namespace VIVE.OpenXR.Editor
{
    [CustomEditor(typeof(VIVERig))]
    public class VIVERigEditor : UnityEditor.Editor
    {
        SerializedProperty m_TrackingOrigin, m_CameraOffset, m_CameraHeight, m_ActionAsset;

		private void OnEnable()
		{
			m_TrackingOrigin = serializedObject.FindProperty("m_TrackingOrigin");
			m_CameraOffset = serializedObject.FindProperty("m_CameraOffset");
			m_CameraHeight = serializedObject.FindProperty("m_CameraHeight");
			m_ActionAsset = serializedObject.FindProperty("m_ActionAsset");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			VIVERig myScript = target as VIVERig;

			EditorGUILayout.PropertyField(m_TrackingOrigin);
			EditorGUILayout.PropertyField(m_CameraOffset);

			EditorGUILayout.HelpBox(
				"Set the height of camera when the Tracking Origin is Device.",
				MessageType.Info);
			EditorGUILayout.PropertyField(m_CameraHeight);

#if ENABLE_INPUT_SYSTEM
			EditorGUILayout.PropertyField(m_ActionAsset);
#endif

			serializedObject.ApplyModifiedProperties();
			if (UnityEngine.GUI.changed)
				EditorUtility.SetDirty((VIVERig)target);
		}
	}
}
#endif