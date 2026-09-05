// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using VIVE.OpenXR.FirstPersonObserver;
using VIVE.OpenXR.SecondaryViewConfiguration;
using VIVE.OpenXR.Toolkits.Spectator.Helper;

namespace VIVE.OpenXR.Toolkits.Spectator
{
    /// <summary>
    /// Name: SpectatorCameraManager.cs
    /// Role: Manager (Singleton)
    /// Responsibility: Manage the spectator camera in content layer
    /// </summary>
    public partial class SpectatorCameraManager : MonoBehaviour, ISpectatorCameraSetting
    {
        private static SpectatorCameraManager _instance;
        
        /// <summary>
        /// SpectatorCameraManager static instance (Singleton)
        /// </summary>
        public static SpectatorCameraManager Instance => _instance;

        public static SpectatorCameraBased SpectatorCameraBased => SpectatorCameraBased.Instance;

        /// <summary>
        /// To denote whether the SpectatorCameraManager is init successfully or not
        /// </summary>
        private bool InitSuccess { get; set; }

        [SerializeField]
        private SpectatorCameraHelper.CameraSourceRef cameraSourceRef = SpectatorCameraHelper.CAMERA_SOURCE_REF_DEFAULT;

        /// <summary>
        /// The current spectator camera tracking source
        /// </summary>
        public SpectatorCameraHelper.CameraSourceRef CameraSourceRef
        {
            get => cameraSourceRef;
            set
            {
                if (cameraSourceRef == value)
                {
                    return;
                }

                cameraSourceRef = value;

                if (Application.isPlaying)
                {
                    CameraStateChangingProcessing();
                    SpectatorCameraBased.SetViewFromHmd(value == SpectatorCameraHelper.CameraSourceRef.Hmd);
                }
            }
        }

        [SerializeField] private LayerMask layerMask = SpectatorCameraHelper.LayerMaskDefault;

        public LayerMask LayerMask
        {
            get => layerMask;
            set
            {
                if (layerMask == value)
                {
                    return;
                }

                layerMask = value;

                // This var in here is only for hmd source. Tracker has own layer mask var.
                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SpectatorCameraBased.SpectatorCamera.cullingMask = layerMask;
                }
            }
        }

        [field: SerializeField]
        public bool IsSmoothCameraMovement { get; set; } = SpectatorCameraHelper.IS_SMOOTH_CAMERA_MOVEMENT_DEFAULT;

        [field: SerializeField]
        public int SmoothCameraMovementSpeed { get; set; } = SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_SPEED_DEFAULT;

        [SerializeField] private bool isFrustumShowed = SpectatorCameraHelper.IS_FRUSTUM_SHOWED_DEFAULT;

