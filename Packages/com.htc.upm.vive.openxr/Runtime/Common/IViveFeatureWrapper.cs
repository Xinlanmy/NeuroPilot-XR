// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace VIVE.OpenXR.Feature
{
    public interface IViveFeatureWrapper
    {
        /// <summary>
        /// OnInstanceCreate might be called multiple times.  Because many features might be using the same instance.
        /// </summary>
        /// <param name="xrInstance"></param>
        /// <param name="xrGetInstanceProcAddr"></param>
        /// <returns></returns>
        public bool OnInstanceCreate(XrInstance xrInstance, IntPtr xrGetInstanceProcAddr);

        /// <summary>
        /// OnInstanceDestroy might be called multiple times.  Because many features might be using the same instance.
        /// </summary>
        public void OnInstanceDestroy();
    }

    public class ViveFeatureWrapperBase<T> where T : ViveFeatureWrapperBase<T>, new()
    {
        private static readonly Lazy<T> lazyInstance = new Lazy<T>(() => new T());

        public static T Instance => lazyInstance.Value;

        // Set true in yourfeature's OnInstanceCreate
        public bool IsInited { get; protected set; } = false;

        /// <summary>
        /// If the feature is inited not successfully, Set this true.  Use to avoid multiple inits.
        /// </summary>
        public bool TryInited { get; protected set; } = false;

        public OpenXRHelper.xrGetInstanceProcAddrDelegate xrGetInstanceProcAddr;

        /// <summary>
        /// Complete the xrGetInstanceProcAddr by set the pointer received in OnInstanceCreate
        /// </summary>
        /// <param name="intPtr"></param>
        public void SetGetInstanceProcAddrPtr(IntPtr intPtr)
        {
            if (intPtr == null || intPtr == IntPtr.Zero)
                throw new Exception("xrGetInstanceProcAddr is null");

            xrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<OpenXRHelper.xrGetInstanceProcAddrDelegate>(intPtr);
        }
    }
}
