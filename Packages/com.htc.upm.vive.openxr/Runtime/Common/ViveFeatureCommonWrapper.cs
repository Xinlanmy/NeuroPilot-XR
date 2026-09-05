// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace VIVE.OpenXR.Feature
{
    /// <summary>
    /// To use this wrapper, you need to call CommonWrapper.Instance.OnInstanceCreate() in your feature's OnInstanceCreate(), 
    /// and call CommonWrapper.Instance.OnInstanceDestroy() in your feature's OnInstanceDestroy().
    /// 
    /// Note:
    /// In Standardalone's OpenXR MockRuntime, the CreateSwapchain and EnumerateSwapchainImages will work and return success,
    /// but the images's native pointer will be null.
    /// </summary>
    internal class CommonWrapper : ViveFeatureWrapperBase<CommonWrapper>, IViveFeatureWrapper
    {
        const string TAG = "CommonWrapper";
        OpenXRHelper.xrGetSystemPropertiesDelegate XrGetSystemProperties;
        OpenXRHelper.xrCreateSwapchainDelegate XrCreateSwapchain;
        OpenXRHelper.xrDestroySwapchainDelegate XrDestroySwapchain;
        OpenXRHelper.xrEnumerateSwapchainFormatsDelegate XrEnumerateSwapchainFormats;
        OpenXRHelper.xrEnumerateSwapchainImagesDelegate XrEnumerateSwapchainImages;
        OpenXRHelper.xrWaitSwapchainImageDelegate XrWaitSwapchainImage;
        OpenXRHelper.xrAcquireSwapchainImageDelegate XrAcquireSwapchainImage;
        OpenXRHelper.xrReleaseSwapchainImageDelegate XrReleaseSwapchainImage;

        /// <summary>
        /// In feature's OnInstanceCreate(), call CommonWrapper.Instance.OnInstanceCreate() for init common APIs.
        /// </summary>
        /// <param name="xrInstance">Passed in feature's OnInstanceCreate.</param>
        /// <param name="xrGetInstanceProcAddr">Pass OpenXRFeature.xrGetInstanceProcAddr in.</param>
        /// <returns></returns>
        /// <exception cref="Exception">If input data not valid.</exception>
        public bool OnInstanceCreate(XrInstance xrInstance, IntPtr xrGetInstanceProcAddrPtr)
        {
            if (IsInited) return true;
            if (TryInited) return false;
                TryInited = true;

            if (xrInstance == 0)
                throw new Exception("CommonWrapper: xrInstance is null");

            Log.D(TAG, "OnInstanceCreate()");
            SetGetInstanceProcAddrPtr(xrGetInstanceProcAddrPtr);

            bool ret = true;
            IntPtr funcPtr = IntPtr.Zero;

            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrGetSystemProperties", out XrGetSystemProperties);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrCreateSwapchain", out XrCreateSwapchain);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrDestroySwapchain", out XrDestroySwapchain);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrEnumerateSwapchainFormats", out XrEnumerateSwapchainFormats);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrEnumerateSwapchainImages", out XrEnumerateSwapchainImages);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrWaitSwapchainImage", out XrWaitSwapchainImage);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrAcquireSwapchainImage", out XrAcquireSwapchainImage);
            ret &= OpenXRHelper.GetXrFunctionDelegate(xrGetInstanceProcAddr, xrInstance, "xrReleaseSwapchainImage", out XrReleaseSwapchainImage);

            if (!ret)
                throw new Exception("CommonWrapper: Get function pointers failed.");

            IsInited = ret;
            return ret;
        }

        /// <summary>
        /// In feature's OnInstanceDestroy(), call CommonWrapper.Instance.OnInstanceDestroy() for disable common APIs.
        /// </summary>
        /// <returns></returns>
        public void OnInstanceDestroy()
        {
            // Do not destroy twice
            if (IsInited == false) return;
            IsInited = false;
            XrGetSystemProperties = null;
            Log.D(TAG, "OnInstanceDestroy()");
        }

        public XrResult GetInstanceProcAddr(XrInstance instance, string name, out IntPtr function)
        {
            if (IsInited == false || xrGetInstanceProcAddr == null)
            {
                function = IntPtr.Zero;
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            return xrGetInstanceProcAddr(instance, name, out function);
        }

        /// <summary>
        /// Helper function to get system properties.  Need input your features' xrInstance and xrSystemId.  Fill the system properites in next for you feature.
        /// See <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrGetSystemProperties">xrGetSystemProperties</see>
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="systemId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public XrResult GetSystemProperties(XrInstance instance, XrSystemId systemId, ref XrSystemProperties properties)
        {
            if (IsInited == false || XrGetSystemProperties == null)
            {
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            return XrGetSystemProperties(instance, systemId, ref properties);
        }


        public XrResult GetProperties<T>(XrInstance instance, XrSystemId systemId, ref T featureProperty)
        {
            XrSystemProperties systemProperties = new XrSystemProperties();
            systemProperties.type = XrStructureType.XR_TYPE_SYSTEM_PROPERTIES;
            systemProperties.next = Marshal.AllocHGlobal(Marshal.SizeOf(featureProperty));

            long offset = 0;
            if (IntPtr.Size == 4)
                offset = systemProperties.next.ToInt32();
            else
                offset = systemProperties.next.ToInt64();

            IntPtr pdPropertiesPtr = new IntPtr(offset);
            Marshal.StructureToPtr(featureProperty, pdPropertiesPtr, false);

            var ret = GetSystemProperties(instance, systemId, ref systemProperties);
            if (ret == XrResult.XR_SUCCESS)
            {
                if (IntPtr.Size == 4)
                    offset = systemProperties.next.ToInt32();
                else
                    offset = systemProperties.next.ToInt64();

                pdPropertiesPtr = new IntPtr(offset);
                featureProperty = Marshal.PtrToStructure<T>(pdPropertiesPtr);
            }

            Marshal.FreeHGlobal(systemProperties.next);
            return ret;
        }

        public XrResult CreateSwapchain(XrSession session, ref XrSwapchainCreateInfo createInfo, out XrSwapchain swapchain)
        {
            if (IsInited == false || XrCreateSwapchain == null)
            {
                swapchain = default;
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            return XrCreateSwapchain(session, ref createInfo, out swapchain);
        }

        public XrResult DestroySwapchain(XrSwapchain swapchain)
        {
            if (IsInited == false || XrDestroySwapchain == null)
            {
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            return XrDestroySwapchain(swapchain);
        }

        public XrResult EnumerateSwapchainFormats(XrSession session, uint formatCapacityInput, ref uint formatCountOutput, ref long[] formats)
        {
            if (IsInited == false || XrEnumerateSwapchainFormats == null)
            {
                formatCountOutput = 0;
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            if (formatCapacityInput != 0 && (formats == null || formats.Length < formatCapacityInput))
                return XrResult.XR_ERROR_SIZE_INSUFFICIENT;

            if (formatCapacityInput == 0)
            {
                Log.D(TAG, "EnumerateSwapchainFormats(ci=" + formatCapacityInput + ")");
                return XrEnumerateSwapchainFormats(session, 0, ref formatCountOutput, IntPtr.Zero);
            }
            else
            {
                Log.D(TAG, "EnumerateSwapchainFormats(ci=" + formatCapacityInput + ", formats=long[" + formats.Length + "])");
                IntPtr formatsPtr = MemoryTools.MakeRawMemory(formats);
                var ret = XrEnumerateSwapchainFormats(session, formatCapacityInput, ref formatCountOutput, formatsPtr);
                if (ret == XrResult.XR_SUCCESS)
                    MemoryTools.CopyFromRawMemory(formats, formatsPtr, (int)formatCountOutput);
                MemoryTools.ReleaseRawMemory(formatsPtr);
                return ret;
            }
        }

        public XrResult EnumerateSwapchainImages(XrSwapchain swapchain, uint imageCapacityInput, ref uint imageCountOutput, IntPtr imagesPtr)
        {
            if (IsInited == false || XrEnumerateSwapchainImages == null)
            {
                imageCountOutput = 0;
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            return XrEnumerateSwapchainImages(swapchain, imageCapacityInput, ref imageCountOutput, imagesPtr);
        }

        [DllImport("viveopenxr", EntryPoint = "CwAcquireSwapchainImage")]
        public static extern XrResult CwAcquireSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageAcquireInfo acquireInfo, out uint index);

        public XrResult AcquireSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageAcquireInfo acquireInfo, out uint index)
        {
            if (IsInited == false || XrAcquireSwapchainImage == null)
            {
                index = 0;
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            Profiler.BeginSample("ASW:xrAcqScImg");
            var res = XrAcquireSwapchainImage(swapchain, ref acquireInfo, out index);
            Profiler.EndSample();
            return res;
        }

        [DllImport("viveopenxr", EntryPoint = "CwWaitSwapchainImage")]
        public static extern XrResult CwWaitSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageWaitInfo waitInfo);

        public XrResult WaitSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageWaitInfo waitInfo)
        {
            if (IsInited == false || XrWaitSwapchainImage == null)
            {
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            Profiler.BeginSample("ASW:xrWaitScImg");
            var res = XrWaitSwapchainImage(swapchain, ref waitInfo);
            Profiler.EndSample();
            return res;
        }

        [DllImport("viveopenxr", EntryPoint = "CwReleaseSwapchainImage")]
        public static extern XrResult CwReleaseSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageReleaseInfo releaseInfo);

        public XrResult ReleaseSwapchainImage(XrSwapchain swapchain, ref XrSwapchainImageReleaseInfo releaseInfo)
        {
            if (IsInited == false || XrReleaseSwapchainImage == null)
            {
                return XrResult.XR_ERROR_HANDLE_INVALID;
            }

            // Add Profiler
            Profiler.BeginSample("ASW:xrRelScImg");
            var res = XrReleaseSwapchainImage(swapchain, ref releaseInfo);
            Profiler.EndSample();
            return res;
        }
    }
}
