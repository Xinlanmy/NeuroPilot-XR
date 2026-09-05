// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine.Profiling;

namespace VIVE.OpenXR
{
    partial class ViveInterceptors
    {
        [HookHandler("xrEndFrame")]
        private static XrResult OnHookXrEndFrame(XrInstance instance, string name, out IntPtr function)
        {
            if (XrEndFrameOriginal == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                XrEndFrameOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrEndFrame>(function);
            }
            function = xrEndFrameInterceptorPtr;
            return XrResult.XR_SUCCESS;
        }

        public struct XrCompositionLayerBaseHeader
        {
            public XrStructureType type;  // This base structure itself has no associated XrStructureType value.
            public System.IntPtr next;
            public XrCompositionLayerFlags layerFlags;
            public XrSpace space;
        }

        public struct XrFrameEndInfo
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrTime displayTime;
            public XrEnvironmentBlendMode environmentBlendMode;
            public uint layerCount;
            public IntPtr layers;  // XrCompositionLayerBaseHeader IntPtr array
        }

        public delegate XrResult DelegateXrEndFrame(XrSession session, ref XrFrameEndInfo frameEndInfo);
        private static readonly DelegateXrEndFrame xrEndFrameInterceptorHandle = new DelegateXrEndFrame(XrEndFrameInterceptor);
        private static readonly IntPtr xrEndFrameInterceptorPtr = Marshal.GetFunctionPointerForDelegate(xrEndFrameInterceptorHandle);
        static DelegateXrEndFrame XrEndFrameOriginal = null;

        [MonoPInvokeCallback(typeof(DelegateXrEndFrame))]
        private static XrResult XrEndFrameInterceptor(XrSession session, ref XrFrameEndInfo frameEndInfo)
        {
            // instance must not null
            //if (instance == null)
            //	return XrEndFrameOriginal(session, ref frameEndInfo);
            Profiler.BeginSample("VI:EndFrameB");
            XrResult result = XrResult.XR_SUCCESS;
            bool ret = true;
            if (instance.BeforeOriginalEndFrame != null)
                ret = instance.BeforeOriginalEndFrame(session, ref frameEndInfo, ref result);
            Profiler.EndSample();
            if (!ret)
                return result;
            result = XrEndFrameOriginal(session, ref frameEndInfo);
            Profiler.BeginSample("VI:EndFrameA");
            instance.AfterOriginalEndFrame?.Invoke(session, ref frameEndInfo, ref result);
            Profiler.EndSample();
            return result;
        }

        /// <summary>
        /// If you return false, the original function will not be called.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="frameEndInfo"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public delegate bool DelegateXrEndFrameInterceptor(XrSession session, ref XrFrameEndInfo frameEndInfo, ref XrResult result);

        /// <summary>
        /// Use this to intercept the original function.  This will be called before the original function.
        /// </summary>
        public DelegateXrEndFrameInterceptor BeforeOriginalEndFrame;

        /// <summary>
        /// Use this to intercept the original function.  This will be called after the original function.
        /// </summary>
        public DelegateXrEndFrameInterceptor AfterOriginalEndFrame;

#if PERFORMANCE_TEST
        public delegate XrResult DelegateXrLocateSpace(XrSpace space, XrSpace baseSpace, XrTime time, ref XrSpaceLocation location);
        private static readonly DelegateXrLocateSpace xrLocateSpaceInterceptorHandle = new DelegateXrLocateSpace(XrLocateSpaceInterceptor);
        private static readonly IntPtr xrLocateSpaceInterceptorPtr = Marshal.GetFunctionPointerForDelegate(xrLocateSpaceInterceptorHandle);
        static DelegateXrLocateSpace XrLocateSpaceOriginal = null;

        [MonoPInvokeCallback(typeof(DelegateXrLocateSpace))]
        public static XrResult XrLocateSpaceInterceptor(XrSpace space, XrSpace baseSpace, XrTime time, ref XrSpaceLocation location)
        {
            Profiler.BeginSample("VI:LocateSpace");
            var ret = XrLocateSpaceOriginal(space, baseSpace, time, ref location);
            Profiler.EndSample();
            return ret;
        }
#endif
    }
}
