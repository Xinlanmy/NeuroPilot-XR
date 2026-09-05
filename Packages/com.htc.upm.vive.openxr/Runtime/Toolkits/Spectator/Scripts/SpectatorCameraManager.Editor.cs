// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VIVE.OpenXR.Toolkits.Spectator.Helper;

namespace VIVE.OpenXR.Toolkits.Spectator
{
    /// <summary>
    /// Name: SpectatorCameraManager.Editor.cs
    /// Role: General script use in Unity Editor only
    /// Responsibility: Display the SpectatorCameraManager.cs in Unity Inspector
    /// </summary>
    public partial class SpectatorCameraManager
    {
        [SerializeField] private Material spectatorCameraViewMaterial;

        /// <summary>
        /// Material that show the spectator camera view
        /// </summary>
        private Material SpectatorCameraViewMaterial
        {
            // get; private set;
            get => spectatorCameraViewMaterial;
            set
            {
                spectatorCameraViewMaterial = value;
                if (SpectatorCameraBased && value)
                {
                    SpectatorCameraBased.SpectatorCameraViewMaterial = value;
                }
            }
        }

#if UNITY_EDITOR
        [field: SerializeField] private bool IsShowHmdPart { get; set; }
        [field: SerializeField] private bool IsShowTrackerPart { get; set; }
        [field: SerializeField] private bool IsRequireLoadJsonFile { get; set; }

        [CustomEditor(typeof(SpectatorCameraManager))]
        public class SpectatorCameraManagerEditor : UnityEditor.Editor
        {
            private static readonly Color HighlightRegionBackgroundColor = new Color(.2f, .2f, .2f, .1f);

            private SerializedProperty IsShowHmdPart { get; set; }
            private SerializedProperty IsShowTrackerPart { get; set; }
            private SerializedProperty IsRequireLoadJsonFile { get; set; }
            private List<string> JsonFileList { get; set; }
            private Vector2 JsonFileScrollViewVector { get; set; }

            private SerializedProperty IsSmoothCameraMovement { get; set; }
            private SerializedProperty SmoothCameraMovementSpeed { get; set; }
            private SerializedProperty PanoramaResolution { get; set; }
            private SerializedProperty PanoramaOutputFormat { get; set; }
            private SerializedProperty PanoramaOutputType { get; set; }

            private SerializedProperty SpectatorCameraPrefab { get; set; }

            private void OnEnable()
            {
                IsShowHmdPart = serializedObject.FindProperty(EditorHelper.PropertyName("IsShowHmdPart"));
                IsShowTrackerPart = serializedObject.FindProperty(EditorHelper.PropertyName("IsShowTrackerPart"));
                IsRequireLoadJsonFile =
                    serializedObject.FindProperty(EditorHelper.PropertyName("IsRequireLoadJsonFile"));
                JsonFileList = new List<string>();
                JsonFileScrollViewVector = Vector2.zero;

                IsSmoothCameraMovement =
                    serializedObject.FindProperty(EditorHelper.PropertyName("IsSmoothCameraMovement"));
                SmoothCameraMovementSpeed =
                    serializedObject.FindProperty(EditorHelper.PropertyName("SmoothCameraMovementSpeed"));

                PanoramaResolution = serializedObject.FindProperty(EditorHelper.PropertyName("PanoramaResolution"));
                PanoramaOutputFormat = serializedObject.FindProperty(EditorHelper.PropertyName("PanoramaOutputFormat"));
                PanoramaOutputType = serializedObject.FindProperty(EditorHelper.PropertyName("PanoramaOutputType"));

                SpectatorCameraPrefab =
                    serializedObject.FindProperty(EditorHelper.PropertyName("SpectatorCameraPrefab"));
            }

            public override void OnInspectorGUI()
            {
                // Just return if not "SpectatorCameraManager" class
                if (!(target is SpectatorCameraManager))
                {
                    return;
                }

                serializedObject.Update();

                DrawGUI();

                serializedObject.ApplyModifiedProperties();
            }

            private void DrawGUI()
            {
                #region GUIStyle

                var labelStyle = new GUIStyle()
                {
                    richText = true,
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.green : Color.black
                    }
                };

