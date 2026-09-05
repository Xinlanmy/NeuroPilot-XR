// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine.Profiling;

namespace VIVE.OpenXR
{
    public partial class ViveInterceptors
    {
        [HookHandler("xrLocateViews")]
        private static XrResult OnHookXrLocateViews(XrInstance instance, string name, out IntPtr function)
        {
            if (xrLocateViewsOriginal == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                xrLocateViewsOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrLocateViews>(function);
            }
            function = xrLocateViewsInterceptorPtr;
            return XrResult.XR_SUCCESS;
        }

        public struct XrViewLocateInfo
        {
            public XrStructureType type;
            public IntPtr next;
            public XrViewConfigurationType viewConfigurationType;
            public XrTime displayTime;
            public XrSpace space;
        }

        public struct XrView
        {
            public XrStructureType type;
            public IntPtr next;
            public XrPosef pose;
            public XrFovf fov;
        }

        public enum XrViewStateFlags {
            ORIENTATION_VALID_BIT = 0x00000001,
            POSITION_VALID_BIT = 0x00000002,
            ORIENTATION_TRACKED_BIT = 0x00000004,
            POSITION_TRACKED_BIT = 0x00000008,
        }

        public struct XrViewState
        {
            public XrStructureType type;
            public IntPtr next;
            public XrViewStateFlags viewStateFlags;
        }

        public delegate XrResult DelegateXrLocateViews(XrSession session, IntPtr /*XrViewLocateInfo*/ viewLocateInfo, IntPtr /*XrViewState*/ viewState, uint viewCapacityInput, ref uint viewCountOutput, IntPtr /*XrView*/ views);

        private static readonly DelegateXrLocateViews xrLocateViewsInterceptorHandle = new DelegateXrLocateViews(XrLocateViewsInterceptor);
        private static readonly IntPtr xrLocateViewsInterceptorPtr = Marshal.GetFunctionPointerForDelegate(xrLocateViewsInterceptorHandle);
        static DelegateXrLocateViews xrLocateViewsOriginal = null;
        static int xrLocateViewsReferenceCount = 0;

        [MonoPInvokeCallback(typeof(DelegateXrLocateViews))]
        private static XrResult XrLocateViewsInterceptor(XrSession session, IntPtr viewLocateInfo, IntPtr viewState, uint viewCapacityInput, ref uint viewCountOutput, IntPtr views)
        {
            // Call the original function if the reference count is less than or equal to 0
            if (xrLocateViewsReferenceCount <= 0)
                return xrLocateViewsOriginal(session, viewLocateInfo, viewState, viewCapacityInput, ref viewCountOutput, views);

            Profiler.BeginSample("VI:LocateViewsA");
            XrResult result = XrResult.XR_SUCCESS;
            if (instance.BeforeOriginalLocateViews != null)
                instance.BeforeOriginalLocateViews(session, viewLocateInfo, viewState, viewCapacityInput, ref viewCountOutput, views);
            Profiler.EndSample();
            result = xrLocateViewsOriginal(session, viewLocateInfo, viewState, viewCapacityInput, ref viewCountOutput, views);
            Profiler.BeginSample("VI:LocateViewsB");
            instance.AfterOriginalLocateViews?.Invoke(session, viewLocateInfo, viewState, viewCapacityInput, ref viewCountOutput, views);
            Profiler.EndSample();
            return result;
        }

        /// <summary>
        /// If you return false, the original function will not be called.
        /// </summary>
        /// <returns></returns>
        public delegate bool DelegateXrLocateViewsInterceptor(XrSession session, IntPtr viewLocateInfo, IntPtr viewState, uint viewCapacityInput, ref uint viewCountOutput, IntPtr views);

        /// <summary>
        /// Use this to intercept the original function.  This will be called before the original function.
        /// </summary>
        public DelegateXrLocateViewsInterceptor BeforeOriginalLocateViews;

        /// <summary>
        /// Use this to intercept the original function.  This will be called after the original function.
        /// </summary>
        public DelegateXrLocateViewsInterceptor AfterOriginalLocateViews;
    }
}
