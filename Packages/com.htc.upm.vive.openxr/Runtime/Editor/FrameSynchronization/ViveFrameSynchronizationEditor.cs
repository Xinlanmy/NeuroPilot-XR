// Copyright HTC Corporation All Rights Reserved.

#if UNITY_EDITOR
using UnityEditor;

using VIVE.OpenXR.FrameSynchronization;

namespace VIVE.OpenXR.Editor.FrameSynchronization
{
    [CustomEditor(typeof(ViveFrameSynchronization))]
    public class ViveFrameSynchronizationEditor : UnityEditor.Editor
    {
        SerializedProperty m_SynchronizationMode;

		private void OnEnable()
		{
			m_SynchronizationMode = serializedObject.FindProperty("m_SynchronizationMode");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_SynchronizationMode);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif