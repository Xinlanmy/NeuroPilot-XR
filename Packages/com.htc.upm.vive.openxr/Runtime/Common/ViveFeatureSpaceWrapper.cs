// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VIVE.OpenXR.Feature
{

    /// <summary>
    /// To use this wrapper, you need to call CommonWrapper.Instance.OnInstanceCreate() in your feature's OnInstanceCreate(),
    /// and call CommonWrapper.Instance.OnInstanceDestroy() in your feature's OnInstanceDestroy().
    /// </summary>
    public class SpaceWrapper : ViveFeatureWrapperBase<SpaceWrapper>, IViveFeatureWrapper
    {
        const string TAG = "ViveSpaceWrapper";

        public delegate XrResult DelegateXrEnumerateReferenceSpaces(XrSession session, uint spaceCapacityInput, out uint spaceCountOutput, [Out] XrReferenceSpaceType[] spaces);
        delegate XrResult DelegateXrLocateSpace(XrSpace space, XrSpace baseSpace, XrTime time, ref XrSpaceLocation location);
        delegate XrResult DelegateXrDestroySpace(XrSpace space);

        DelegateXrEnumerateReferenceSpaces XrEnumerateReferenceSpaces;
        OpenXRHelper.xrCreateReferenceSpaceDelegate XrCreateReferenceSpace;
        DelegateXrLocateSpace XrLocateSpace;
        DelegateXrDestroySpace XrDestroySpace;

        /// <summary>
        /// Features should call ViveSpaceWrapper.Instance.OnInstanceCreate() in their OnInstanceCreate().
        /// </summary>
        /// <param name="xrInstance"></param>
        /// <param name="GetAddr"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool OnInstanceCreate(XrInstance xrInstance, IntPtr GetAddr)
        {
            if (IsInited) return true;
            if (TryInited) return false;
                TryInited = true;

            if (xrInstance == null)
                throw new Exception("ViveSpaceWrapper: xrInstance is null");

            SetGetInstanceProcAddrPtr(GetAddr);

            Log.D(TAG, "OnInstanceCreate()");

            bool ret = true;
            IntPtr funcPtr = IntPtr.Zero;

            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrEnumerateReferenceSpaces", out XrEnumerateReferenceSpaces);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrCreateReferenceSpace", out XrCreateReferenceSpace);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrLocateSpace", out XrLocateSpace);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrDestroySpace", out XrDestroySpace);
            IsInited = ret;
            return ret;
        }

        public void OnInstanceDestroy()
        {
            // Do not destroy twice
            if (IsInited == false) return;
            IsInited = false;
            XrEnumerateReferenceSpaces = null;
            XrCreateReferenceSpace = null;
            XrLocateSpace = null;
            XrDestroySpace = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="spaceCapacityInput"></param>
        /// <param name="spaceCountOutput"></param>
        /// <param name="spaces"></param>
        /// <returns></returns>
        public XrResult EnumerateReferenceSpaces(XrSession session, int spaceCapacityInput, ref int spaceCountOutput, ref XrReferenceSpaceType[] spaces)
        {
            spaceCountOutput = 0;
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;
            if (spaceCapacityInput != 0 && spaces != null && spaces.Length < spaceCapacityInput)
                return XrResult.XR_ERROR_SIZE_INSUFFICIENT;
            var ret = XrEnumerateReferenceSpaces(session, (uint)spaceCapacityInput, out uint spaceCountOutputXR, spaces);
            spaceCountOutput = (int)spaceCountOutputXR;
            return ret;
        }

        /// <summary>
        /// Create a reference space without create info.
        /// Example:
        ///   CreateReferenceSpace(session, XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL, XrPosef.Identity, out space);
        ///   CreateReferenceSpace(session, XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE, XrPosef.Identity, out space);
        /// </summary>
        /// <param name="session"></param>
        /// <param name="referenceSpaceType"></param>
        /// <param name="pose"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public XrResult CreateReferenceSpace(XrSession session, XrReferenceSpaceType referenceSpaceType, XrPosef pose, out XrSpace space)
        {
            space = 0;
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            var createInfo = new XrReferenceSpaceCreateInfo();
            createInfo.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
            createInfo.next = IntPtr.Zero;
            createInfo.referenceSpaceType = referenceSpaceType;
            createInfo.poseInReferenceSpace = pose;
            return XrCreateReferenceSpace(session, ref createInfo, out space);
        }

        /// <summary>
        /// Create a reference space.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="createInfo"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public XrResult CreateReferenceSpace(XrSession session, XrReferenceSpaceCreateInfo createInfo, out XrSpace space)
        {
            space = 0;
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            return XrCreateReferenceSpace(session, ref createInfo, out space);
        }

        public XrResult LocateSpace(XrSpace space, XrSpace baseSpace, XrTime time, ref XrSpaceLocation location)
        {
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;
            //Debug.Log($"LocateSpace(s={space}, bs={baseSpace}, t={time}");
            return XrLocateSpace(space, baseSpace, time, ref location);
        }

        public XrResult DestroySpace(XrSpace space)
        {
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;
            Log.D(TAG, $"DestroySpace({space})");
            return XrDestroySpace(space);
        }
    }

    /// <summary>
    /// The XrSpace's Unity wrapper.  Input and output are in Unity coordinate system.
    /// After use it, you should call Dispose() to release the XrSpace.
    /// </summary>
    public class Space : IDisposable
    {
        protected XrSpace space;
        private bool disposed = false;

        public Space(XrSpace space)
        {
            Log.D($"Space({space})");
            this.space = space;
        }

        /// <summary>
        /// Get the raw XrSpace.  Only use it when class Space instance is alive.
        /// You should not try to store this XrSpace, because it may be destroyed.
        /// </summary>
        /// <returns></returns>
        public XrSpace GetXrSpace()
        {
            return space;
        }

        public bool GetRelatedPose(XrSpace baseSpace, XrTime time, out UnityEngine.Pose pose)
        {
            // If the xrBaseSpace is changed, the pose will be updated.
            pose = default;
            XrSpaceLocation location = new XrSpaceLocation();
            location.type = XrStructureType.XR_TYPE_SPACE_LOCATION;
            location.next = IntPtr.Zero;
            var ret = SpaceWrapper.Instance.LocateSpace(space, baseSpace, time, ref location);

            if (ret != XrResult.XR_SUCCESS)
            {
                //Debug.Log("Space: LocateSpace ret=" + ret);
                return false;
            }

            //Debug.Log("Space: baseSpace=" + baseSpace + ", space=" + space + ", time=" + time + ", ret=" + ret);
            //Debug.Log("Space: location.locationFlags=" + location.locationFlags);
            //Debug.Log("Space: location.pose.position=" + location.pose.position.x + "," + location.pose.position.y + "," + location.pose.position.z);
            //Debug.Log("Space: location.pose.orientation=" + location.pose.orientation.x + "," + location.pose.orientation.y + "," + location.pose.orientation.z + "," + location.pose.orientation.w);
            if ((location.locationFlags & XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_VALID_BIT) > 0 &&
                (location.locationFlags & XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_VALID_BIT) > 0)
            {
                pose = new Pose(location.pose.position.ToUnityVector(), location.pose.orientation.ToUnityQuaternion());
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Managered resource
                }
                // Non managered resource
                //Debug.Log($"Space: DestroySpace({space})");
                SpaceWrapper.Instance.DestroySpace(space);
                space = 0;
                disposed = true;
            }
        }

        ~Space()
        {
            Dispose(false);
        }
    }
}
