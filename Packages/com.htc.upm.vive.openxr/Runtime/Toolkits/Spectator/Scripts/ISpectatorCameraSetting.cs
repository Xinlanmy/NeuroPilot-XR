// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
using VIVE.OpenXR.Toolkits.Spectator.Helper;

namespace VIVE.OpenXR.Toolkits.Spectator
{
    /// <summary>
    /// Name: ISpectatorCameraSetting.cs
    /// Role: Contract
    /// Responsibility: Define the setting attribute of the spectator camera.
    /// </summary>
    public interface ISpectatorCameraSetting
    {
        #region Property

        /// <summary>
        /// The struct UnityEngine.LayerMask defines which layer the camera can see or not.
        /// </summary>
        LayerMask LayerMask { get; set; }
        
        /// <summary>
        /// Whether or not to enable the feature of smoothing the spectator camera movement.
        /// </summary>
        bool IsSmoothCameraMovement { get; set; }
        
        /// <summary>
        /// The speed factor to control the smoothing impact.
        /// </summary>
        int SmoothCameraMovementSpeed { get; set; }
        
        /// <summary>
        /// True if visualize the spectator camera vertical FOV.
        /// </summary>
        bool IsFrustumShowed { get; set; }
        
        /// <summary>
        /// The spectator camera vertical FOV.
        /// </summary>
        float VerticalFov { get; set; }
        
        /// <summary>
        /// The panorama image resolution.
        /// </summary>
        SpectatorCameraHelper.SpectatorCameraPanoramaResolution PanoramaResolution { get; set; }
        
        /// <summary>
        /// The panorama image output format.
        /// </summary>
        TextureProcessHelper.PictureOutputFormat PanoramaOutputFormat { get; set; }
        
        /// <summary>
        /// The panorama types.
        /// </summary>
        TextureProcessHelper.PanoramaType PanoramaOutputType { get; set; }
        
        /// <summary>
        /// How many frustum lines will be shown?
        /// </summary>
        SpectatorCameraHelper.FrustumLineCount FrustumLineCount { get; set; }
        
        /// <summary>
        /// How many frustum center lines will be shown?
        /// </summary>
        SpectatorCameraHelper.FrustumCenterLineCount FrustumCenterLineCount { get; set; }
        
        /// <summary>
        /// Frustum line width.
        /// </summary>
        float FrustumLineWidth { get; set; }
        
        /// <summary>
        /// Frustum center line width.
        /// </summary>
        float FrustumCenterLineWidth { get; set; }
        
        /// <summary>
        /// Frustum line color.
        /// </summary>
        Color FrustumLineColor { get; set; }
        
        /// <summary>
        /// Frustum center line color.
        /// </summary>
        Color FrustumCenterLineColor { get; set; }

        #endregion

        #region Function

        /// <summary>
        /// Reset the spectator camera setting to the default value.
        /// </summary>
        void ResetSetting();
        
        /// <summary>
        /// Export the current spectator camera setting as a JSON file and then save it to the resource folder or persistent folder.
        /// </summary>
        /// <param name="attributeFileLocation">The enum SpectatorCameraHelper.AttributeFileLocation.</param>
        void ExportSetting2JsonFile(in SpectatorCameraHelper.AttributeFileLocation attributeFileLocation);
        
        /// <summary>
        /// Load the setting (JSON) file via input full file path.
        /// </summary>
        /// <param name="jsonFilePath">The setting file’s full path (including file name and JSON extension).</param>
        void LoadSettingFromJsonFile(in string jsonFilePath);

        /// <summary>
        /// Load the setting (JSON) file via input scene name, GameObject (hmd) name, and the file location (resource folder or persistent folder).
        /// </summary>
        /// <param name="sceneName">The scene name.</param>
        /// <param name="gameObjectName">The GameObject name.</param>
        /// <param name="attributeFileLocation"> The enum SpectatorCameraHelper.AttributeFileLocation.</param>
        void LoadSettingFromJsonFile(
            in string sceneName,
            in string gameObjectName,
            in SpectatorCameraHelper.AttributeFileLocation attributeFileLocation);

        /// <summary>
        /// Apply the spectator camera setting to the current component.
        /// </summary>
        /// <param name="data">The data you want to apply.</param>
        void ApplyData(in SpectatorCameraHelper.SpectatorCameraAttribute data);

        #endregion
    }
}