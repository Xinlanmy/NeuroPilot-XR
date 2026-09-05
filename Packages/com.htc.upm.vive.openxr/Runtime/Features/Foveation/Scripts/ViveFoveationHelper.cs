// Copyright HTC Corporation All Rights Reserved.

namespace VIVE.OpenXR.Foveation
{
    #region 12.86. XR_HTC_foveation
    /// <summary>
    /// The XrFoveationModeHTC identifies the different foveation modes.
    /// </summary>
    public enum XrFoveationModeHTC
    {
        XR_FOVEATION_MODE_DISABLE_HTC = 0,
        XR_FOVEATION_MODE_FIXED_HTC = 1,
        XR_FOVEATION_MODE_DYNAMIC_HTC = 2,
        XR_FOVEATION_MODE_CUSTOM_HTC = 3,
        XR_FOVEATION_MODE_MAX_ENUM_HTC = 0x7FFFFFFF
    }
    /// <summary>
    /// The XrFoveationLevelHTC identifies the pixel density drop level of periphery area.
    /// </summary>
    public enum XrFoveationLevelHTC
    {
        XR_FOVEATION_LEVEL_NONE_HTC = 0,
        XR_FOVEATION_LEVEL_LOW_HTC = 1,
        XR_FOVEATION_LEVEL_MEDIUM_HTC = 2,
        XR_FOVEATION_LEVEL_HIGH_HTC = 3,
        XR_FOVEATION_LEVEL_MAX_ENUM_HTC = 0x7FFFFFFF
    }
    /// <summary>
    /// The XrFoveationConfigurationHTC structure contains the custom foveation settings for the corresponding views.
    /// </summary>
    public struct XrFoveationConfigurationHTC
    {
        public XrFoveationLevelHTC level;
        public float clearFovDegree;
        public XrVector2f focalCenterOffset;
    }
    #endregion
}