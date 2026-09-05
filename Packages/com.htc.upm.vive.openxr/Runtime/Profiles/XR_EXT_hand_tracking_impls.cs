// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR;

using VIVE.OpenXR.Hand;

namespace VIVE.OpenXR
{
    public class XR_EXT_hand_tracking_impls : XR_EXT_hand_tracking_defs
    {
        #region Log
        const string LOG_TAG = "VIVE.OpenXR.XR_EXT_hand_tracking_impls";
        StringBuilder m_sb = null;
        StringBuilder sb
        {
            get
            {
                if (m_sb == null) { m_sb = new StringBuilder(); }
                return m_sb;
            }
        }
        void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
        #endregion

        public XR_EXT_hand_tracking_impls() { sb.Clear().Append("XR_EXT_hand_tracking_impls()"); DEBUG(sb); }

        private ViveHandTracking feature = null;
        private bool ASSERT_FEATURE(bool init = false)
        {
            if (feature == null) { feature = OpenXRSettings.Instance.GetFeature<ViveHandTracking>(); }
            bool enabled = (feature != null);
            if (init)
            {
                sb.Clear().Append("ViveHandTracking is ").Append((enabled ? "enabled." : "disabled."));
                DEBUG(sb);
            }
            return enabled;
        }

        public override XrResult xrCreateHandTrackerEXT(ref XrHandTrackerCreateInfoEXT createInfo, out ulong handTracker)
        {
            XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;
            handTracker = 0;

            if (ASSERT_FEATURE(true))
            {
                sb.Clear().Append("xrCreateHandTrackerEXT"); DEBUG(sb);
                XrHandTrackerCreateInfoEXT info = createInfo;
                result = (XrResult)feature.CreateHandTrackerEXT(ref info, out XrHandTrackerEXT tracker);
                if (result == XrResult.XR_SUCCESS) { handTracker = tracker; }
            }

            return result;
        }
        public override XrResult xrDestroyHandTrackerEXT(ulong handTracker)
        {
            if (ASSERT_FEATURE(true))
            {
                sb.Clear().Append("xrDestroyHandTrackerEXT"); DEBUG(sb);
                return (XrResult)feature.DestroyHandTrackerEXT(handTracker);
            }

            return XrResult.XR_ERROR_VALIDATION_FAILURE;
        }
        public override XrResult xrLocateHandJointsEXT(ulong handTracker, XrHandJointsLocateInfoEXT locateInfo, out XrHandJointLocationsEXT locations)
        {
            XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;

            InitializeHandJointLocations();
            locations = m_JointLocations;

            if (ASSERT_FEATURE())
            {
                XrHandJointLocationsEXT joints = m_JointLocations;
                result = (XrResult)feature.LocateHandJointsEXT(handTracker, locateInfo, ref joints);
                if (result == XrResult.XR_SUCCESS) { locations = joints; }
            }

            return result;
        }

        public override bool GetJointLocations(bool isLeft, out XrHandJointLocationEXT[] handJointLocation, out XrTime timestamp)
        {
            if (ASSERT_FEATURE())
            {
                if (feature.GetJointLocations(isLeft, out handJointLocation, out timestamp))
                {
                    return true;
                }
            }

            handJointLocation = s_JointLocation[isLeft];
            timestamp = 0;
            return false;
        }
        public override bool GetJointLocations(bool isLeft, out XrHandJointLocationEXT[] handJointLocation)
        {
            return GetJointLocations(isLeft, out handJointLocation, out XrTime timestamp);
        }
    }
}
