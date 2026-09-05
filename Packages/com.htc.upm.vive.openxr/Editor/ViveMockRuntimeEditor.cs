// Copyright HTC Corporation All Rights Reserved.
using UnityEditor;
using UnityEngine;
using VIVE.OpenXR.Feature;

namespace VIVE.OpenXR.Editor
{
    [CustomEditor(typeof(ViveMockRuntime))]
    internal class ViveMockRuntimeEditor : UnityEditor.Editor
    {
        private SerializedProperty enableFuture;
        private SerializedProperty enableAnchor;

        void OnEnable()
        {
            enableFuture = serializedObject.FindProperty("enableFuture");
            enableAnchor = serializedObject.FindProperty("enableAnchor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // Show a text field for description
            EditorGUILayout.HelpBox("VIVE's mock runtime.  Used with OpenXR MockRuntime to test unsupported extensions and features on Editor.", MessageType.Info);

            if (GUILayout.Button("Install MockRuntime Library")) {
                InstallMockRuntimeLibrary();
            }

            // check if changed
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enableFuture);
            if (EditorGUI.EndChangeCheck()) {
                if (!enableFuture.boolValue) {
                    enableAnchor.boolValue = false;
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enableAnchor);
            if (EditorGUI.EndChangeCheck()) {
                if (enableAnchor.boolValue) {
                    enableFuture.boolValue = true;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void InstallMockRuntimeLibrary() {
            string sourcePathName = "Packages/com.htc.upm.vive.openxr/MockRuntime~/Win64/ViveMockRuntime.dll";
            string destPath = "Assets/Plugins/Win64";
            string destPathName = "Assets/Plugins/Win64/ViveMockRuntime.dll";

            // check if the folder exists.  If not, create it.
            if (!System.IO.Directory.Exists(destPath)) {
                System.IO.Directory.CreateDirectory(destPath);
            }
           
            FileUtil.CopyFileOrDirectory(sourcePathName, destPathName);
            AssetDatabase.Refresh();
        }
    }
}
