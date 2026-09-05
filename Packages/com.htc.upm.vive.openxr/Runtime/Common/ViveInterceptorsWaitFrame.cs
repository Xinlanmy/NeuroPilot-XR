// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using AOT;
using UnityEngine.Profiling;

namespace VIVE.OpenXR
{
    partial class ViveInterceptors
    {
        [HookHandler("xrWaitFrame")]
        private static XrResult OnHookXrWaitFrame(XrInstance instance, string name, out IntPtr function)
        {
            if (XrWaitFrameOriginal == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                XrWaitFrameOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrWaitFrame>(function);
            }
            function = xrWaitFrameInterceptorPtr;
            return XrResult.XR_SUCCESS;
        }

        public struct XrFrameWaitInfo
        {
            public XrStructureType type;
            public IntPtr next;
        }

        public struct XrFrameState
        {
            public XrStructureType type;
            public IntPtr next;
            public XrTime predictedDisplayTime;
            public XrDuration predictedDisplayPeriod;
            public XrBool32 shouldRender;
        }

        bool isWaitFrameIntercepted = false;

        public delegate XrResult DelegateXrWaitFrame(XrSession session, ref XrFrameWaitInfo frameWaitInfo, ref XrFrameState frameState);
        private static readonly DelegateXrWaitFrame xrWaitFrameInterceptorHandle = new DelegateXrWaitFrame(XrWaitFrameInterceptor);
        private static readonly IntPtr xrWaitFrameInterceptorPtr = Marshal.GetFunctionPointerForDelegate(xrWaitFrameInterceptorHandle);
        static DelegateXrWaitFrame XrWaitFrameOriginal = null;

        [MonoPInvokeCallback(typeof(DelegateXrWaitFrame))]
        private static XrResult XrWaitFrameInterceptor(XrSession session, ref XrFrameWaitInfo frameWaitInfo, ref XrFrameState frameState)
        {
            // instance must not null
            //if (instance == null)
            //	return XrWaitFrameOriginal(session, ref frameWaitInfo, ref frameState);
            Profiler.BeginSample("VI:WaitFrame");
            instance.isWaitFrameIntercepted = true;
            XrResult result = XrResult.XR_SUCCESS;
            if (instance.BeforeOriginalWaitFrame != null &&
                !instance.BeforeOriginalWaitFrame(session, ref frameWaitInfo, ref frameState, ref result))
            {
                Profiler.EndSample();
                return result;
            }
            var ret = XrWaitFrameOriginal(session, ref frameWaitInfo, ref frameState);
            instance.AfterOriginalWaitFrame?.Invoke(session, ref frameWaitInfo, ref frameState, ref result);
            currentFrameState = frameState;
            Profiler.EndSample();
            return result;
        }

        static XrFrameState currentFrameState = new XrFrameState() { predictedDisplayTime = 0 };

        /// <summary>
        /// Get the waitframe's result: XrFrameState.  This result used in update is not matching the current frame.  Use it after onBeforeRender.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrFrameState GetCurrentFrameState()
        {
            if (!isWaitFrameIntercepted) throw new Exception("ViveInterceptors is not intercepted");

            return currentFrameState;
        }

        /// <summary>
        /// Must request xrWaitFrame before calling this function.  This result used in update is not matching the current frame.  Use it after onBeforeRender.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public XrTime GetPredictTime()
        {
            if (!isWaitFrameIntercepted) throw new Exception("ViveInterceptors is not intercepted");

            //Debug.Log($"{TAG}: XrWaitFrameInterceptor(predictedDisplayTime={currentFrameState.predictedDisplayTime}");
            if (currentFrameState.predictedDisplayTime == 0)
                return new XrTime((long)(1000000L * (Time.unscaledTimeAsDouble + 0.011f)));
            else
                return currentFrameState.predictedDisplayTime;
        }

        /// <summary>
        /// Register WaitFrame event
        /// </summary>
        /// <param name="session"></param>
        /// <param name="frameWaitInfo"></param>
        /// <param name="frameState"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public delegate bool DelegateXrWaitFrameInterceptor(XrSession session, ref XrFrameWaitInfo frameWaitInfo, ref XrFrameState frameState, ref XrResult result);

        /// <summary>
        /// Use this to intercept the original function.  This will be called before the original function.
        /// </summary>
        public DelegateXrWaitFrameInterceptor BeforeOriginalWaitFrame;

        /// <summary>
        /// Use this to intercept the original function.  This will be called after the original function.
        /// </summary>
        public DelegateXrWaitFrameInterceptor AfterOriginalWaitFrame;
    }
}
