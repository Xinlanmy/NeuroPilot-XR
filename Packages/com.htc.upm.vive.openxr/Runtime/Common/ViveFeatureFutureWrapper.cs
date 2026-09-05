// Copyright HTC Corporation All Rights Reserved.
using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace VIVE.OpenXR.Feature
{
    using XrFutureEXT = System.IntPtr;

    /// <summary>
    /// To use this wrapper,
    /// 1. Add the "XR_EXT_Future" extension to the instance's enabled extensions list.
    /// 2. Call FutureWrapper.Instance.OnInstanceCreate() in your feature's OnInstanceCreate().
    /// 3. Call FutureWrapper.Instance.OnInstanceDestroy() in your feature's OnInstanceDestroy().
    /// 
    /// <see cref="VIVE.OpenXR.Toolkits.FutureTask.Poll"/> function helps make async Task.
    /// </summary>
    public class FutureWrapper : ViveFeatureWrapperBase<FutureWrapper>, IViveFeatureWrapper
    {
        const string TAG = "ViveFuture";

        public enum XrFutureStateEXT
        {
            None = 0,  // Not defined in extension. A default value.
            Pending = 1,
            Ready = 2,
            MAX = 0x7FFFFFFF
        }

        public struct XrFuturePollInfoEXT {
            public XrStructureType type;  // XR_TYPE_FUTURE_POLL_INFO_EXT
            public IntPtr next;
            public XrFutureEXT future;
        }

        public struct XrFuturePollResultEXT {
            public XrStructureType type;  // XR_TYPE_FUTURE_POLL_RESULT_EXT
            public IntPtr next;
            public XrFutureStateEXT state;
        }

        public struct XrFutureCancelInfoEXT
        {
            public XrStructureType type;  // XR_TYPE_FUTURE_CANCEL_INFO_EXT
            public IntPtr next;
            public XrFutureEXT future;
        }

        public struct XrFutureCompletionBaseHeaderEXT
        {
            public XrStructureType type;  // XR_TYPE_FUTURE_COMPLETION_EXT
            public IntPtr next;
            public XrResult futureResult;
        }

        public struct XrFutureCompletionEXT
        {
            public XrStructureType type;  // XR_TYPE_FUTURE_COMPLETION_EXT
            public IntPtr next;
            public XrResult futureResult;
        }

        public delegate XrResult XrPollFutureEXTDelegate(XrInstance instance, ref XrFuturePollInfoEXT pollInfo, out XrFuturePollResultEXT pollResult);
        public delegate XrResult XrCancelFutureEXTDelegate(XrInstance instance, ref XrFutureCancelInfoEXT cancelInfo);

        XrPollFutureEXTDelegate XrPollFutureEXT;
        XrCancelFutureEXTDelegate XrCancelFutureEXT;

        XrInstance xrInstance;

        /// <summary>
        /// Features should call FutureWrapper.Instance.OnInstanceCreate() in their OnInstanceCreate().
        /// </summary>
        /// <param name="xrInstance"></param>
        /// <param name="xrGetInstanceProcAddrPtr"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool OnInstanceCreate(XrInstance xrInstance, IntPtr xrGetInstanceProcAddrPtr)
        {
            if (IsInited) return true;
            if (TryInited) return false;
                TryInited = true;

            if (xrInstance == null)
                throw new Exception("FutureWrapper: xrInstance is null");
            this.xrInstance = xrInstance;

            if (xrGetInstanceProcAddrPtr == null)
                throw new Exception("FutureWrapper: xrGetInstanceProcAddr is null");
            SetGetInstanceProcAddrPtr(xrGetInstanceProcAddrPtr);

            Log.D(TAG, "OnInstanceCreate()");

            bool hasFuture = OpenXRRuntime.IsExtensionEnabled("XR_EXT_future");
            if (!hasFuture)
            {
                Log.E(TAG, "FutureWrapper: XR_EXT_future is not enabled.  Check your feature's kOpenxrExtensionString.");
                return false;
            }

            bool ret = true;
            IntPtr funcPtr = IntPtr.Zero;

            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrPollFutureEXT", out XrPollFutureEXT);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrCancelFutureEXT", out XrCancelFutureEXT);

            if (!ret)
            {
                Log.E(TAG,"FutureWrapper: Failed to get function pointer.");
                return false;
            }

            IsInited = ret;
            return ret;
        }

        public void OnInstanceDestroy()
        {
            Log.D(TAG, "OnInstanceDestroy()");
            IsInited = false;
            XrPollFutureEXT = null;
            XrCancelFutureEXT = null;
            xrInstance = 0;
        }

        /// <summary>
        /// Used to get the state of a future. If Ready, Call complete functions to get the result.
        /// </summary>
        /// <param name="pollInfo"></param>
        /// <param name="pollResult"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrResult PollFuture(ref XrFuturePollInfoEXT pollInfo, out XrFuturePollResultEXT pollResult)
        {
            pollResult= new XrFuturePollResultEXT()
            {
                type = XrStructureType.XR_TYPE_FUTURE_POLL_RESULT_EXT,
                next = IntPtr.Zero,
                state = XrFutureStateEXT.None
            };
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            return XrPollFutureEXT(xrInstance, ref pollInfo, out pollResult);
        }

        /// <summary>
        /// Used to get the state of a future. If Ready, Call complete functions to get the result.
        /// </summary>
        /// <param name="future"></param>
        /// <param name="pollResult"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrResult PollFuture(XrFutureEXT future, out XrFuturePollResultEXT pollResult)
        {
            pollResult = new XrFuturePollResultEXT()
            {
                type = XrStructureType.XR_TYPE_FUTURE_POLL_RESULT_EXT,
                next = IntPtr.Zero,
                state = XrFutureStateEXT.None
            };
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            XrFuturePollInfoEXT pollInfo = new XrFuturePollInfoEXT()
            {
                type = XrStructureType.XR_TYPE_FUTURE_POLL_INFO_EXT,
                next = IntPtr.Zero,
                future = future
            };

            return XrPollFutureEXT(xrInstance, ref pollInfo, out pollResult);
        }

        /// <summary>
        /// This function cancels the future and signals that the async operation is not required.
        /// After a future has been cancelled any functions using this future must return XR_ERROR_FUTURE_INVALID_EXT.
        /// </summary>
        /// <param name="cancelInfo"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrResult CancelFuture(ref XrFutureCancelInfoEXT cancelInfo)
        {
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            return XrCancelFutureEXT(xrInstance, ref cancelInfo);
        }

        /// <summary>
        /// <see cref="CancelFuture(ref XrFutureCancelInfoEXT)"/>
        /// </summary>
        /// <param name="future"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrResult CancelFuture(XrFutureEXT future)
        {
            if (!IsInited)
                return XrResult.XR_ERROR_HANDLE_INVALID;

            XrFutureCancelInfoEXT cancelInfo = new XrFutureCancelInfoEXT()
            {
                type = XrStructureType.XR_TYPE_FUTURE_CANCEL_INFO_EXT,
                next = IntPtr.Zero,
                future = future
            };

            return XrCancelFutureEXT(xrInstance, ref cancelInfo);
        }
    }
}