        public bool IsFrustumShowed
        {
            get => isFrustumShowed;
            set
            {
                if (isFrustumShowed == value)
                {
                    return;
                }

                isFrustumShowed = value;

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustum();
                }
            }
        }

        [SerializeField] private float verticalFov = SpectatorCameraHelper.VERTICAL_FOV_DEFAULT;

        public float VerticalFov
        {
            get => verticalFov;
            set
            {
                if (Math.Abs(verticalFov - value) < SpectatorCameraHelper.COMPARE_FLOAT_MEDIUM_THRESHOLD)
                {
                    return;
                }

                verticalFov = Mathf.Clamp(value,
                    SpectatorCameraHelper.VERTICAL_FOV_MIN,
                    SpectatorCameraHelper.VERTICAL_FOV_MAX);

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SpectatorCameraBased.SpectatorCamera.fieldOfView = verticalFov;
                    SetupFrustum();
                }
            }
        }

        #region Panorama properties

        [field: SerializeField]
        public SpectatorCameraHelper.SpectatorCameraPanoramaResolution PanoramaResolution { get; set; } =
            SpectatorCameraHelper.PANORAMA_RESOLUTION_DEFAULT;

        [field: SerializeField]
        public TextureProcessHelper.PictureOutputFormat PanoramaOutputFormat { get; set; } =
            SpectatorCameraHelper.PANORAMA_OUTPUT_FORMAT_DEFAULT;

        [field: SerializeField]
        public TextureProcessHelper.PanoramaType PanoramaOutputType { get; set; } =
            SpectatorCameraHelper.PANORAMA_TYPE_DEFAULT;

        #endregion

        [SerializeField] private SpectatorCameraHelper.FrustumLineCount frustumLineCount =
            SpectatorCameraHelper.FRUSTUM_LINE_COUNT_DEFAULT;

        public SpectatorCameraHelper.FrustumLineCount FrustumLineCount
        {
            get => frustumLineCount;
            set
            {
                if (frustumLineCount == value)
                {
                    return;
                }

                frustumLineCount = value;

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumLine();
                }
            }
        }

        [SerializeField] private SpectatorCameraHelper.FrustumCenterLineCount frustumCenterLineCount =
            SpectatorCameraHelper.FRUSTUM_CENTER_LINE_COUNT_DEFAULT;

        public SpectatorCameraHelper.FrustumCenterLineCount FrustumCenterLineCount
        {
            get => frustumCenterLineCount;
            set
            {
                if (frustumCenterLineCount == value)
                {
                    return;
                }

                frustumCenterLineCount = value;

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumCenterLine();
                }
            }
        }

        [SerializeField] private float frustumLineWidth = SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_DEFAULT;

        public float FrustumLineWidth
        {
            get => frustumLineWidth;
            set
            {
                if (Math.Abs(frustumLineWidth - value) < SpectatorCameraHelper.COMPARE_FLOAT_SUPER_SMALL_THRESHOLD)
                {
                    return;
                }

                frustumLineWidth = Mathf.Clamp(
                    value,
                    SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MIN,
                    SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_MAX);

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumLine();
                }
            }
        }

        [SerializeField] private float frustumCenterLineWidth = SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_DEFAULT;

        public float FrustumCenterLineWidth
        {
            get => frustumCenterLineWidth;
            set
            {
                if (Math.Abs(frustumCenterLineWidth - value) <
                    SpectatorCameraHelper.COMPARE_FLOAT_SUPER_SMALL_THRESHOLD)
                {
                    return;
                }

                frustumCenterLineWidth = Mathf.Clamp(value, SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MIN,
                    SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_MAX);

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumCenterLine();
                }
            }
        }

        [SerializeField] private Color frustumLineColor = SpectatorCameraHelper.LineColorDefault;

        public Color FrustumLineColor
        {
            get => frustumLineColor;
            set
            {
                if (frustumLineColor == value)
                {
                    return;
                }

                frustumLineColor = value;

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumLine();
                }
            }
        }

        [SerializeField] private Color frustumCenterLineColor = SpectatorCameraHelper.LineColorDefault;

        public Color FrustumCenterLineColor
        {
            get => frustumCenterLineColor;
            set
            {
                if (frustumCenterLineColor == value)
                {
                    return;
                }

                frustumCenterLineColor = value;

                if (Application.isPlaying && IsCameraSourceAsHmd())
                {
                    SetupFrustumCenterLine();
                }
            }
        }

        #region Varibles of the camera prefeb, camera gameobject, camera handler and main (rig) camera

        /// <summary>
        /// The camera prefab. It will be created as a GameObject and denote a spectator camera at runtime.
        /// </summary>
        [field: SerializeField] private GameObject SpectatorCameraPrefab { get; set; }

        /// <summary>
        /// The GameObject is created from SpectatorCameraPrefab.
        /// </summary>
        public GameObject SpectatorCamera { get; private set; }

        #endregion

        #region Varibles of last value of camera FOV and main camera position and rotation

        /// <summary>
        /// The previous value of the position of the main (hmd) camera (P.S. only use in CameraSourceRef.Hmd mode)
        /// </summary>
        private Vector3 CameraLastPosition { get; set; }

        /// <summary>
        /// The previous value of the rotation of the main (hmd) camera (P.S. only use in CameraSourceRef.Hmd mode)
        /// </summary>
        private Quaternion CameraLastRotation { get; set; }

        #endregion

        #region Variables of visualization FOV

        /// <summary>
        /// The frustum line root
        /// </summary>
        private GameObject FrustumLineRoot { get; set; }
        
        /// <summary>
        /// The frustum center line root
        /// </summary>
        private GameObject FrustumCenterLineRoot { get; set; }
        
        /// <summary>
        /// The list contain frustum line (LineRenderer)
        /// </summary>
        private List<LineRenderer> frustumLineList;
        
        /// <summary>
        /// The list contain frustum center line (LineRenderer)
        /// </summary>
        private List<LineRenderer> frustumCenterLineList;

        #endregion

        #region Varibles of tracker list includes all trackers in the scene, the current tracker candidate, and its index in the tracker list

        private List<SpectatorCameraTracker> spectatorCameraTrackerList;
        
        /// <summary>
        /// The tracker list that contains all tracker in the scene
        /// </summary>
        public IReadOnlyList<SpectatorCameraTracker> SpectatorCameraTrackerList => spectatorCameraTrackerList;

        [SerializeField] private SpectatorCameraTracker followSpectatorCameraTracker;

        /// <summary>
        /// The tracker candidate that the camera will follow in "Tracker mode".
        /// </summary>
        public SpectatorCameraTracker FollowSpectatorCameraTracker
        {
            get => followSpectatorCameraTracker;
            set
            {
                if (followSpectatorCameraTracker == value)
                {
                    return;
                }

                followSpectatorCameraTracker = value;

                if (Application.isPlaying)
                {
                    AddSpectatorCameraTracker(value);
                    if (IsCameraSourceAsTracker())
                    {
                        CameraStateChangingProcessing();
                    }
                }
            }
        }

        /// <summary>
        /// The index of the current candidate of the tracker in the tracker list.
        /// </summary>
        private int FollowSpectatorCameraTrackerIndex
        {
            get
            {
                if (spectatorCameraTrackerList != null && FollowSpectatorCameraTracker != null)
                {
                    return spectatorCameraTrackerList.IndexOf(FollowSpectatorCameraTracker);
                }

                return -1;
            }
        }

        #endregion

        #region Public Functions of camera setting I/O

        public void ResetSetting()
        {
            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
            LayerMask = SpectatorCameraHelper.LayerMaskDefault;
            IsSmoothCameraMovement = SpectatorCameraHelper.IS_SMOOTH_CAMERA_MOVEMENT_DEFAULT;
            SmoothCameraMovementSpeed = SpectatorCameraHelper.SMOOTH_CAMERA_MOVEMENT_SPEED_DEFAULT;
            IsFrustumShowed = SpectatorCameraHelper.IS_FRUSTUM_SHOWED_DEFAULT;
            VerticalFov = SpectatorCameraHelper.VERTICAL_FOV_DEFAULT;
            PanoramaResolution = SpectatorCameraHelper.PANORAMA_RESOLUTION_DEFAULT;
            PanoramaOutputFormat = SpectatorCameraHelper.PANORAMA_OUTPUT_FORMAT_DEFAULT;
            PanoramaOutputType = SpectatorCameraHelper.PANORAMA_TYPE_DEFAULT;
            FrustumLineCount = SpectatorCameraHelper.FRUSTUM_LINE_COUNT_DEFAULT;
            FrustumCenterLineCount = SpectatorCameraHelper.FRUSTUM_CENTER_LINE_COUNT_DEFAULT;
            FrustumLineWidth = SpectatorCameraHelper.FRUSTUM_LINE_WIDTH_DEFAULT;
            FrustumCenterLineWidth = SpectatorCameraHelper.FRUSTUM_CENTER_LINE_WIDTH_DEFAULT;
            FrustumLineColor = SpectatorCameraHelper.LineColorDefault;
            FrustumCenterLineColor = SpectatorCameraHelper.LineColorDefault;

            SpectatorCameraViewMaterial = null;
        }

        public void ExportSetting2JsonFile(in SpectatorCameraHelper.AttributeFileLocation attributeFileLocation)
        {
#if !UNITY_EDITOR
			if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.ResourceFolder)
			{
				Debug.LogError("It's not allowed to save setting to resource folder in runtime mode");
				return;
			}
#endif

            var data = new SpectatorCameraHelper.SpectatorCameraAttribute(
                CameraSourceRef,
                Vector3.zero, // HMD does not need to save the position
                Quaternion.identity, // HMD does not need to save the rotation
                LayerMask,
                IsSmoothCameraMovement,
                SmoothCameraMovementSpeed,
                IsFrustumShowed,
                VerticalFov,
                PanoramaResolution,
                PanoramaOutputFormat,
                PanoramaOutputType,
                FrustumLineCount,
                FrustumCenterLineCount,
                FrustumLineWidth,
                FrustumCenterLineWidth,
                FrustumLineColor,
                FrustumCenterLineColor);

