// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine.Profiling;

namespace VIVE.OpenXR
{
    public partial class ViveInterceptors
    {
        [HookHandler("xrGetVisibilityMaskKHR")]
        private static XrResult OnHookXrGetVisibilityMaskKHR(XrInstance instance, string name, out IntPtr function)
        {
            if (xrGetVisibilityMaskKHROriginal == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                xrGetVisibilityMaskKHROriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrGetVisibilityMaskKHR>(function);
            }
            function = xrGetVisibilityMaskKHRInterceptorPtr;
            return XrResult.XR_SUCCESS;
        }

        public enum XrVisibilityMaskTypeKHR
        {
            HIDDEN_TRIANGLE_MESH_KHR = 1,
            VISIBLE_TRIANGLE_MESH_KHR = 2,
            LINE_LOOP_KHR = 3,
        }

        public struct XrVisibilityMaskKHR
        {
            public XrStructureType type;
            public IntPtr next;
            public uint vertexCapacityInput;
            public uint vertexCountOutput;
            public IntPtr vertices;  // XrVector2f array
            public uint indexCapacityInput;
            public uint indexCountOutput;
            public IntPtr indices;  // uint array
        }

        // XrCompositionLayerSpaceWarpInfoFlagsFB bits
        public delegate XrResult DelegateXrGetVisibilityMaskKHR(XrSession session, XrViewConfigurationType viewConfigurationType, uint viewIndex, XrVisibilityMaskTypeKHR visibilityMaskType, ref XrVisibilityMaskKHR visibilityMask);

        private static readonly DelegateXrGetVisibilityMaskKHR xrGetVisibilityMaskKHRInterceptorHandle = new DelegateXrGetVisibilityMaskKHR(XrGetVisibilityMaskKHRInterceptor);
        private static readonly IntPtr xrGetVisibilityMaskKHRInterceptorPtr = Marshal.GetFunctionPointerForDelegate(xrGetVisibilityMaskKHRInterceptorHandle);
        static DelegateXrGetVisibilityMaskKHR xrGetVisibilityMaskKHROriginal = null;

        [MonoPInvokeCallback(typeof(DelegateXrGetVisibilityMaskKHR))]
        private static XrResult XrGetVisibilityMaskKHRInterceptor(XrSession session, XrViewConfigurationType viewConfigurationType, uint viewIndex, XrVisibilityMaskTypeKHR visibilityMaskType, ref XrVisibilityMaskKHR visibilityMask)
        {
            // instance must not null
            //if (instance == null)
            //	return XrGetVisibilityMaskKHROriginal(session, ref frameEndInfo);
            Profiler.BeginSample("VI:GetVMB");
            XrResult result = XrResult.XR_SUCCESS;
            bool ret = true;
            if (instance.BeforeOriginalGetVisibilityMaskKHR != null)
                ret = instance.BeforeOriginalGetVisibilityMaskKHR(session, viewConfigurationType, viewIndex, visibilityMaskType, ref visibilityMask, ref result);
            Profiler.EndSample();
            if (!ret)
                return result;
            result = xrGetVisibilityMaskKHROriginal(session, viewConfigurationType, viewIndex, visibilityMaskType, ref visibilityMask);
            Profiler.BeginSample("VI:GetVMA");
            instance.AfterOriginalGetVisibilityMaskKHR?.Invoke(session, viewConfigurationType, viewIndex, visibilityMaskType, ref visibilityMask, ref result);
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
        public delegate bool DelegateXrGetVisibilityMaskKHRInterceptor(XrSession session, XrViewConfigurationType viewConfigurationType, uint viewIndex, XrVisibilityMaskTypeKHR visibilityMaskType, ref XrVisibilityMaskKHR visibilityMask, ref XrResult result);

        /// <summary>
        /// Use this to intercept the original function.  This will be called before the original function.
        /// </summary>
        public DelegateXrGetVisibilityMaskKHRInterceptor BeforeOriginalGetVisibilityMaskKHR;

        /// <summary>
        /// Use this to intercept the original function.  This will be called after the original function.
        /// </summary>
        public DelegateXrGetVisibilityMaskKHRInterceptor AfterOriginalGetVisibilityMaskKHR;
    }
}