                var resetButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.yellow : Color.red
                    },
                    hover = new GUIStyleState
                    {
                        textColor = Color.red
                    },
                    active = new GUIStyleState
                    {
                        textColor = Color.cyan
                    },
                };

                var boldButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                };

                #endregion

                var script = (SpectatorCameraManager)target;

                // Button for reset value
                if (GUILayout.Button("Reset to default value", resetButtonStyle))
                {
                    Undo.RecordObject(target, "Reset SpectatorCameraManager to default value");
                    EditorUtility.SetDirty(target);
                    script.ResetSetting();
                }

                // Button for export setting
                if (GUILayout.Button("Export Spectator Camera HMD Setting", boldButtonStyle))
                {
                    script.ExportSetting2JsonFile(SpectatorCameraHelper.AttributeFileLocation.ResourceFolder);
                    AssetDatabase.Refresh();
                }

                #region Load Setting From JSON File

                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(IsRequireLoadJsonFile.boolValue);
                if (GUILayout.Button("Load Setting From JSON File in Resources Folder", boldButtonStyle) ||
                    IsRequireLoadJsonFile.boolValue)
                {
                    IsRequireLoadJsonFile.boolValue = true;
                    var searchPattern =
                        $"{SpectatorCameraHelper.ATTRIBUTE_FILE_PREFIX_NAME}*.{SpectatorCameraHelper.ATTRIBUTE_FILE_EXTENSION}";

                    if (JsonFileList == null)
                    {
                        JsonFileList = new List<string>();
                    }

                    JsonFileList.Clear();

                    var dir = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources"));
                    var files = dir.GetFiles(searchPattern);
                    foreach (var item in files)
                    {
                        JsonFileList.Add(item.Name);
                    }

                    if (JsonFileList.Count == 0)
                    {
                        Debug.Log(
                            "Can't find any JSON file related to the spectator camera setting in the Resources folder.");
                        IsRequireLoadJsonFile.boolValue = false;
                    }
                }

                EditorGUI.EndDisabledGroup();

                if (IsRequireLoadJsonFile.boolValue)
                {
                    if (GUILayout.Button("Cancel"))
                    {
                        IsRequireLoadJsonFile.boolValue = false;
                    }
                }

                GUILayout.EndHorizontal();

                if (IsRequireLoadJsonFile.boolValue)
                {
                    Rect r = EditorGUILayout.BeginVertical();

                    JsonFileScrollViewVector = EditorGUILayout.BeginScrollView(JsonFileScrollViewVector,
                        GUILayout.Width(r.width),
                        GUILayout.Height(80));

                    for (int i = 0; i < JsonFileList.Count; i++)
                    {
                        if (GUILayout.Button(JsonFileList[i]))
                        {
                            var path = Path.Combine(
                                System.IO.Path.Combine(Application.dataPath, "Resources"),
                                JsonFileList[i]);
                            Undo.RecordObject(target, $"Load {JsonFileList[i]} setting to {target.name} SpectatorCameraManager");
                            EditorUtility.SetDirty(target);
                            script.LoadSettingFromJsonFile(path);
                        }
                    }

                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.EndVertical();
                }

                #endregion

                EditorGUILayout.LabelField("\n");

                // Spectator camera prefab
                EditorGUILayout.PropertyField(SpectatorCameraPrefab, new GUIContent("Spectator Camera Prefab"));
                if (SpectatorCameraPrefab.objectReferenceValue != null &&
                    PrefabUtility.GetPrefabAssetType(SpectatorCameraPrefab.objectReferenceValue) ==
                    PrefabAssetType.NotAPrefab)
                {
                    // The assign object is scene object
                    Debug.Log("Please assign the object as prefab only.");
                    SpectatorCameraPrefab.objectReferenceValue = null;
                }

                EditorGUILayout.LabelField("\n");

                EditorGUILayout.LabelField("<b>[ General Setting ]</b>", labelStyle);

                // Setting of spectator camera reference source
                // EditorGUILayout.PropertyField(CameraSourceRef, new GUIContent("Camera Source"));
                EditorGUI.BeginChangeCheck();
                var currentCameraSourceRef = (SpectatorCameraHelper.CameraSourceRef)
                    EditorGUILayout.EnumPopup("Camera Source", script.CameraSourceRef);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Modified SpectatorCameraManager CameraSourceRef");
                    EditorUtility.SetDirty(target);
                    script.CameraSourceRef = currentCameraSourceRef;
                }

                #region Tracker Region

                if (script.CameraSourceRef == SpectatorCameraHelper.CameraSourceRef.Tracker)
                {
                    EditorGUI.BeginChangeCheck();
                    var currentFollowSpectatorCameraTracker = EditorGUILayout.ObjectField(
                        "Tracker",
                        script.FollowSpectatorCameraTracker,
                        typeof(SpectatorCameraTracker),
                        true) as SpectatorCameraTracker;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Modified SpectatorCameraManager FollowSpectatorCameraTracker");
                        EditorUtility.SetDirty(target);
                        script.FollowSpectatorCameraTracker = currentFollowSpectatorCameraTracker;
                    }

                    if (script.FollowSpectatorCameraTracker == null)
                    {
                        // The assign object is null
                        EditorGUILayout.HelpBox("Please assign the SpectatorCameraTracker", MessageType.Info, false);
                    }
                    else if (PrefabUtility.GetPrefabAssetType(script.FollowSpectatorCameraTracker) !=
                             PrefabAssetType.NotAPrefab)
                    {
                        // Don't allow assign object is prefab
                        Debug.Log("Please assign the scene object.");
                        script.FollowSpectatorCameraTracker = null;
                    }
                    else
                    {
                        // The assign object is scene object => ok
                        EditorGUILayout.LabelField("\n");
                        IsShowTrackerPart.boolValue =
                            EditorGUILayout.Foldout(IsShowTrackerPart.boolValue, "Tracker Setting");
                        if (IsShowTrackerPart.boolValue)
                        {
                            // If show the tracker setting
                            Rect r = EditorGUILayout.BeginVertical();

                            SpectatorCameraTracker trackerObject = script.FollowSpectatorCameraTracker;

                            if (trackerObject != null)
                            {
                                EditorGUILayout.HelpBox(
                                    $"You are now editing the tracker setting in \"{trackerObject.gameObject.name}\" GameObject",
                                    MessageType.Info,
                                    true);

                                EditorGUI.BeginChangeCheck();
                                var currentTrackerObjectLayerMask =
                                    LayerMaskHelper.LayerMaskDrawer.LayerMaskField("Camera Layer Mask",
                                        trackerObject.LayerMask);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} LayerMask");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.LayerMask = currentTrackerObjectLayerMask;
                                }

                                EditorGUI.BeginChangeCheck();
                                var currentTrackerObjectIsSmoothCameraMovement=
                                    EditorGUILayout.Toggle("Enable Smoothing Camera Movement",
                                        trackerObject.IsSmoothCameraMovement);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} IsSmoothCameraMovement");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.IsSmoothCameraMovement = currentTrackerObjectIsSmoothCameraMovement;
                                }

                                if (trackerObject.IsSmoothCameraMovement)
                                {
                                    EditorGUILayout.LabelField("\n");

                                    EditorGUILayout.LabelField("<b>[ Smooth Camera Movement Speed Setting ]</b>",
                                        labelStyle);
                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectSmoothCameraMovementSpeed =
                                        EditorGUILayout.IntSlider(
                                            new GUIContent("Speed of Smoothing Camera Movement"),
                                            trackerObject.SmoothCameraMovementSpeed,
                                            SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_MIN,
                                            SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_MAX);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} SmoothCameraMovementSpeed");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.SmoothCameraMovementSpeed = currentTrackerObjectSmoothCameraMovementSpeed;
                                    }

                                    EditorGUILayout.LabelField("\n");
                                }

                                EditorGUI.BeginChangeCheck();
                                // Spectator camera frustum show/hide
                                var currentTrackerObjectIsFrustumShowed =
                                    EditorGUILayout.Toggle("Enable Camera FOV Frustum", trackerObject.IsFrustumShowed);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} IsFrustumShowed");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.IsFrustumShowed = currentTrackerObjectIsFrustumShowed;
                                }

                                EditorGUILayout.LabelField("\n");

                                #region VerticalFov

                                EditorGUILayout.LabelField("<b>[ Vertical FOV Setting ]</b>", labelStyle);

                                EditorGUI.BeginChangeCheck();
                                var currentTrackerObjectVerticalFov = EditorGUILayout.Slider(
                                    "Vertical FOV",
                                    trackerObject.VerticalFov,
                                    SpectatorCameraHelper.VERTICAL_FOV_MIN,
                                    SpectatorCameraHelper.VERTICAL_FOV_MAX);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} VerticalFov");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.VerticalFov = currentTrackerObjectVerticalFov;
                                }

                                #endregion

                                EditorGUILayout.LabelField("\n");

                                #region Setting related to panorama capturing of spectator camera

                                // Panorama resolution
                                EditorGUILayout.LabelField("<b>[ Panorama Setting ]</b>", labelStyle);
                                
                                EditorGUI.BeginChangeCheck();
                                // Panorama output resolution
                                var currentTrackerObjectPanoramaResolution =
                                    (SpectatorCameraHelper.SpectatorCameraPanoramaResolution)
                                    EditorGUILayout.EnumPopup("Resolution", trackerObject.PanoramaResolution);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} PanoramaResolution");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.PanoramaResolution = currentTrackerObjectPanoramaResolution;
                                }

                                EditorGUI.BeginChangeCheck();
                                // Panorama output format
                                var currentTrackerObjectPanoramaOutputFormat = (TextureProcessHelper.PictureOutputFormat)
                                    EditorGUILayout.EnumPopup("Output Format", trackerObject.PanoramaOutputFormat);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} PanoramaOutputFormat");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.PanoramaOutputFormat = currentTrackerObjectPanoramaOutputFormat;
                                }

                                EditorGUI.BeginChangeCheck();
                                // Panorama output type
                                var currentTrackerObjectPanoramaOutputType = (TextureProcessHelper.PanoramaType)
                                    EditorGUILayout.EnumPopup("Output Type", trackerObject.PanoramaOutputType);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} PanoramaOutputType");
                                    EditorUtility.SetDirty(trackerObject);
                                    trackerObject.PanoramaOutputType = currentTrackerObjectPanoramaOutputType;
                                }

                                #endregion

                                EditorGUILayout.LabelField("\n");

                                #region Setting related to frustum

                                if (trackerObject.IsFrustumShowed)
                                {
                                    EditorGUILayout.LabelField("<b>[ Frustum Setting ]</b>",
                                        labelStyle);

                                    #region Count of frustum and frustum center line

                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumLineCount = (SpectatorCameraHelper.FrustumLineCount)
                                        EditorGUILayout.EnumPopup("Frustum Line Total", trackerObject.FrustumLineCount);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumLineCount");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumLineCount = currentTrackerObjectFrustumLineCount;
                                    }
                                    
                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumCenterLineCount = 
                                        (SpectatorCameraHelper.FrustumCenterLineCount)
                                        EditorGUILayout.EnumPopup("Frustum Center Line Total",
                                            trackerObject.FrustumCenterLineCount);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumCenterLineCount");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumCenterLineCount = currentTrackerObjectFrustumCenterLineCount;
                                    }

                                    #endregion

                                    EditorGUILayout.LabelField("\n");

                                    #region Width of frustum and frustum center line

                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumLineWidth =
                                        EditorGUILayout.Slider(
                                            "Frustum Line Width",
                                            trackerObject.FrustumLineWidth,
                                            SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MIN,
                                            SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MAX);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumLineWidth");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumLineWidth = currentTrackerObjectFrustumLineWidth;
                                    }
                                    
                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumCenterLineWidth =
                                        EditorGUILayout.Slider(
                                            "Frustum Center Line Width",
                                            trackerObject.FrustumCenterLineWidth,
                                            SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MIN,
                                            SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MAX);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumCenterLineWidth");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumCenterLineWidth = currentTrackerObjectFrustumCenterLineWidth;
                                    }

                                    #endregion

                                    EditorGUILayout.LabelField("\n");

                                    #region Material of frustum and frustum center line

                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumLineColor = EditorGUILayout.ColorField(
                                        "Frustum Line Color", trackerObject.FrustumLineColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumLineColor");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumLineColor = currentTrackerObjectFrustumLineColor;
                                    }
                                    
                                    EditorGUI.BeginChangeCheck();
                                    var currentTrackerObjectFrustumCenterLineColor = EditorGUILayout.ColorField(
                                        "Frustum Center Line Color",
                                        trackerObject.FrustumCenterLineColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(trackerObject, $"Modified {trackerObject.name} FrustumCenterLineColor");
                                        EditorUtility.SetDirty(trackerObject);
                                        trackerObject.FrustumCenterLineColor = currentTrackerObjectFrustumCenterLineColor;
                                    }

                                    #endregion
                                }

                                #endregion

                                EditorGUILayout.EndVertical();
                                r = new Rect(r.x, r.y, r.width, r.height);
                                EditorGUI.DrawRect(r, HighlightRegionBackgroundColor);

                                EditorGUILayout.LabelField("\n");
                            }
                        }
                    }
                }

                #endregion

                #region HMD Region

                IsShowHmdPart.boolValue = EditorGUILayout.Foldout(IsShowHmdPart.boolValue, "HMD Setting");
                if (IsShowHmdPart.boolValue)
                {
                    Rect r = EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Setting of spectator camera layer mask
                    var currentLayerMask =
                        LayerMaskHelper.LayerMaskDrawer.LayerMaskField("Camera Layer Mask", script.LayerMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Modified SpectatorCameraManager LayerMask");
                        EditorUtility.SetDirty(target);
                        script.LayerMask = currentLayerMask;
                    }

                    // Setting of smooth spectator camera movement
                    EditorGUILayout.PropertyField(IsSmoothCameraMovement,
                        new GUIContent("Enable Smoothing Camera Movement"));
                    if (IsSmoothCameraMovement.boolValue)
                    {
                        EditorGUILayout.LabelField("\n");

                        EditorGUILayout.LabelField("<b>[ Smooth Camera Movement Speed Setting ]</b>", labelStyle);

                        // Setting of smooth spectator camera movement speed
                        EditorGUILayout.IntSlider(
                            SmoothCameraMovementSpeed,
                            SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_MIN,
                            SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_MAX,
                            "Speed of Smoothing Camera Movement");

                        EditorGUILayout.LabelField("\n");
                    }

                    EditorGUI.BeginChangeCheck();
                    // Spectator camera frustum show/hide
                    var currentIsFrustumShowed =
                        EditorGUILayout.Toggle("Enable Camera FOV Frustum", script.IsFrustumShowed);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Modified SpectatorCameraManager IsFrustumShowed");
                        EditorUtility.SetDirty(target);
                        script.IsFrustumShowed = currentIsFrustumShowed;
                    }

                    EditorGUILayout.LabelField("\n");

                    #region VerticalFov

                    EditorGUILayout.LabelField("<b>[ Vertical FOV Setting ]</b>", labelStyle);

                    EditorGUI.BeginChangeCheck();
                    // FOV
                    var currentVerticalFov = EditorGUILayout.Slider(
                        "Vertical FOV",
                        script.VerticalFov,
                        SpectatorCameraHelper.VERTICAL_FOV_MIN,
                        SpectatorCameraHelper.VERTICAL_FOV_MAX);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Modified SpectatorCameraManager VerticalFov");
                        EditorUtility.SetDirty(target);
                        script.VerticalFov = currentVerticalFov;
                    }

                    #endregion

                    EditorGUILayout.LabelField("\n");

                    #region Setting related to panorama capturing of spectator camera

                    // Panorama resolution
                    EditorGUILayout.LabelField("<b>[ Panorama Setting ]</b>", labelStyle);
                    EditorGUILayout.PropertyField(PanoramaResolution, new GUIContent("Resolution"));

                    // Panorama output format
                    EditorGUILayout.PropertyField(PanoramaOutputFormat, new GUIContent("Output Format"));

                    // Panorama output type
                    EditorGUILayout.PropertyField(PanoramaOutputType, new GUIContent("Output Type"));

                    #endregion

                    #region Setting related to frustum

                    if (script.IsFrustumShowed)
                    {
                        EditorGUILayout.LabelField("\n");

                        EditorGUILayout.LabelField("<b>[ Frustum Setting ]</b>", labelStyle);

                        EditorGUI.BeginChangeCheck();
                        // Count of frustum line
                        var currentFrustumLineCount = (SpectatorCameraHelper.FrustumLineCount)
                            EditorGUILayout.EnumPopup("Frustum Line Total", script.FrustumLineCount);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumLineCount");
                            EditorUtility.SetDirty(target);
                            script.FrustumLineCount = currentFrustumLineCount;
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        // Count of frustum center line
                        var currentFrustumCenterLineCount = (SpectatorCameraHelper.FrustumCenterLineCount)
                            EditorGUILayout.EnumPopup("Frustum Center Line Total", script.FrustumCenterLineCount);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumCenterLineCount");
                            EditorUtility.SetDirty(target);
                            script.FrustumCenterLineCount = currentFrustumCenterLineCount;
                        }

                        EditorGUILayout.LabelField("\n");

                        EditorGUI.BeginChangeCheck();
                        // Width of frustum line
                        var currentFrustumLineWidth =
                            EditorGUILayout.Slider(
                                "Frustum Line Width",
                                script.FrustumLineWidth,
                                SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MIN,
                                SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MAX);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumLineWidth");
                            EditorUtility.SetDirty(target);
                            script.FrustumLineWidth = currentFrustumLineWidth;
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        // Width of frustum center line
                        var currentFrustumCenterLineWidth =
                            EditorGUILayout.Slider(
                                "Frustum Center Line Width",
                                script.FrustumCenterLineWidth,
                                SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MIN,
                                SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MAX);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumCenterLineWidth");
                            EditorUtility.SetDirty(target);
                            script.FrustumCenterLineWidth = currentFrustumCenterLineWidth;
                        }

                        EditorGUILayout.LabelField("\n");

                        EditorGUI.BeginChangeCheck();
                        // Color of frustum line
                        var currentFrustumLineColor = EditorGUILayout.ColorField(
                            "Frustum Line Color", script.FrustumLineColor);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumLineColor");
                            EditorUtility.SetDirty(target);
                            script.FrustumLineColor = currentFrustumLineColor;
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        // Color of frustum center line
                        var currentFrustumCenterLineColor = EditorGUILayout.ColorField(
                            "Frustum Center Line Color",
                            script.FrustumCenterLineColor);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "Modified SpectatorCameraManager FrustumCenterLineColor");
                            EditorUtility.SetDirty(target);
                            script.FrustumCenterLineColor = currentFrustumCenterLineColor;
                        }
                    }

                    #endregion

                    EditorGUILayout.EndVertical();
                    r = new Rect(r.x, r.y, r.width, r.height);
                    EditorGUI.DrawRect(r, HighlightRegionBackgroundColor);
                }

                #endregion

                EditorGUILayout.LabelField("\n");

                #region Test 360 Output

                EditorGUILayout.LabelField("<b>[ Debug Setting ]</b>", labelStyle);

                EditorGUI.BeginChangeCheck();
                var currentSpectatorCameraViewMaterial = EditorGUILayout.ObjectField(
                    "Spectator Camera View Material",
                    script.SpectatorCameraViewMaterial,
                    typeof(Material),
                    false) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Modified SpectatorCameraManager SpectatorCameraViewMaterial");
                    EditorUtility.SetDirty(target);
                    script.SpectatorCameraViewMaterial = currentSpectatorCameraViewMaterial;
                }

                EditorGUILayout.HelpBox("Test - Output 360 photo", MessageType.Info, true);
                if (GUILayout.Button("Test - Output 360 photo"))
                {
                    script.CaptureSpectatorCamera360Photo();
                }

                #endregion
            }
        }
#endif
    }
}