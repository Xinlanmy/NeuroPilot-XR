// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Passthrough;

namespace VIVE.OpenXR
{
    public class XR_HTC_passthrough_impls : XR_HTC_passthrough_defs
    {
        const string LOG_TAG = "VIVE.OpenXR.XR_HTC_passthrough_impls";
        void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
        public XR_HTC_passthrough_impls() { DEBUG("XR_HTC_passthrough_impls()"); }
        private VivePassthrough feature = null;

        private void ASSERT_FEATURE()
        {
            if (feature == null) { feature = OpenXRSettings.Instance.GetFeature<VivePassthrough>(); }
        }

        public override XrResult xrCreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough)
        {
            XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;
            passthrough = 0;
            ASSERT_FEATURE();

            if(feature)
                result =  feature.CreatePassthroughHTC(createInfo,out passthrough);

            return result;
        }
        public override XrResult xrDestroyPassthroughHTC(XrPassthroughHTC passthrough)
        {
            XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;
            ASSERT_FEATURE();

            if(feature)
                result =  feature.DestroyPassthroughHTC(passthrough);

            return result;
        }

        public override XrSpace GetTrackingSpace()
        {
            ASSERT_FEATURE();

            if (feature)
                return feature.GetTrackingSpace();

            return 0;
        }

        public override XrFrameState GetFrameState()
        {
            ASSERT_FEATURE();
            if (feature)
                return feature.GetFrameState();
            return new XrFrameState();
        }
    }
}

