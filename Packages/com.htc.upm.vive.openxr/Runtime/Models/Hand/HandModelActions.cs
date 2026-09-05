// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using VIVE.OpenXR.Hand;

namespace VIVE.OpenXR.Models
{
    public class HandModelActions : MonoBehaviour
    {
        #region Log
        const string LOG_TAG = "VIVE.OpenXR.Models.HandModelActions";
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
        void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
        #endregion

        #region Inspector
        [SerializeField] private bool m_IsLeft = false;

        [SerializeField] private GameObject m_Wrist = null;
        [SerializeField] private GameObject m_Palm = null;

        [SerializeField] private GameObject m_Thumb_Metacarpal = null;
        [SerializeField] private GameObject m_Thumb_Proximal = null;
        [SerializeField] private GameObject m_Thumb_Distal = null;
        [SerializeField] private GameObject m_Thumb_Tip = null;

        [SerializeField] private GameObject m_Index_Metacarpal = null;
        [SerializeField] private GameObject m_Index_Proximal = null;
        [SerializeField] private GameObject m_Index_Intermediate = null;
        [SerializeField] private GameObject m_Index_Distal = null;
        [SerializeField] private GameObject m_Index_Tip = null;

        [SerializeField] private GameObject m_Middle_Metacarpal = null;
        [SerializeField] private GameObject m_Middle_Proximal = null;
        [SerializeField] private GameObject m_Middle_Intermediate = null;
        [SerializeField] private GameObject m_Middle_Distal = null;
        [SerializeField] private GameObject m_Middle_Tip = null;

        [SerializeField] private GameObject m_Ring_Metacarpal = null;
        [SerializeField] private GameObject m_Ring_Proximal = null;
        [SerializeField] private GameObject m_Ring_Intermediate = null;
        [SerializeField] private GameObject m_Ring_Distal = null;
        [SerializeField] private GameObject m_Ring_Tip = null;

        [SerializeField] private GameObject m_Little_Metacarpal = null;
        [SerializeField] private GameObject m_Little_Proximal = null;
        [SerializeField] private GameObject m_Little_Intermediate = null;
        [SerializeField] private GameObject m_Little_Distal = null;
        [SerializeField] private GameObject m_Little_Tip = null;

        [HideInInspector]
        public bool ForceHidden = false;
        #endregion

        private SkinnedMeshRenderer skinMeshRenderer = null;
        private void Awake()
        {
            skinMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        private void OnEnable()
        {
            Application.onBeforeRender += BeforeRenderUpdate;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= BeforeRenderUpdate;
        }

        private void Update()
        {
            UpdateHand();
        }

        private void BeforeRenderUpdate()
        {
            if (!skinMeshRenderer.enabled) { return; }
            UpdateHand();
        }

        private void UpdateHand()
        {
            Profiler.BeginSample("HandModelActions");
            if (skinMeshRenderer == null) { Profiler.EndSample(); return; }

            Profiler.BeginSample("GetJointLocations");
            if (!XR_EXT_hand_tracking.Interop.GetJointLocations(m_IsLeft, out XrHandJointLocationEXT[] handJointLocation))
            {
                skinMeshRenderer.enabled = false;
                Profiler.EndSample();
                Profiler.EndSample();
                return;
            }
            Profiler.EndSample();

            Profiler.BeginSample("UpdateJointPose");
            skinMeshRenderer.enabled = !ForceHidden;

            UpdateJointPosition(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_WRIST_EXT], ref m_Wrist);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_WRIST_EXT], ref m_Wrist);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_PALM_EXT], ref m_Palm);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_THUMB_METACARPAL_EXT], ref m_Thumb_Metacarpal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_THUMB_PROXIMAL_EXT], ref m_Thumb_Proximal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_THUMB_DISTAL_EXT], ref m_Thumb_Distal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_THUMB_TIP_EXT], ref m_Thumb_Tip);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_INDEX_METACARPAL_EXT], ref m_Index_Metacarpal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_INDEX_PROXIMAL_EXT], ref m_Index_Proximal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_INDEX_INTERMEDIATE_EXT], ref m_Index_Intermediate);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_INDEX_DISTAL_EXT], ref m_Index_Distal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_INDEX_TIP_EXT], ref m_Index_Tip);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_MIDDLE_METACARPAL_EXT], ref m_Middle_Metacarpal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_MIDDLE_PROXIMAL_EXT], ref m_Middle_Proximal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_MIDDLE_INTERMEDIATE_EXT], ref m_Middle_Intermediate);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_MIDDLE_DISTAL_EXT], ref m_Middle_Distal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_MIDDLE_TIP_EXT], ref m_Middle_Tip);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_RING_METACARPAL_EXT], ref m_Ring_Metacarpal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_RING_PROXIMAL_EXT], ref m_Ring_Proximal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_RING_INTERMEDIATE_EXT], ref m_Ring_Intermediate);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_RING_DISTAL_EXT], ref m_Ring_Distal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_RING_TIP_EXT], ref m_Ring_Tip);

            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_LITTLE_METACARPAL_EXT], ref m_Little_Metacarpal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_LITTLE_PROXIMAL_EXT], ref m_Little_Proximal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_LITTLE_INTERMEDIATE_EXT], ref m_Little_Intermediate);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_LITTLE_DISTAL_EXT], ref m_Little_Distal);
            UpdateJointRotation(handJointLocation[(int)XrHandJointEXT.XR_HAND_JOINT_LITTLE_TIP_EXT], ref m_Little_Tip);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        private void UpdateJointPosition(XrHandJointLocationEXT pose, ref GameObject joint)
        {
            if (((UInt64)pose.locationFlags & (UInt64)XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_TRACKED_BIT) != 0)
            {
                Vector3 pos = OpenXRHelper.ToUnityVector(pose.pose.position);
                m_Wrist.transform.localPosition = pos;
            }
        }
        private void UpdateJointRotation(XrHandJointLocationEXT pose, ref GameObject joint)
        {
            if (((UInt64)pose.locationFlags & (UInt64)XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT) != 0)
            {
                Quaternion rot = OpenXRHelper.ToUnityQuaternion(pose.pose.orientation);
                joint.transform.rotation = rot;
            }
        }
    }
}