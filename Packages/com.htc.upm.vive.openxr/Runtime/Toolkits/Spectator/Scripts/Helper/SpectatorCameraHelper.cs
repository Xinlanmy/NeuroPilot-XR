// Copyright HTC Corporation All Rights Reserved.

using System.IO;
using UnityEngine;

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public class SpectatorCameraHelper
    {
        # region Attribute value range

        public const float VERTICAL_FOV_MIN = 10f;
        public const float VERTICAL_FOV_MAX = 130f;

        public const float FRUSTUM_LINE_WIDTH_MIN = .02f;
        public const float FRUSTUM_LINE_WIDTH_MAX = .05f;

        public const float FRUSTUM_CENTER_LINE_WIDTH_MIN = .01f;
        public const float FRUSTUM_CENTER_LINE_WIDTH_MAX = .04f;

        public const int PANORAMA_RESOLUTION_MIN = 512;
        public const int PANORAMA_RESOLUTION_MAX = 4096;

        public const int SMOOTH_CAMERA_MOVEMENT_MIN = 8;
        public const int SMOOTH_CAMERA_MOVEMENT_MAX = 16;

        public const float COMPARE_FLOAT_SUPER_SMALL_THRESHOLD = .001f;
        public const float COMPARE_FLOAT_EXTRA_SMALL_THRESHOLD = .01f;
        public const float COMPARE_FLOAT_SMALL_THRESHOLD = .1f;
        public const float COMPARE_FLOAT_MEDIUM_THRESHOLD = 1f;
        public const float COMPARE_FLOAT_LARGE_THRESHOLD = 10f;

        public const float INTERVAL_SECOND_GET_SPECTATOR_HANDLER = .5f;
        public const float MAX_SECOND_GET_SPECTATOR_HANDLER = 5f;

        public const float INTERVAL_SECOND_INTERNAL_SPECTATOR_CAMERA = 1f;
        public const float MAX_SECOND_GET_INTERNAL_SPECTATOR_CAMERA = 2f;

        # endregion

        public static readonly Vector3 SpectatorCameraSpherePrefabScaleDefault = new Vector3(.08f, .08f, .08f);

        #region Attribute default value definition

        public const int TEXTURE_WIDTH = 1920;
        public const int TEXTURE_HEIGHT = 1080;

        public const CameraSourceRef CAMERA_SOURCE_REF_DEFAULT = CameraSourceRef.Hmd;
        public static readonly Vector3 PositionDefault = new Vector3(0f, 1.7f, 0f);
        public static readonly Quaternion RotationDefault = Quaternion.identity;
        public static readonly LayerMask LayerMaskDefault = -1;

        public const bool IS_SMOOTH_CAMERA_MOVEMENT_DEFAULT = true;
        public const int SMOOTH_CAMERA_MOVEMENT_SPEED_DEFAULT = 10;

        public const bool IS_FRUSTUM_SHOWED_DEFAULT = false;

        public const float VERTICAL_FOV_DEFAULT = 60f;

        public const SpectatorCameraPanoramaResolution PANORAMA_RESOLUTION_DEFAULT =
            SpectatorCameraPanoramaResolution._2048;

        public const TextureProcessHelper.PictureOutputFormat PANORAMA_OUTPUT_FORMAT_DEFAULT =
            TextureProcessHelper.PictureOutputFormat.PNG;

        public const TextureProcessHelper.PanoramaType PANORAMA_TYPE_DEFAULT =
            TextureProcessHelper.PanoramaType.Monoscopic;

        public const float STEREO_SEPARATION_DEFAULT = 0.065f;

        public const FrustumLineCount FRUSTUM_LINE_COUNT_DEFAULT =
            FrustumLineCount.Four;

        public const FrustumCenterLineCount FRUSTUM_CENTER_LINE_COUNT_DEFAULT =
            FrustumCenterLineCount.Center;

        public const float FRUSTUM_LINE_WIDTH_DEFAULT = .02f;
        public const float FRUSTUM_CENTER_LINE_WIDTH_DEFAULT = .01f;

        public const float FRUSTUM_LINE_BEGIN_DEFAULT = .3f;

        // Define by Unity: https://docs.unity3d.com/ScriptReference/Camera.CalculateFrustumCorners.html
        public const int FRUSTUM_OUT_CORNERS_COUNT = 4;

        public static readonly Color LineColorDefault = Color.white;
        public const string LINE_SHADER_NAME_DEFAULT = "UI/Default";

#if UNITY_EDITOR
        public const bool IS_DEBUG_SPECTATOR_CAMERA = true;
#endif

        #endregion

        #region Frustum game object name definition

        public const string FRUSTUM_LINE_ROOT_NAME_DEFAULT = "FrustumLines";
        public const string FRUSTUM_LINE_NAME_PREFIX_DEFAULT = "Frustum";
        public const string FRUSTUM_CENTER_LINE_ROOT_NAME_DEFAULT = "FrustumCenterLines";
        public const string FRUSTUM_CENTER_LINE_NAME_PREFIX_DEFAULT = "FrustumCenter";

        #endregion

        #region Save file definition

        public const string SAVE_PHOTO360_ALBUM_NAME = "Screenshots";

        public const string ATTRIBUTE_FILE_PREFIX_NAME = "SpectatorCameraAttribute";

        public const string ATTRIBUTE_FILE_EXTENSION = "json";

        #endregion

        /// <summary>
        /// Load the attribute file from resource folder
        /// </summary>
        /// <param name="sceneName">The corresponding scene name of the attribute file</param>
        /// <param name="gameObjectName">The corresponding gameObject name of the attribute file</param>
        /// <param name="data">The attribute data which save in attribute file</param>
        /// <returns>True if get the attribute data successfully. Otherwise, return false.</returns>
        public static bool LoadAttributeFileFromResourcesFolder(
            in string sceneName,
            in string gameObjectName,
            out SpectatorCameraAttribute data)
        {
            Debug.Log("Get spectator camera attribute at resources folder");

            data = new SpectatorCameraAttribute();
            TextAsset json;

            // Name format: {PREFIX}_{SCENE NAME}_{GAME OBJECT NAME}.json
            var fileName = GetSpectatorCameraAttributeFileNamePattern(sceneName, gameObjectName, false);

            try
            {
                json = Resources.Load<TextAsset>(fileName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Get attribute data from resources folder fail:");
                Debug.LogWarning($"{e}");
                return false;
            }

            if (json == null)
            {
                Debug.LogWarning("The attribute data at resources folder is empty or null");
                return false;
            }

            Debug.Log($"The attribute value is: {json.text}");
            data = JsonUtility.FromJson<SpectatorCameraAttribute>(json.text);

            if (data == null)
            {
                Debug.LogWarning("Convert attribute data from resources folder fail");
                return false;
            }

            Debug.Log("Get attribute data from resources folder successful");
            return true;
        }

        /// <summary>
        /// Load the attribute file from full path
        /// </summary>
        /// <param name="fullPathWithFileNameAndExtension">Full path</param>
        /// <param name="data">The attribute data which save in persistent folder</param>
        /// <returns>True if get the attribute data successfully. Otherwise, return false.</returns>
        public static bool LoadAttributeFileFromFolder(
            string fullPathWithFileNameAndExtension,
            out SpectatorCameraAttribute data)
        {
            Debug.Log($"Get spectator camera attribute at {fullPathWithFileNameAndExtension}");

            return LoadAttributeData(fullPathWithFileNameAndExtension, out data);
        }

        /// <summary>
        /// Load the attribute file from persistent folder
        /// </summary>
        /// <param name="sceneName">The corresponding scene name of the attribute file</param>
        /// <param name="gameObjectName">The corresponding gameObject name of the attribute file</param>
        /// <param name="data">The attribute data which save in persistent folder</param>
        /// <returns>True if get the attribute data successfully. Otherwise, return false.</returns>
        public static bool LoadAttributeFileFromPersistentFolder(
            in string sceneName,
            in string gameObjectName,
            out SpectatorCameraAttribute data)
        {
            // Name format: {PREFIX}_{SCENE NAME}_{GAME OBJECT NAME}.json
            var fileNameWithExtension = GetSpectatorCameraAttributeFileNamePattern(sceneName, gameObjectName);

            var attributeFileDirectoryAndFileNameWithExtension =
                Path.Combine(Application.persistentDataPath, fileNameWithExtension);

            Debug.Log($"Get spectator camera attribute at {attributeFileDirectoryAndFileNameWithExtension}");

            return LoadAttributeData(attributeFileDirectoryAndFileNameWithExtension, out data);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Save the spectator camera attribute as a JSON file to the resources folder located in the Unity project's assets folder.
        /// </summary>
        public static void SaveAttributeData2ResourcesFolder(
            in string sceneName,
            in string gameObjectName,
            in SpectatorCameraAttribute data)
        {
            // Name format: {PREFIX}_{SCENE NAME}_{GAME OBJECT NAME}.json
            var fileNameWithExtension = GetSpectatorCameraAttributeFileNamePattern(sceneName, gameObjectName);
            string resourcesFolderPath = Path.Combine(Application.dataPath, "Resources");
            SaveAttributeData(resourcesFolderPath, fileNameWithExtension, data);
        }
#endif

        /// <summary>
        /// Save the spectator camera attribute as a JSON file to a persistent folder.
        /// </summary>
        public static void SaveAttributeData2PersistentFolder(
            in string sceneName,
            in string gameObjectName,
            in SpectatorCameraAttribute data)
        {
            // Name format: {PREFIX}_{SCENE NAME}_{GAME OBJECT NAME}.json
            var fileNameWithExtension = GetSpectatorCameraAttributeFileNamePattern(sceneName, gameObjectName);

            SaveAttributeData(Application.persistentDataPath, fileNameWithExtension, data);
        }

        /// <summary>
        /// Load the spectator camera attribute as a JSON file on disk.
        /// </summary>
        /// <param name="fullPathWithFileNameAndExtension">The path (string) that include directory, file name and extension</param>
        /// <param name="data">The data of spectator camera attribute</param>
        /// <returns>True if get the attribute data successfully. Otherwise, return false.</returns>
        private static bool LoadAttributeData(
            string fullPathWithFileNameAndExtension,
            out SpectatorCameraAttribute data)
        {
            data = new SpectatorCameraAttribute();
            string json;
            try
            {
                json = File.ReadAllText(fullPathWithFileNameAndExtension);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Get attribute data from {fullPathWithFileNameAndExtension} failed: {e}");
                return false;
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"The attribute data at {fullPathWithFileNameAndExtension} is empty or null");
                return false;
            }

            Debug.Log($"The attribute value is: {json}");
            data = JsonUtility.FromJson<SpectatorCameraAttribute>(json);

            if (data == null)
            {
                Debug.LogWarning($"Convert attribute data from {fullPathWithFileNameAndExtension} failed");
                return false;
            }

            Debug.Log($"Get attribute data from {fullPathWithFileNameAndExtension} successful");
            return true;
        }

        /// <summary>
        /// Save the spectator camera attribute as a JSON file on disk.
        /// </summary>
        /// <param name="saveFileDirectory">The directory that the spectator camera attribute (JSON file) will save to</param>
        /// <param name="saveFileNameWithExtension">The file name of the spectator camera attribute (JSON file)</param>
        /// <param name="data">The data of spectator camera attribute</param>
        private static void SaveAttributeData(
            in string saveFileDirectory,
            in string saveFileNameWithExtension,
            in SpectatorCameraAttribute data)
        {
            if (string.IsNullOrEmpty(saveFileDirectory) || string.IsNullOrEmpty(saveFileNameWithExtension))
            {
                Debug.LogError("The saving file directory or name is null or empty");
                return;
            }

            string fullPath = Path.Combine(saveFileDirectory, saveFileNameWithExtension);

            // Convert to string format
            string json = JsonUtility.ToJson(data);

            // Make sure the file path is exist
            if (!Directory.Exists(saveFileDirectory))
            {
                Directory.CreateDirectory(saveFileDirectory);
            }

            File.WriteAllText(fullPath, json);

            Debug.Log($"The configuration save at {fullPath}");
        }

        public static string GetSpectatorCameraAttributeFileNamePattern(
            in string sceneName,
            in string gameObjectName,
            bool withExtension = true)
        {
            var fileNamePattern = $"{ATTRIBUTE_FILE_PREFIX_NAME}_{sceneName}_{gameObjectName}";
            if (withExtension)
            {
                fileNamePattern += $".{ATTRIBUTE_FILE_EXTENSION}";
            }

            return fileNamePattern;
        }

        /// <summary>
        /// Check the panorama resolution is power of two or not. If not, convert to power of two.
        /// </summary>
        /// <param name="inputResolution">The panorama resolution</param>
        /// <returns></returns>
        public static int CheckAndConvertPanoramaResolution(in int inputResolution)
        {
            int result;

            // Check is power of two
            if ((inputResolution != 0) && ((inputResolution & (inputResolution - 1)) == 0))
            {
                result = Mathf.Clamp(inputResolution, PANORAMA_RESOLUTION_MIN, PANORAMA_RESOLUTION_MAX);
            }
            else
            {
                // If not power of two, convert to power of two
                int clampInputValue = Mathf.Clamp(inputResolution, PANORAMA_RESOLUTION_MIN, PANORAMA_RESOLUTION_MAX);
                var base2LogarithmOfClampInputValue = (int)System.Math.Log(clampInputValue, 2);

                var base2LogarithmOfClampInputValueLowerStepValue =
                    (int)System.Math.Pow(2, base2LogarithmOfClampInputValue);
                var base2LogarithmOfClampInputValueUpperStepValue =
                    (int)System.Math.Pow(2, base2LogarithmOfClampInputValue + 1);

                // Check which one is closer
                if (clampInputValue - base2LogarithmOfClampInputValueLowerStepValue <=
                    base2LogarithmOfClampInputValueUpperStepValue - clampInputValue)
                {
                    result = base2LogarithmOfClampInputValueLowerStepValue;
                }
                else
                {
                    result = base2LogarithmOfClampInputValueUpperStepValue;
                }
            }

            return result;
        }

        #region Functions of changing camera culling mask

        /// <summary>
        /// Set the layer that the camera can watch
        /// </summary>
        /// <param name="targetCullingMask">The camera culling mask</param>
        /// <param name="shownLayer">The layer number that you want to set the camera can watch</param>
        /// <returns>The new camera culling mask after modify</returns>
        public static LayerMask SetCameraVisualizationLayer(in LayerMask targetCullingMask, in int shownLayer)
        {
            LayerMask targetLayerMaskAfterModify = targetCullingMask;
            targetLayerMaskAfterModify |= 1 << shownLayer;
            return targetLayerMaskAfterModify;
        }

        /// <summary>
        /// Set the layer that the camera cannot watch
        /// </summary>
        /// <param name="targetCullingMask">The camera culling mask</param>
        /// <param name="hiddenLayer">The layer number that you want to set the camera cannot watch</param>
        /// <returns>The new camera culling mask after modify</returns>
        public static LayerMask SetCameraHiddenLayer(in LayerMask targetCullingMask, in int hiddenLayer)
        {
            LayerMask targetLayerMaskAfterModify = targetCullingMask;
            targetLayerMaskAfterModify &= ~(1 << hiddenLayer);
            return targetLayerMaskAfterModify;
        }

        /// <summary>
        /// Inverse specific layer in camera culling mask
        /// </summary>
        /// <param name="targetCullingMask">The camera culling mask</param>
        /// <param name="inverseLayer">The layer number that you want to inverse</param>
        /// <returns>The new camera culling mask after modify</returns>
        public static LayerMask InverseCameraLayer(in LayerMask targetCullingMask, in int inverseLayer)
        {
            LayerMask targetLayerMaskAfterModify = targetCullingMask;
            targetLayerMaskAfterModify ^= 1 << inverseLayer;
            return targetLayerMaskAfterModify;
        }

        #endregion

        [System.Serializable]
        public class SpectatorCameraAttribute
        {
            #region Serializable class field

            public CameraSourceRef source;
            public Vector3 position;
            public Quaternion rotation;
            public LayerMask layerMask;
            public bool isSmoothCameraMovement;
            public int smoothCameraMovementSpeed;
            public bool isFrustumShowed;
            public float verticalFov;
            public SpectatorCameraPanoramaResolution panoramaResolution;
            public TextureProcessHelper.PictureOutputFormat panoramaOutputFormat;
            public TextureProcessHelper.PanoramaType panoramaOutputType;
            public FrustumLineCount frustumLineCount;
            public FrustumCenterLineCount frustumCenterLineCount;
            public float frustumLineWidth;
            public float frustumCenterLineWidth;
            public Color frustumLineColor;
            public Color frustumCenterLineColor;

            #endregion

            #region Constructor

            public SpectatorCameraAttribute()
            {
            }

            public SpectatorCameraAttribute(
                CameraSourceRef source,
                Vector3 position,
                Quaternion rotation,
                LayerMask layerMask,
                bool isSmoothCameraMovement,
                int smoothCameraMovementSpeed,
                bool isFrustumShowed,
                float verticalFov,
                SpectatorCameraPanoramaResolution panoramaResolution,
                TextureProcessHelper.PictureOutputFormat panoramaOutputFormat,
                TextureProcessHelper.PanoramaType panoramaOutputType,
                FrustumLineCount frustumLineCount,
                FrustumCenterLineCount frustumCenterLineCount,
                float frustumLineWidth,
                float frustumCenterLineWidth,
                Color frustumLineColor,
                Color frustumCenterLineColor)
            {
                this.source = source;
                this.position = position;
                this.rotation = rotation;
                this.layerMask = layerMask;
                this.isSmoothCameraMovement = isSmoothCameraMovement;
                this.smoothCameraMovementSpeed = smoothCameraMovementSpeed;
                this.isFrustumShowed = isFrustumShowed;
                this.verticalFov = verticalFov;
                this.panoramaResolution = panoramaResolution;
                this.panoramaOutputFormat = panoramaOutputFormat;
                this.panoramaOutputType = panoramaOutputType;
                this.frustumLineCount = frustumLineCount;
                this.frustumCenterLineCount = frustumCenterLineCount;
                this.frustumLineWidth = frustumLineWidth;
                this.frustumCenterLineWidth = frustumCenterLineWidth;
                this.frustumLineColor = frustumLineColor;
                this.frustumCenterLineColor = frustumCenterLineColor;
            }

            #endregion
        }

        #region Enum definition

        public enum CameraSourceRef
        {
            Hmd,
            Tracker
        }

        public enum FrustumLineCount
        {
            None = 0,
            Four = 4,
            Eight = 8,
            Sixteen = 16,
            ThirtyTwo = 32,
            OneTwentyEight = 128,
        }

        public enum FrustumCenterLineCount
        {
            None = 0,
            Center = 1,
            RuleOfThirds = 4,
            CenterAndRuleOfThirds = 5,
        }

        public enum SpectatorCameraPanoramaResolution
        {
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096
        }

        public enum AttributeFileLocation
        {
            ResourceFolder,
            PersistentFolder
        }

        #endregion
    }
}