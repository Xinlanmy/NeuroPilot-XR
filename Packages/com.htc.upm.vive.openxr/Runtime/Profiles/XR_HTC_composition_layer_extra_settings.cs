// Copyright HTC Corporation All Rights Reserved.

using VIVE.OpenXR.CompositionLayer;

namespace VIVE.OpenXR
{
    public class XR_HTC_composition_layer_extra_settings_defs
    {
        /// <summary>
        /// Enable the sharpening setting to the projection layer.
        /// </summary>
        /// <param name="sharpeningMode">The sharpening mode in <see cref="XrSharpeningModeHTC"/>.</param>
        /// <param name="sharpeningLevel">The sharpening level in float [0, 1].</param>
        /// <returns>True for success.</returns>
        public virtual bool EnableSharpening(XrSharpeningModeHTC sharpeningMode, float sharpeningLevel)
		{
            return false;
		}

        /// <summary>
        /// Disable the sharpening setting on the projection layer.
        /// </summary>
        /// <returns>True for success</returns>
        public virtual bool DisableSharpening()
        {
            return false;
        }
    }

    public static class XR_HTC_composition_layer_extra_settings
    {
        static XR_HTC_composition_layer_extra_settings_defs m_Instance = null;
        public static XR_HTC_composition_layer_extra_settings_defs Interop
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new XR_HTC_composition_layer_extra_settings_impls();
                }
                return m_Instance;
            }
        }
    }
}
