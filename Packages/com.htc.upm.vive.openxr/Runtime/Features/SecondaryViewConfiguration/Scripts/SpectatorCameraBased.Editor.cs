// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VIVE.OpenXR.SecondaryViewConfiguration
{
    /// <summary>
    /// Name: SpectatorCameraBased.Editor.cs
    /// Role: General script use in Unity Editor only
    /// Responsibility: Display the SpectatorCameraBased.cs in Unity Inspector
    /// </summary>
    public partial class SpectatorCameraBased
    {
#if UNITY_EDITOR
        [SerializeField, Tooltip("State of debugging the spectator camera or not")]
        private bool isDebugSpectatorCamera;
        
        /// <summary>
        /// State of debugging the spectator camera or not
        /// </summary>
        public bool IsDebugSpectatorCamera
        {
            get => isDebugSpectatorCamera;
            set
            {
                isDebugSpectatorCamera = value;
                if (!value)
                {
                    IsRecording = false;
                }
            }
        }

        [CustomEditor(typeof(SpectatorCameraBased))]
        public class SpectatorCameraBasedEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                // Just return if not "SpectatorCameraBased" class
                if (!(target is SpectatorCameraBased))
                {
                    return;
                }

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();
                DrawGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("SpectatorCameraBased script is changed.");
                    EditorUtility.SetDirty(target);
                }

                serializedObject.ApplyModifiedProperties();
            }

            private void DrawGUI()
            {
                var script = (SpectatorCameraBased)target;

                EditorGUI.BeginChangeCheck();
                var currentSpectatorCameraViewMaterial = EditorGUILayout.ObjectField(
                    "Spectator Camera View Material",
                    script.SpectatorCameraViewMaterial,
                    typeof(Material),
                    false) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, currentSpectatorCameraViewMaterial
                        ? "Change Spectator Camera View Material"
                        : "Set Spectator Camera View Material as NULL");
                    script.SpectatorCameraViewMaterial = currentSpectatorCameraViewMaterial;
                }
                
                EditorGUI.BeginChangeCheck();
                var currentIsDebugSpectatorCamera = 
                    EditorGUILayout.Toggle("Active Spectator Camera Debugging", script.IsDebugSpectatorCamera);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change IsDebugSpectatorCamera Value");
                    script.IsDebugSpectatorCamera = currentIsDebugSpectatorCamera;
                }

                if (script.IsDebugSpectatorCamera)
                {
                    EditorGUI.BeginChangeCheck();
                    var currentIsRecording = 
                        EditorGUILayout.Toggle("Active Spectator Camera Recording", script.IsRecording);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Change IsRecording Value");
                        script.IsRecording = currentIsRecording;
                    }
                }
                else
                {
                    script.IsRecording = false;
                }

                if (script.IsDebugSpectatorCamera && GUILayout.Button("Load \"Simple_Demo_2\" scene for testing"))
                {
                    if (DoesSceneExist("Simple_Demo_2"))
                    {
                        SceneManager.LoadScene("Simple_Demo_2");
                    }
                    else
                    {
                        Debug.LogWarning("Simple_Demo_2 scene not found. Please add it in build setting first.");
                    }
                }
            }
        }
        
        /// <summary>
        /// Returns true if the scene 'name' exists and is in your Build settings, false otherwise.
        /// </summary>
        private static bool DoesSceneExist(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var lastSlash = scenePath.LastIndexOf("/", StringComparison.Ordinal);
                var sceneName = scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".", StringComparison.Ordinal) - lastSlash - 1);

                if (string.Compare(name, sceneName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}