// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VIVE.OpenXR.SecondaryViewConfiguration
{
    /// <summary>
    /// Name: SecondaryViewConfiguration.Editor.cs
    /// Role: General script use in Unity Editor only
    /// Responsibility: Display the SecondaryViewConfiguration.cs in Unity Project Settings
    /// </summary>
    public partial class ViveSecondaryViewConfiguration
    {
        [field: SerializeField] internal bool IsAllowSpectatorCameraCapture360Image { get; set; }
        [field: SerializeField] internal bool IsEnableDebugLog { get; set; }

#if UNITY_EDITOR
        [CustomEditor(typeof(ViveSecondaryViewConfiguration))]
        public class ViveSecondaryViewConfigurationEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                // Just return if not "ViveSecondaryViewConfiguration" class
                if (!(target is ViveSecondaryViewConfiguration))
                {
                    return;
                }

                serializedObject.Update();
                DrawGUI();
                serializedObject.ApplyModifiedProperties();
            }

            private void DrawGUI()
            {
                var script = (ViveSecondaryViewConfiguration)target;
                
                EditorGUI.BeginChangeCheck();
                var currentIsAllowSpectatorCameraCapture360Image =
                    EditorGUILayout.Toggle("Allow capture panorama", script.IsAllowSpectatorCameraCapture360Image);
                if (EditorGUI.EndChangeCheck())
                {
                    if (currentIsAllowSpectatorCameraCapture360Image && !PlayerSettings.enable360StereoCapture)
                    {
                        const string acceptButtonString = 
                            "OK";
                        const string cancelButtonString = 
                            "Cancel";
                        const string openCapture360ImageAdditionRequestTitle =
                            "Additional Request of Capturing 360 Image throughout the Spectator Camera";
                        const string openCapture360ImageAdditionRequestDescription =
                            "Allow the spectator camera to capture 360 images. Addition Request:\n" +
                            "1.) Open the \"enable360StereoCapture\" in the Unity Player Setting " +
                            "Page.";
                    
                        bool acceptDialog1 = EditorUtility.DisplayDialog(
                            openCapture360ImageAdditionRequestTitle,
                            openCapture360ImageAdditionRequestDescription,
                            acceptButtonString,
                            cancelButtonString);

                        if (acceptDialog1)
                        {
                            PlayerSettings.enable360StereoCapture = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                    
                    Undo.RecordObject(target, "Modified ViveSecondaryViewConfiguration IsAllowSpectatorCameraCapture360Image");
                    EditorUtility.SetDirty(target);
                    script.IsAllowSpectatorCameraCapture360Image = currentIsAllowSpectatorCameraCapture360Image;
                }
                
                EditorGUI.BeginChangeCheck();
                var currentIsEnableDebugLog =
                    EditorGUILayout.Toggle("Print log for debugging", script.IsEnableDebugLog);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Modified ViveSecondaryViewConfiguration IsEnableDebugLog");
                    EditorUtility.SetDirty(target);
                    script.IsEnableDebugLog = currentIsEnableDebugLog;
                }
            }
        }
#endif
    }
}