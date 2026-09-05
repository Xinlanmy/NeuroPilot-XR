// Copyright HTC Corporation All Rights Reserved.
using UnityEditor;
using VIVE.OpenXR.Feature;

namespace VIVE.OpenXR.Editor
{
    [CustomEditor(typeof(ViveAnchor))]
    internal class ViveAnchorEditor : UnityEditor.Editor
    {
        private SerializedProperty enablePersistedAnchor;

        void OnEnable()
        {
            enablePersistedAnchor = serializedObject.FindProperty("enablePersistedAnchor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(enablePersistedAnchor);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