#if UNITY_EDITOR
            if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.ResourceFolder)
            {
                SpectatorCameraHelper.SaveAttributeData2ResourcesFolder(
                    SceneManager.GetActiveScene().name,
                    gameObject.name,
                    data);
            }
            else if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.PersistentFolder)
            {
                SpectatorCameraHelper.SaveAttributeData2PersistentFolder(
                    SceneManager.GetActiveScene().name,
                    gameObject.name,
                    data);
            }
#else
			SpectatorCameraHelper.SaveAttributeData2PersistentFolder(
				SceneManager.GetActiveScene().name,
				gameObject.name,
				data);
#endif
        }

        public void LoadSettingFromJsonFile(in string jsonFilePath)
        {
            bool loadSuccess = SpectatorCameraHelper.LoadAttributeFileFromFolder(
                jsonFilePath,
                out SpectatorCameraHelper.SpectatorCameraAttribute data);
            if (loadSuccess)
            {
                ApplyData(data);
            }
            else
            {
                Debug.Log($"Load setting from {jsonFilePath} file to {gameObject.name} failed.");
            }
        }

        /// <summary>
        /// Load the setting (JSON) file via input scene name, GameObject (hmd) name, and the file location (resource folder or persistent folder).
        /// </summary>
        /// <param name="sceneName">The scene name.</param>
        /// <param name="hmdName">The GameObject name.</param>
        /// <param name="attributeFileLocation"> The enum SpectatorCameraHelper.AttributeFileLocation.</param>
        public void LoadSettingFromJsonFile(
            in string sceneName,
            in string hmdName,
            in SpectatorCameraHelper.AttributeFileLocation attributeFileLocation)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(hmdName))
            {
                Debug.LogError("Scene name or hmd name is null or empty");
                return;
            }

            var loadSuccess = false;
            SpectatorCameraHelper.SpectatorCameraAttribute data = new SpectatorCameraHelper.SpectatorCameraAttribute();
            if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.ResourceFolder)
            {
                loadSuccess = SpectatorCameraHelper.LoadAttributeFileFromResourcesFolder(
                    sceneName,
                    hmdName,
                    out data);
            }
            else if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.PersistentFolder)
            {
                loadSuccess = SpectatorCameraHelper.LoadAttributeFileFromPersistentFolder(
                    sceneName,
                    hmdName,
                    out data);
            }

            if (loadSuccess)
            {
                ApplyData(data);
            }
            else
            {
                var fileDirectory = string.Empty;
                if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.ResourceFolder)
                {
                    fileDirectory = System.IO.Path.Combine(Application.dataPath, "Resources");
                }
                else if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.PersistentFolder)
                {
                    fileDirectory = Application.persistentDataPath;
                }

                var fileName =
                    SpectatorCameraHelper.GetSpectatorCameraAttributeFileNamePattern(sceneName, hmdName);

                Debug.Log(
                    $"Load setting from {fileDirectory}/{fileName} file to {hmdName} failed.");
            }
        }

        public void ApplyData(in SpectatorCameraHelper.SpectatorCameraAttribute data)
        {
            CameraSourceRef = data.source;

            LayerMask = data.layerMask;
            IsSmoothCameraMovement = data.isSmoothCameraMovement;
            SmoothCameraMovementSpeed = data.smoothCameraMovementSpeed;
            IsFrustumShowed = data.isFrustumShowed;
            VerticalFov = data.verticalFov;
            PanoramaResolution = data.panoramaResolution;
            PanoramaOutputFormat = data.panoramaOutputFormat;
            PanoramaOutputType = data.panoramaOutputType;
            FrustumLineCount = data.frustumLineCount;
            FrustumCenterLineCount = data.frustumCenterLineCount;
            FrustumLineWidth = data.frustumLineWidth;
            FrustumCenterLineWidth = data.frustumCenterLineWidth;
            FrustumLineColor = data.frustumLineColor;
            FrustumCenterLineColor = data.frustumCenterLineColor;
        }

        #endregion

        #region Public functions of add/remove tracker

        /// <summary>
        /// Add the spectator camera tracker to the tracker list in SpectatorCameraManager.
        /// </summary>
        /// <param name="tracker">The tracker you want to add.</param>
        public void AddSpectatorCameraTracker(SpectatorCameraTracker tracker)
        {
            if (spectatorCameraTrackerList == null)
            {
                spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
            }

            if (spectatorCameraTrackerList.Contains(tracker))
            {
                return;
            }

            spectatorCameraTrackerList.Add(tracker);
        }

        /// <summary>
        /// Remove the spectator camera tracker from the tracker list in SpectatorCameraManager.
        /// </summary>
        /// <param name="tracker">The tracker you want to remove.</param>
        public void RemoveSpectatorCameraTracker(SpectatorCameraTracker tracker)
        {
            if (spectatorCameraTrackerList == null)
            {
                return;
            }

            if (spectatorCameraTrackerList.Contains(tracker))
            {
                spectatorCameraTrackerList.Remove(tracker);

                if (FollowSpectatorCameraTracker == tracker)
                {
                    if (spectatorCameraTrackerList.Count > 0)
                    {
                        // If the tracker that is removed is the current tracker candidate and there are still
                        // some trackers in the list, change the current tracker candidate to the first tracker
                        // in the list
                        FollowSpectatorCameraTracker = spectatorCameraTrackerList[0];
                    }
                    else
                    {
                        // If the tracker that is removed is the current tracker candidate and there is no
                        // tracker in the list, change the camera source to HMD and set the current tracker
                        // candidate to null
                        CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                        FollowSpectatorCameraTracker = null;
                    }
                }
            }
        }

        #endregion

        #region Public function of 360 photo capture

        /// <summary>
        /// Capture the 360 photo at the current spectator camera position and rotation.
        /// There are two types of panorama type: <b>Monoscopic</b> and <b>Stereoscopic</b>
        /// can be chosen.
        /// </summary>
        public void CaptureSpectatorCamera360Photo()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Pls play the application for 360 photo capture.");
                return;
            }

            if (!SpectatorCameraBased.IsAllowSpectatorCameraCapture360Image)
            {
                Debug.LogError(
                    "The spectator camera capture 360 image feature is not enabled, pls enable it on the" +
                    "OpenXR Setting page in Unity editor project setting first.");
                return;
            }

            // If the spectator handle is not set, return
            if (!IsSpectatorCameraHandlerExist())
            {
                Debug.LogError(
                    "Cannot init the function for capturing 360, pls make sure the spectator handler is init.");
                return;
            }

            #region Create a new camera component for capture 360 photo

            Transform refTransform;
            LayerMask layerMaskValue;
            SpectatorCameraHelper.SpectatorCameraPanoramaResolution panoramaResolutionValue;
            TextureProcessHelper.PictureOutputFormat panoramaOutputFormatValue;
            TextureProcessHelper.PanoramaType panoramaOutputTypeValue;

            // Create a new GameObject which position is according to the CameraSourceRef.
            // If CameraSourceRef = HMD, refer the transform from camera main (hmd),
            // otherwise, refer the transform from tracker.
            switch (CameraSourceRef)
            {
                case SpectatorCameraHelper.CameraSourceRef.Hmd:
                {
                    if (IsMainCameraExist())
                    {
                        refTransform = SpectatorCameraBased.GetMainCamera().transform;
                        layerMaskValue = LayerMask;
                        panoramaResolutionValue = PanoramaResolution;
                        panoramaOutputFormatValue = PanoramaOutputFormat;
                        panoramaOutputTypeValue = PanoramaOutputType;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot find the main camera in the scene to capture 360 photo.");
                        return;
                    }
                }
                    break;
                case SpectatorCameraHelper.CameraSourceRef.Tracker:
                {
                    if (!IsFollowTrackerExist())
                    {
                        if (spectatorCameraTrackerList == null)
                        {
                            spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                        }
                        
                        if (SpectatorCameraTrackerList.Count > 0)
                        {
                            // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                            // some trackers in the scene, change to use the first tracker in the list as default
                            FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                        }
                        else
                        {
                            // If there is no tracker in the scene, change to use the HMD as default
                            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                            return;
                        }
                    }

                    refTransform = FollowSpectatorCameraTracker.transform;
                    layerMaskValue = FollowSpectatorCameraTracker.LayerMask;
                    panoramaResolutionValue = FollowSpectatorCameraTracker.PanoramaResolution;
                    panoramaOutputFormatValue = FollowSpectatorCameraTracker.PanoramaOutputFormat;
                    panoramaOutputTypeValue = FollowSpectatorCameraTracker.PanoramaOutputType;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Rotation ignore but only Y-axis (yaw).
            var spectatorCamera360 = new GameObject
            {
                transform =
                {
                    position = refTransform.position
                }
            }.AddComponent<Camera>();

            // Set the spectatorCamera360's stereo target eye according to the panorama type
            spectatorCamera360.stereoTargetEye =
                panoramaOutputTypeValue is TextureProcessHelper.PanoramaType.Stereoscopic
                    ? StereoTargetEyeMask.Both
                    : StereoTargetEyeMask.None;
            // Set the spectatorCamera360's culling mask
            spectatorCamera360.cullingMask = layerMaskValue;
            // Set the spectatorCamera360's eye distance
            spectatorCamera360.stereoSeparation = SpectatorCameraHelper.STEREO_SEPARATION_DEFAULT;

            #endregion

            RenderTexture capture360ResultEquirect = TextureProcessHelper.Capture360RenderTexture(
                spectatorCamera360,
                (int)panoramaResolutionValue,
                panoramaOutputTypeValue);

            // Destroy the gameObject
            Destroy(spectatorCamera360.gameObject);

            if (capture360ResultEquirect == null)
            {
                Debug.LogWarning(
                    "Capture360RenderTexture return null, pls check the error log on the above for more details.");
                return;
            }

            var filename = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";

            try
            {
                TextureProcessHelper.SaveRenderTextureToDisk(
                    imageAlbumName: SpectatorCameraHelper.SAVE_PHOTO360_ALBUM_NAME
                    , imageNameWithoutFileExtension: filename
                    , imageOutputFormat: panoramaOutputFormatValue
                    , sourceRenderTexture: capture360ResultEquirect
                    , yawRotation: refTransform.rotation.eulerAngles.y
#if !UNITY_ANDROID || UNITY_EDITOR
                    , saveDirectory: Application.persistentDataPath
#endif
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Error on output the panoramic photo: {e}.");
            }
            finally
            {
                // Release the Temporary RenderTexture
                RenderTexture.ReleaseTemporary(capture360ResultEquirect);
            }
        }

        #endregion

        #region Public functions of safety check

        /// <summary>
        /// Check whether the current state of the spectator camera is following the hmd or not.
        /// </summary>
        /// <returns>True if the spectator camera is following the hmd. Otherwise, return false.</returns>
        public bool IsCameraSourceAsHmd()
        {
            return CameraSourceRef is SpectatorCameraHelper.CameraSourceRef.Hmd;
        }

        /// <summary>
        /// Check whether the current state of the spectator camera is following the tracker or not.
        /// </summary>
        /// <returns>True if the spectator camera is following the tracker. Otherwise, return false.</returns>
        public bool IsCameraSourceAsTracker()
        {
            return CameraSourceRef is SpectatorCameraHelper.CameraSourceRef.Tracker;
        }

        /// <summary>
        /// Check whether the current tracker tracked by the spectator camera equals the tracker you input.
        /// </summary>
        /// <param name="tracker">The tracker you want to check</param>
        /// <returns>True if equals. Otherwise, return false.</returns>
        public bool IsFollowTrackerEqualTo(SpectatorCameraTracker tracker)
        {
            return FollowSpectatorCameraTracker == tracker;
        }

        /// <summary>
        /// Check whether the spectator camera exists.
        /// </summary>
        /// <returns>True if the spectator camera exists. Otherwise, return false.</returns>
        public bool IsSpectatorCameraHandlerExist()
        {
            return SpectatorCameraBased.SpectatorCamera != null;
        }

        /// <summary>
        /// Check whether the main camera exists in the currently loaded scene.
        /// </summary>
        /// <returns>True if the main camera exists. Otherwise, return false.</returns>
        public bool IsMainCameraExist()
        {
            return SpectatorCameraBased.GetMainCamera() != null;
        }

        /// <summary>
        /// Check whether the current tracker tracked by the spectator camera exists or not.
        /// </summary>
        /// <returns>True if the tracker exists. Otherwise, return false.</returns>
        public bool IsFollowTrackerExist()
        {
            return FollowSpectatorCameraTracker != null;
        }

        #endregion

        #region Functions of visualization camera view ray

        /// <summary>
        /// Setup the frustum and frustum center line
        /// </summary>
        public void SetupFrustum()
        {
            SetupFrustumLine();
            SetupFrustumCenterLine();
        }

        /// <summary>
        /// Setup the frustum line
        /// </summary>
        public void SetupFrustumLine()
        {
            bool isFrustumShowedValue;
            SpectatorCameraHelper.FrustumLineCount frustumLineCountValue;
            float frustumLineWidthValue;
            Color frustumLineColorValue;

            switch (CameraSourceRef)
            {
                case SpectatorCameraHelper.CameraSourceRef.Hmd:
                {
                    if (IsMainCameraExist())
                    {
                        isFrustumShowedValue = IsFrustumShowed;
                        frustumLineCountValue = FrustumLineCount;
                        frustumLineWidthValue = FrustumLineWidth;
                        frustumLineColorValue = FrustumLineColor;
                    }
                    else
                    {
                        Debug.LogWarning("Main camera does not exist in the scene");
                        return;
                    }
                }
                    break;
                case SpectatorCameraHelper.CameraSourceRef.Tracker:
                {
                    if (!IsFollowTrackerExist())
                    {
                        if (spectatorCameraTrackerList == null)
                        {
                            spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                        }
                        
                        if (SpectatorCameraTrackerList.Count > 0)
                        {
                            // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                            // some trackers in the scene, change to use the first tracker in the list as default
                            FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                        }
                        else
                        {
                            // If there is no tracker in the scene, change to use the HMD as default
                            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                            return;
                        }
                    }

                    isFrustumShowedValue = FollowSpectatorCameraTracker.IsFrustumShowed;
                    frustumLineCountValue = FollowSpectatorCameraTracker.FrustumLineCount;
                    frustumLineWidthValue = FollowSpectatorCameraTracker.FrustumLineWidth;
                    frustumLineColorValue = FollowSpectatorCameraTracker.FrustumLineColor;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (
#if UNITY_EDITOR
                !(SpectatorCameraBased.IsDebugSpectatorCamera && SpectatorCameraBased.IsRecording) ||
#else
            (!SpectatorCameraBased.IsRecording &&
             /* Because OpenXR periodically changes the spectator enabled flag, we need
              to consider checking the state with a time delay so that we can make sure
              it is changing for a long while or just periodically. */
             Math.Abs(SpectatorCameraBased.LastRecordingStateIsActiveTime - SpectatorCameraBased.LastRecordingStateIsDisableTime) >
             SpectatorCameraBased.RECORDING_STATE_CHANGE_THRESHOLD_IN_SECOND
            ) ||
#endif
                !isFrustumShowedValue ||
                frustumLineCountValue is SpectatorCameraHelper.FrustumLineCount.None)
            {
                if (FrustumLineRoot != null)
                {
                    FrustumLineRoot.SetActive(false);
                }

                return;
            }

            if (FrustumLineRoot == null)
            {
                FrustumLineRoot = new GameObject(SpectatorCameraHelper.FRUSTUM_LINE_ROOT_NAME_DEFAULT);
                FrustumLineRoot.transform.SetParent(SpectatorCamera.transform, false);
            }

            FrustumLineRoot.SetActive(true);

            if (frustumLineList != null && frustumLineList.Count > 0)
            {
                // Destroy all the line renderer and then re-init
                // in order to make sure that using new variables
                // e.g. line count, width and color
                foreach (LineRenderer item in frustumLineList)
                {
                    Destroy(item.gameObject);
                }

                frustumLineList.Clear();
            }

            SetupLineRenderer(
                lineCount: (int)frustumLineCountValue,
                lineWidth: frustumLineWidthValue,
                lineNamePrefix: SpectatorCameraHelper.FRUSTUM_LINE_NAME_PREFIX_DEFAULT,
                lineMaterial: new Material(Shader.Find(SpectatorCameraHelper.LINE_SHADER_NAME_DEFAULT))
                {
                    color = frustumLineColorValue
                },
                lineParent: FrustumLineRoot.transform,
                lineList: out frustumLineList);
        }

        /// <summary>
        /// Setup the frustum center line
        /// </summary>
        public void SetupFrustumCenterLine()
        {
            bool isFrustumShowedValue;
            SpectatorCameraHelper.FrustumCenterLineCount frustumCenterLineCountValue;
            float frustumCenterLineWidthValue;
            Color frustumCenterLineColorValue;

            switch (CameraSourceRef)
            {
                case SpectatorCameraHelper.CameraSourceRef.Hmd:
                {
                    if (IsMainCameraExist())
                    {
                        isFrustumShowedValue = IsFrustumShowed;
                        frustumCenterLineCountValue = FrustumCenterLineCount;
                        frustumCenterLineWidthValue = FrustumCenterLineWidth;
                        frustumCenterLineColorValue = FrustumCenterLineColor;
                    }
                    else
                    {
                        Debug.LogWarning("Main camera does not exist in the scene");
                        return;
                    }
                }
                    break;
                case SpectatorCameraHelper.CameraSourceRef.Tracker:
                {
                    if (!IsFollowTrackerExist())
                    {
                        if (spectatorCameraTrackerList == null)
                        {
                            spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                        }
                        
                        if (SpectatorCameraTrackerList.Count > 0)
                        {
                            // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                            // some trackers in the scene, change to use the first tracker in the list as default
                            FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                        }
                        else
                        {
                            // If there is no tracker in the scene, change to use the HMD as default
                            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                            return;
                        }
                    }

                    isFrustumShowedValue = FollowSpectatorCameraTracker.IsFrustumShowed;
                    frustumCenterLineCountValue = FollowSpectatorCameraTracker.FrustumCenterLineCount;
                    frustumCenterLineWidthValue = FollowSpectatorCameraTracker.FrustumCenterLineWidth;
                    frustumCenterLineColorValue = FollowSpectatorCameraTracker.FrustumCenterLineColor;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (
#if UNITY_EDITOR
                !(SpectatorCameraBased.IsDebugSpectatorCamera && SpectatorCameraBased.IsRecording) ||
#else
            (!SpectatorCameraBased.IsRecording &&
             /* Because OpenXR periodically changes the spectator enabled flag, we need
              to consider checking the state with a time delay so that we can make sure
              it is changing for a long while or just periodically. */
             Math.Abs(SpectatorCameraBased.LastRecordingStateIsActiveTime - SpectatorCameraBased.LastRecordingStateIsDisableTime) >
             SpectatorCameraBased.RECORDING_STATE_CHANGE_THRESHOLD_IN_SECOND
            ) ||
#endif
                !isFrustumShowedValue ||
                frustumCenterLineCountValue is SpectatorCameraHelper.FrustumCenterLineCount.None)
            {
                if (FrustumCenterLineRoot != null)
                {
                    FrustumCenterLineRoot.SetActive(false);
                }

                return;
            }

            if (FrustumCenterLineRoot == null)
            {
                FrustumCenterLineRoot = new GameObject(SpectatorCameraHelper.FRUSTUM_CENTER_LINE_ROOT_NAME_DEFAULT);
                FrustumCenterLineRoot.transform.SetParent(SpectatorCamera.transform, false);
            }

            FrustumCenterLineRoot.SetActive(true);

            if (frustumCenterLineList != null && frustumCenterLineList.Count > 0)
            {
                // Destroy all the line renderer and then re-init
                // in order to make sure that using new variables
                // e.g. line count, width and color
                foreach (LineRenderer item in frustumCenterLineList)
                {
                    Destroy(item.gameObject);
                }

                frustumCenterLineList.Clear();
            }

            SetupLineRenderer(
                lineCount: (int)frustumCenterLineCountValue,
                lineWidth: frustumCenterLineWidthValue,
                lineNamePrefix: SpectatorCameraHelper.FRUSTUM_CENTER_LINE_NAME_PREFIX_DEFAULT,
                lineMaterial: new Material(Shader.Find(SpectatorCameraHelper.LINE_SHADER_NAME_DEFAULT))
                {
                    color = frustumCenterLineColorValue
                },
                lineParent: FrustumCenterLineRoot.transform,
                lineList: out frustumCenterLineList);
        }

        /// <summary>
        /// Setup the line renderer in order to render the frustum and frustum center line
        /// </summary>
        /// <param name="lineCount">The total number of the LineRenderer</param>
        /// <param name="lineWidth">The width of the line</param>
        /// <param name="lineNamePrefix">The GameObject name prefix. It attaches the LineRenderer component</param>
        /// <param name="lineMaterial">The material of the line</param>
        /// <param name="lineParent">The parent GameObject of all GameObjects that include the LineRenderer component</param>
        /// <param name="lineList">The return value, which is the list of the Line Renderer and all of it is already initiated with the input parameter</param>
        private void SetupLineRenderer(
            in int lineCount,
            in float lineWidth,
            in string lineNamePrefix,
            in Material lineMaterial,
            in Transform lineParent,
            out List<LineRenderer> lineList)
        {
            lineList = new List<LineRenderer>(lineCount);

            for (int i = 0; i < lineCount; i++)
            {
                var obj = new GameObject($"{lineNamePrefix}{i}");
                obj.transform.SetParent(lineParent, false);
                // Set to "UI" layer
                obj.layer = LayerMask.NameToLayer("UI");

                var lr = obj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.sharedMaterial = lineMaterial;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.alignment = LineAlignment.View;

                lineList.Add(lr);
            }

            CalculateLineRendererPosition(lineList);
        }

        /// <summary>
        /// Calculate the line renderer position
        /// </summary>
        /// <param name="lineList">The list that saves all LineRenderer</param>
        private void CalculateLineRendererPosition(List<LineRenderer> lineList)
        {
            if (!IsSpectatorCameraHandlerExist())
            {
                return;
            }

            var frustumCornersVector = new Vector3[SpectatorCameraHelper.FRUSTUM_OUT_CORNERS_COUNT];

            Camera spectatorHandlerInternalCamera = SpectatorCameraBased.SpectatorCamera;

            spectatorHandlerInternalCamera.CalculateFrustumCorners(
                new Rect(0, 0, 1, 1),
                spectatorHandlerInternalCamera.farClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                frustumCornersVector);

            int setLineStep = lineList.Count / SpectatorCameraHelper.FRUSTUM_OUT_CORNERS_COUNT;
            for (var currentCorner = 0;
                 currentCorner < SpectatorCameraHelper.FRUSTUM_OUT_CORNERS_COUNT;
                 currentCorner++)
            {
                for (var currentLineStep = 0; currentLineStep < setLineStep; currentLineStep++)
                {
                    Vector3 currentVector = Vector3.Lerp(
                        frustumCornersVector[currentCorner],
                        currentCorner < SpectatorCameraHelper.FRUSTUM_OUT_CORNERS_COUNT - 1
                            // If currentCorner is not the last one, draw the line between current corner and next corner.
                            ? frustumCornersVector[currentCorner + 1]
                            // Otherwise, draw the line between the last corner and the first corner.
                            : frustumCornersVector[0],
                        currentLineStep / (float)setLineStep);

                    SetLineRendererPosition(
                        lineList[currentCorner * setLineStep + currentLineStep],
                        currentVector,
                        SpectatorCameraHelper.FRUSTUM_LINE_BEGIN_DEFAULT);
                }
            }
        }

        /// <summary>
        /// Set the line renderer position
        /// </summary>
        /// <param name="lineRenderer">The LineRenderer that will set to</param>
        /// <param name="endPoint">The end position of the line</param>
        /// <param name="startOffset">The offset of the start position of the line</param>
        private static void SetLineRendererPosition(LineRenderer lineRenderer, Vector3 endPoint, float startOffset = 0)
        {
            lineRenderer.SetPosition(0, startOffset < SpectatorCameraHelper.COMPARE_FLOAT_SMALL_THRESHOLD
                ? Vector3.zero
                : (endPoint).normalized * startOffset);
            lineRenderer.SetPosition(1, endPoint);
        }

        #endregion

        /// <summary>
        /// The function is called when the camera source is changed.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Occur when enum CameraSourceRef is undefined.</exception>
        private void CameraStateChangingProcessing()
        {
            // Update layer mask
            // Update vertical fov
            // Update last position and rotation (avoid camera pose interpolation between two different camera source)
            // Update is frustum showed

            LayerMask layerMaskValue;
            float verticalFovValue;
            Transform changeCameraTransform;

            switch (CameraSourceRef)
            {
                case SpectatorCameraHelper.CameraSourceRef.Hmd:
                {
                    if (IsMainCameraExist())
                    {
                        layerMaskValue = LayerMask;
                        verticalFovValue = VerticalFov;
                        changeCameraTransform = SpectatorCameraBased.GetMainCamera().transform;
                    }
                    else
                    {
                        Debug.LogWarning("Main camera does not exist in the scene");
                        return;
                    }
                }
                    break;
                case SpectatorCameraHelper.CameraSourceRef.Tracker:
                {
                    if (!IsFollowTrackerExist())
                    {
                        if (spectatorCameraTrackerList == null)
                        {
                            spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                        }
                        
                        if (SpectatorCameraTrackerList.Count > 0)
                        {
                            // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                            // some trackers in the scene, change to use the first tracker in the list as default
                            FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                        }
                        else
                        {
                            // If there is no tracker in the scene, change to use the HMD as default
                            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                            return;
                        }
                    }

                    layerMaskValue = FollowSpectatorCameraTracker.LayerMask;
                    verticalFovValue = FollowSpectatorCameraTracker.VerticalFov;
                    changeCameraTransform = FollowSpectatorCameraTracker.transform;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CameraLastPosition = changeCameraTransform.position;
            CameraLastRotation = changeCameraTransform.rotation;

            SpectatorCameraBased.SpectatorCamera.cullingMask = layerMaskValue;
            SpectatorCameraBased.SpectatorCamera.fieldOfView = verticalFovValue;

            SetupFrustum();
        }

        /// <summary>
        /// Sometimes, if the user sets material on both SpectatorCameraBased or SpectatorCameraManager script,
        /// always use the top of control setting (aka SpectatorCameraManager) as a final value.
        /// This function will be called when the recording is started.
        /// </summary>
        private void SetSpectatorCameraViewMaterial()
        {
            if (SpectatorCameraViewMaterial ||
                SpectatorCameraBased.SpectatorCameraViewMaterial != SpectatorCameraViewMaterial)
            {
                SpectatorCameraBased.SpectatorCameraViewMaterial = SpectatorCameraViewMaterial;
            }
        }

        #region Unity lifecycle event functions

        private IEnumerator Start()
        {
            InitSuccess = false;
            
#if !UNITY_EDITOR && UNITY_ANDROID
            // To check, "XR_MSFT_first_person_observer" is enough because it
            // requires "XR_MSFT_secondary_view_configuration" to be enabled also.
            if (!ViveFirstPersonObserver.IsExtensionEnabled())
            {
                Debug.LogWarning(
                    $"The OpenXR extension, {ViveSecondaryViewConfiguration.OPEN_XR_EXTENSION_STRING} " +
                    $"or {ViveFirstPersonObserver.OPEN_XR_EXTENSION_STRING}, is disabled. " +
                    "Please enable the extension before building the app.");
                Debug.Log("Destroy the SpectatorCameraManager");
                Destroy(this);
                yield break;
            }
#endif

            if (_instance != null && _instance != this)
            {
                Debug.Log("Destroy the SpectatorCameraManager because it already exist one instance.");
                Destroy(this);
                yield break;
            }

            _instance = this;

            // To prevent this from being destroyed on load, check whether this gameObject has a parent;
            // if so, set it to no game parent.
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(_instance.gameObject);

            if (!SpectatorCameraBased)
            {
                _ = new GameObject("Spectator Camera Base", typeof(SpectatorCameraBased))
                {
                    transform =
                    {
                        position = Vector3.zero,
                        rotation = Quaternion.identity
                    }
                };

                // Wait one frame for creating SpectatorCameraBased.
                yield return null;
            }

            SpectatorCameraBased.SetViewFromHmd(CameraSourceRef == SpectatorCameraHelper.CameraSourceRef.Hmd);

            if (SpectatorCameraViewMaterial)
            {
                SpectatorCameraBased.SpectatorCameraViewMaterial = SpectatorCameraViewMaterial;
            }

            // Create camera
            CreateCameraPrefab();

            // Init camera parameter
            InitCameraPose();
            InitCameraFov();
            InitCameraLayerMask();

            // Register event
            SpectatorCameraBased.OnSpectatorStart += SetupFrustum;
            SpectatorCameraBased.OnSpectatorStart += SetSpectatorCameraViewMaterial;
            SpectatorCameraBased.OnSpectatorStop += SetupFrustum;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // For opening the app the first time, manually call SetupFrustum to
            // avoid missing show frustum if the user is already recording
            SetupFrustum();

            InitSuccess = true;

            yield break;

            // Create the spectator camera (prefab)
            void CreateCameraPrefab()
            {
                if (SpectatorCameraPrefab)
                {
                    SpectatorCamera = Instantiate(SpectatorCameraPrefab);
                }
                else
                {
                    SpectatorCamera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    SpectatorCamera.transform.localScale =
                        SpectatorCameraHelper.SpectatorCameraSpherePrefabScaleDefault;
                }

                SpectatorCameraBased.SpectatorCameraGameObject.transform.SetParent(SpectatorCamera.transform, false);

                DontDestroyOnLoad(SpectatorCamera);
            }

            // Init the camera pose
            void InitCameraPose()
            {
                Vector3 position;
                Quaternion rotation;

                switch (CameraSourceRef)
                {
                    case SpectatorCameraHelper.CameraSourceRef.Hmd:
                    {
                        if (IsMainCameraExist())
                        {
                            Transform mainCameraTransform = SpectatorCameraBased.GetMainCamera().transform;
                            position = mainCameraTransform.position;
                            rotation = mainCameraTransform.rotation;
                        }
                        else
                        {
                            Debug.LogWarning("Main camera does not exist in the scene");
                            return;
                        }
                    }
                        break;
                    case SpectatorCameraHelper.CameraSourceRef.Tracker:
                    {
                        if (!IsFollowTrackerExist())
                        {
                            if (spectatorCameraTrackerList == null)
                            {
                                spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                            }
                            
                            if (SpectatorCameraTrackerList.Count > 0)
                            {
                                // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                                // some trackers in the scene, change to use the first tracker in the list as default
                                FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                            }
                            else
                            {
                                // If there is no tracker in the scene, change to use the HMD as default
                                CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                                return;
                            }
                        }

                        Transform followTrackerTransform = FollowSpectatorCameraTracker.transform;
                        position = followTrackerTransform.position;
                        rotation = followTrackerTransform.rotation;
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CameraLastPosition = position;
                CameraLastRotation = rotation;

                // Init camera prefab position
                SpectatorCamera.transform.position = position;
                // Init camera prefab rotation
                SpectatorCamera.transform.rotation = rotation;
            }

            // Init the camera FOV
            void InitCameraFov()
            {
                float fov;

                switch (CameraSourceRef)
                {
                    case SpectatorCameraHelper.CameraSourceRef.Hmd:
                    {
                        if (IsMainCameraExist())
                        {
                            fov = VerticalFov;
                        }
                        else
                        {
                            Debug.LogWarning("Main camera does not exist in the scene");
                            return;
                        }
                    }
                        break;
                    case SpectatorCameraHelper.CameraSourceRef.Tracker:
                    {
                        if (!IsFollowTrackerExist())
                        {
                            if (spectatorCameraTrackerList == null)
                            {
                                spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                            }
                            
                            if (SpectatorCameraTrackerList.Count > 0)
                            {
                                // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                                // some trackers in the scene, change to use the first tracker in the list as default
                                FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                            }
                            else
                            {
                                // If there is no tracker in the scene, change to use the HMD as default
                                CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                                return;
                            }
                        }

                        fov = FollowSpectatorCameraTracker.VerticalFov;
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                SpectatorCameraBased.SpectatorCamera.fieldOfView = fov;
            }

            // Init the camera layer mask
            void InitCameraLayerMask()
            {
                LayerMask layerMaskValue;

                switch (CameraSourceRef)
                {
                    case SpectatorCameraHelper.CameraSourceRef.Hmd:
                    {
                        if (IsMainCameraExist())
                        {
                            layerMaskValue = SpectatorCameraBased.GetMainCamera().cullingMask;
                        }
                        else
                        {
                            Debug.LogWarning("Main camera does not exist in the scene");
                            return;
                        }
                    }
                        break;
                    case SpectatorCameraHelper.CameraSourceRef.Tracker:
                    {
                        if (!IsFollowTrackerExist())
                        {
                            if (spectatorCameraTrackerList == null)
                            {
                                spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                            }
                            
                            if (SpectatorCameraTrackerList.Count > 0)
                            {
                                // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                                // some trackers in the scene, change to use the first tracker in the list as default
                                FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                            }
                            else
                            {
                                // If there is no tracker in the scene, change to use the HMD as default
                                CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                                return;
                            }
                        }

                        layerMaskValue = FollowSpectatorCameraTracker.LayerMask;
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                SpectatorCameraBased.SpectatorCamera.cullingMask = layerMaskValue;
            }
        }

        private void LateUpdate()
        {
            if (!InitSuccess)
            {
                return;
            }

            Vector3 position;
            Quaternion rotation;
            bool isSmoothCameraMovementValue;
            float smoothCameraMovementSpeedValue;

            switch (CameraSourceRef)
            {
                case SpectatorCameraHelper.CameraSourceRef.Hmd:
                {
                    if (IsMainCameraExist())
                    {
                        Transform mainCameraTransform = SpectatorCameraBased.GetMainCamera().transform;
                        position = mainCameraTransform.position;
                        rotation = mainCameraTransform.rotation;
                        isSmoothCameraMovementValue = IsSmoothCameraMovement;
                        smoothCameraMovementSpeedValue = SmoothCameraMovementSpeed;
                    }
                    else
                    {
                        Debug.LogWarning("Main camera does not exist in the scene");
                        return;
                    }
                }
                    break;
                case SpectatorCameraHelper.CameraSourceRef.Tracker:
                {
                    if (!IsFollowTrackerExist())
                    {
                        if (spectatorCameraTrackerList == null)
                        {
                            spectatorCameraTrackerList = new List<SpectatorCameraTracker>();
                        }
                        
                        if (SpectatorCameraTrackerList.Count > 0)
                        {
                            // If there is no tracker assign to the FollowSpectatorCameraTracker and there are
                            // some trackers in the scene, change to use the first tracker in the list as default
                            FollowSpectatorCameraTracker = SpectatorCameraTrackerList[0];
                        }
                        else
                        {
                            // If there is no tracker in the scene, change to use the HMD as default
                            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;
                            return;
                        }
                    }

                    isSmoothCameraMovementValue = FollowSpectatorCameraTracker.IsSmoothCameraMovement;
                    smoothCameraMovementSpeedValue = FollowSpectatorCameraTracker.SmoothCameraMovementSpeed;

                    Transform trackerTransform = FollowSpectatorCameraTracker.transform;
                    position = trackerTransform.position;
                    rotation = trackerTransform.rotation;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (isSmoothCameraMovementValue)
            {
                position = Vector3.Lerp(CameraLastPosition, position, Time.deltaTime * smoothCameraMovementSpeedValue);

                // To avoid problem:
                // https://discussions.unity.com/t/error-compareapproximately-ascalar-0-0f-with-quaternion-lerp/161461/2
                float t = Mathf.Clamp(Time.deltaTime * smoothCameraMovementSpeedValue, 0f, 1 - Mathf.Epsilon);
                rotation = Quaternion.Lerp(CameraLastRotation, rotation, t);
            }

            CameraLastPosition = position;
            CameraLastRotation = rotation;

            // Set camera prefab position and rotation
            SpectatorCamera.transform.position = position;
            SpectatorCamera.transform.rotation = rotation;
        }

        private void OnDestroy()
        {
            if (!InitSuccess)
            {
                return;
            }

            Debug.Log("OnDestroy");

            if (SpectatorCameraBased)
            {
                SpectatorCameraBased.SpectatorCameraGameObject.transform.SetParent(null);
                SpectatorCameraBased.OnSpectatorStart -= SetupFrustum;
                SpectatorCameraBased.OnSpectatorStart -= SetSpectatorCameraViewMaterial;
                SpectatorCameraBased.OnSpectatorStop -= SetupFrustum;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;

            Destroy(SpectatorCamera);
            _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!InitSuccess)
            {
                return;
            }

            Debug.Log($"OnSceneLoaded: {scene.name}");

            // Reset camera reference source
            CameraSourceRef = SpectatorCameraHelper.CameraSourceRef.Hmd;

            // Reset Tracker and tracker list
            FollowSpectatorCameraTracker = null;
            spectatorCameraTrackerList?.Clear();
        }

        #endregion
    }
}