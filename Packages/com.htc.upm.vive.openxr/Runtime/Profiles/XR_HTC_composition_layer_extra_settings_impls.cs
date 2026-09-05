// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
using UnityEngine.XR.OpenXR;

using VIVE.OpenXR.CompositionLayer;

namespace VIVE.OpenXR
{
    public class XR_HTC_composition_layer_extra_settings_impls : XR_HTC_composition_layer_extra_settings_defs
    {
        const string LOG_TAG = "VIVE.OpenXR.XR_HTC_composition_layer_extra_settings_impls";
        void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
        public XR_HTC_composition_layer_extra_settings_impls() { DEBUG("XR_HTC_composition_layer_extra_settings_impls()"); }
        private ViveCompositionLayerExtraSettings feature = null;

        private void ASSERT_FEATURE()
        {
            if (feature == null) { feature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayerExtraSettings>(); }
        }

        public override bool EnableSharpening(XrSharpeningModeHTC sharpeningMode, float sharpeningLevel)
        {
            DEBUG("xrViveCompositionLayer_EnableSharpening");
            ASSERT_FEATURE();
            if (feature)
            {
                return feature.EnableSharpening(sharpeningMode, sharpeningLevel);
            }
            return false;
        }

        public override bool DisableSharpening()
        {
            DEBUG("xrViveCompositionLayer_EnableSharpening");
            ASSERT_FEATURE();
            if (feature)
            {
                return feature.DisableSharpening();
            }
            return false;
        }
    }
}
