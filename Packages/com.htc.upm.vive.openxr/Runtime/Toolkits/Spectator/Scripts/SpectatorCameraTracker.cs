// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VIVE.OpenXR.Toolkits.Spectator.Helper;

namespace VIVE.OpenXR.Toolkits.Spectator
{
    /// <summary>
    /// Name: SpectatorCameraTracker.cs
    /// Role: General script
    /// Responsibility: To implement the spectator camera tracker setting and I/O function
    /// </summary>
    public partial class SpectatorCameraTracker : MonoBehaviour, ISpectatorCameraSetting
    {
        private const int TryAddTrackerIntervalSecond = 1;
        
        private SpectatorCameraManager SpectatorCameraManager => SpectatorCameraManager.Instance;
        public SpectatorCameraHelper.CameraSourceRef CameraSourceRef => SpectatorCameraHelper.CameraSourceRef.Tracker;

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SpectatorCameraBased.SpectatorCamera.cullingMask = layerMask;
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustum();
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

                verticalFov = Mathf.Clamp(
                    value,
                    SpectatorCameraHelper.VERTICAL_FOV_MIN,
                    SpectatorCameraHelper.VERTICAL_FOV_MAX);

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SpectatorCameraBased.SpectatorCamera.fieldOfView = verticalFov;
                    SpectatorCameraManager.SetupFrustum();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumLine();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumCenterLine();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumLine();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumCenterLine();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumLine();
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

                if (Application.isPlaying &&
                    SpectatorCameraManager != null &&
                    SpectatorCameraManager.IsCameraSourceAsTracker() &&
                    SpectatorCameraManager.IsFollowTrackerEqualTo(this))
                {
                    SpectatorCameraManager.SetupFrustumCenterLine();
                }
            }
        }

        public void ResetSetting()
        {
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
                Position,
                Rotation,
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
                Debug.Log($"Load setting from {jsonFilePath} file to scene gameObject {gameObject.name} failed.");
            }
        }

        public void LoadSettingFromJsonFile(
            in string sceneName,
            in string trackerName,
            in SpectatorCameraHelper.AttributeFileLocation attributeFileLocation)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(trackerName))
            {
                Debug.LogError("sceneName or trackerName is null or empty");
                return;
            }

            var loadSuccess = false;
            SpectatorCameraHelper.SpectatorCameraAttribute data = new SpectatorCameraHelper.SpectatorCameraAttribute();
            if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.ResourceFolder)
            {
                loadSuccess = SpectatorCameraHelper.LoadAttributeFileFromResourcesFolder(
                    sceneName,
                    trackerName,
                    out data);
            }
            else if (attributeFileLocation is SpectatorCameraHelper.AttributeFileLocation.PersistentFolder)
            {
                loadSuccess = SpectatorCameraHelper.LoadAttributeFileFromPersistentFolder(
                    sceneName,
                    trackerName,
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
                    SpectatorCameraHelper.GetSpectatorCameraAttributeFileNamePattern(sceneName, trackerName);

                Debug.Log(
                    $"Load setting from {fileDirectory}/{fileName} file to scene gameObject {gameObject.name} failed.");
            }
        }

        public void ApplyData(in SpectatorCameraHelper.SpectatorCameraAttribute data)
        {
            Transform gameObjectTransform = transform;
            gameObjectTransform.position = data.position;
            gameObjectTransform.rotation = data.rotation;

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

        private void Start()
        {
            if (SpectatorCameraManager != null)
            {
                SpectatorCameraManager.AddSpectatorCameraTracker(this);
            }
            else
            {
                InvokeRepeating(nameof(AddTracker), TryAddTrackerIntervalSecond, TryAddTrackerIntervalSecond);
            }
        }

        private void OnEnable()
        {
            if (SpectatorCameraManager != null)
            {
                SpectatorCameraManager.AddSpectatorCameraTracker(this);
            }
        }

        private void OnDisable()
        {
            if (SpectatorCameraManager != null)
            {
                SpectatorCameraManager.RemoveSpectatorCameraTracker(this);
            }
        }
        
        private void AddTracker()
        {
            if (SpectatorCameraManager != null)
            {
                SpectatorCameraManager.AddSpectatorCameraTracker(this);
                CancelInvoke(nameof(AddTracker));
            }
        }
    }
}